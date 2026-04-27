using Microsoft.Extensions.DependencyInjection;
using Multiformats.Address;
using Nethermind.Libp2p;
using Nethermind.Libp2p.Core;
using STTech.BytesIO.Core;
using System.Buffers;
using System.ComponentModel;

namespace STTech.BytesIO.P2P
{
    /// <summary>
    /// P2P 通信客户端
    /// <para>基于 Libp2p 协议栈，继承自 <see cref="BytesClient"/>，实现 P2P 节点之间的字节流通信。</para>
    /// </summary>
    public partial class PeerClient : BytesClient, IPeerClient
    {
        private ServiceProvider? _serviceProvider;
        private ILocalPeer? _localPeer;
        private ISession? _remoteSession;
        private IChannel? _channel;

        /// <summary>
        /// 内部状态
        /// </summary>
        private InnerStatus _innerStatus = InnerStatus.Free;

        /// <summary>
        /// 状态锁
        /// </summary>
        private readonly object _statusLocker = new();

        /// <inheritdoc/>
        public override bool IsConnected => _channel != null && _remoteSession != null;

        /// <inheritdoc/>
        public override int SendBufferSize { get; set; } = 65536;

        /// <summary>
        /// 构造 P2P 客户端
        /// </summary>
        public PeerClient()
        {
        }

        /// <summary>
        /// 通过已有的 Channel 和 Session 构造 P2P 客户端（用于 BootstrapServer 接入场景）
        /// </summary>
        /// <param name="channel">已建立的 Libp2p 通道</param>
        /// <param name="session">已建立的远端会话</param>
        /// <param name="context">会话上下文</param>
        internal PeerClient(IChannel channel, ISessionContext context)
        {
            _channel = channel;
            _innerStatus = InnerStatus.Busy;

            // 从 context 中提取远端地址信息
            var remoteAddr = context.State.RemoteAddress;
            if (remoteAddr != null)
            {
                RemoteAddress = remoteAddr.ToString();
                var peerId = remoteAddr.Get<Multiformats.Address.Protocols.P2P>();
                if (peerId != null)
                {
                    RemotePeerId = peerId.ToString();
                }
            }

            if (IsConnected)
            {
                GenerateNewConnectionId();
                StartReceiveDataTask();
            }
        }

        /// <inheritdoc/>
        public override ConnectResult Connect(ConnectArgument? argument = null)
        {
            return ConnectAsync(argument).GetAwaiter().GetResult();
        }

        /// <inheritdoc/>
        public override async Task<ConnectResult> ConnectAsync(ConnectArgument? argument = null)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            argument ??= new ConnectArgument();

            lock (_statusLocker)
            {
                if (IsConnected || _innerStatus == InnerStatus.Busy)
                {
                    sw.Stop();
                    var failedArgs = new ConnectionFailedEventArgs(ConnectErrorCode.IsConnected) { CostTime = sw.Elapsed, State = argument.State };
                    RaiseConnectionFailed(this, failedArgs);
                    return new ConnectResult(ConnectErrorCode.IsConnected) { CostTime = sw.Elapsed };
                }

                _innerStatus = InnerStatus.Busy;
            }

            try
            {
                // 构建 Libp2p 服务
                var bytesIOProtocol = new BytesIOProtocol();

                // 设置通道就绪回调
                var channelTcs = new TaskCompletionSource<(IChannel channel, ISessionContext context)>(TaskCreationOptions.RunContinuationsAsynchronously);
                bytesIOProtocol.OnChannelEstablished += (ch, ctx, isDialer) =>
                {
                    channelTcs.TrySetResult((ch, ctx));
                };

                _serviceProvider = BuildServiceProvider(Options, bytesIOProtocol);
                var peerFactory = _serviceProvider.GetRequiredService<IPeerFactory>();

                // 创建本地节点
                Identity? identity = Options.IdentitySeed != null ? new Identity(Options.IdentitySeed) : null;
                _localPeer = peerFactory.Create(identity);
                LocalPeerId = _localPeer.Identity.PeerId.ToString();

                // 解析远端地址
                Multiaddress remoteAddr = RemoteAddress;

                // 拨号连接远端
                var dialTask = _localPeer.DialAsync(remoteAddr, argument.CancellationToken);
                var completedTask = await Task.WhenAny(dialTask, Task.Delay(argument.Timeout, argument.CancellationToken)).ConfigureAwait(false);

                if (completedTask != dialTask)
                {
                    // 超时
                    ResetInternalState();
                    _innerStatus = InnerStatus.Free;
                    sw.Stop();
                    var failedArgs = new ConnectionFailedEventArgs(ConnectErrorCode.Timeout) { CostTime = sw.Elapsed, State = argument.State };
                    RaiseConnectionFailed(this, failedArgs);
                    return new ConnectResult(ConnectErrorCode.Timeout) { CostTime = sw.Elapsed };
                }

                _remoteSession = await dialTask.ConfigureAwait(false);

                // 在远端会话上协商 BytesIO 协议
                await _remoteSession.DialAsync<BytesIOProtocol>(argument.CancellationToken).ConfigureAwait(false);

                // 等待通道就绪
                var channelResult = await channelTcs.Task.WaitAsync(TimeSpan.FromMilliseconds(argument.Timeout), argument.CancellationToken).ConfigureAwait(false);
                _channel = channelResult.channel;

                // 提取远端 PeerId
                var remotePeerAddr = _remoteSession.RemoteAddress;
                if (remotePeerAddr != null)
                {
                    var peerId = remotePeerAddr.Get<Multiformats.Address.Protocols.P2P>();
                    if (peerId != null)
                    {
                        RemotePeerId = peerId.ToString();
                    }
                }

                GenerateNewConnectionId();
                sw.Stop();
                RaiseConnectedSuccessfully(this, new ConnectedSuccessfullyEventArgs() { CostTime = sw.Elapsed, State = argument.State });
                StartReceiveDataTask();

                _innerStatus = InnerStatus.Free;
                return new ConnectResult() { CostTime = sw.Elapsed };
            }
            catch (Exception ex)
            {
                sw.Stop();
                ResetInternalState();
                _innerStatus = InnerStatus.Free;

                var failedArgs = new ConnectionFailedEventArgs(ConnectErrorCode.Error, ex) { CostTime = sw.Elapsed, State = argument.State };
                RaiseConnectionFailed(this, failedArgs);
                return new ConnectResult(ConnectErrorCode.Error, ex) { CostTime = sw.Elapsed };
            }
        }

        /// <inheritdoc/>
        public override DisconnectResult Disconnect(DisconnectArgument? argument = null)
        {
            argument ??= new DisconnectArgument();

            if (argument.GracefulShutdown) WaitAndDrainSendQueue(3000);

            lock (_statusLocker)
            {
                if (_channel != null || _remoteSession != null || _innerStatus == InnerStatus.Busy)
                {
                    try
                    {
                        CancelReceiveDataTask();
                        _channel?.CloseAsync().AsTask().GetAwaiter().GetResult();
                        _remoteSession?.DisconnectAsync().GetAwaiter().GetResult();
                    }
                    catch (Exception ex)
                    {
                        RaiseExceptionOccurs(this, new ExceptionOccursEventArgs(ex));
                    }

                    ResetInternalState();
                    _innerStatus = InnerStatus.Free;

                    RaiseDisconnected(this, new DisconnectedEventArgs(argument.ReasonCode, argument.Exception));
                    return new DisconnectResult();
                }
                else
                {
                    return new DisconnectResult(DisconnectErrorCode.NoConnection);
                }
            }
        }

        /// <inheritdoc/>
        protected override async Task SendHandlerAsync(SendArgs sendArgs)
        {
            if (_channel == null) return;
            try
            {
                var data = new ReadOnlySequence<byte>(sendArgs.Data);
                await _channel.WriteAsync(data).ConfigureAwait(false);
                RaiseDataSent(this, new DataSentEventArgs(sendArgs.Data));

                if (sendArgs.Options.PauseTime > 0)
                {
                    await Task.Delay(sendArgs.Options.PauseTime).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                RaiseExceptionOccurs(this, new ExceptionOccursEventArgs(ex));
            }
        }

        /// <inheritdoc/>
        protected override async Task ReceiveDataHandleAsync(CancellationToken cancellationToken)
        {
            if (_channel == null) return;
            try
            {
                await foreach (var data in _channel.ReadAllAsync(cancellationToken).ConfigureAwait(false))
                {
                    if (cancellationToken.IsCancellationRequested) break;

                    byte[] buffer = data.ToArray();
                    if (buffer.Length == 0)
                    {
                        Disconnect(new DisconnectArgument(DisconnectionReasonCode.Passive));
                        return;
                    }

                    // 使用池化缓冲区
                    byte[] rentedBuffer = RentBuffer();
                    int copyLen = Math.Min(buffer.Length, rentedBuffer.Length);
                    Buffer.BlockCopy(buffer, 0, rentedBuffer, 0, copyLen);

                    var context = CreateReceiveContext(rentedBuffer, 0, copyLen);
                    InvokeDataReceivedEventCallback(context);
                }
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception ex)
            {
                if (!cancellationToken.IsCancellationRequested)
                {
                    RaiseExceptionOccurs(this, new ExceptionOccursEventArgs(ex));
                    Disconnect(new DisconnectArgument(DisconnectionReasonCode.Passive, ex));
                }
            }
        }

        /// <inheritdoc/>
        protected override void ReceiveDataCompletedHandle()
        {
            ResetInternalState();
        }

        /// <inheritdoc/>
        public override void Dispose()
        {
            try
            {
                _channel?.CloseAsync().AsTask().GetAwaiter().GetResult();
            }
            catch { }

            try
            {
                _remoteSession?.DisconnectAsync().GetAwaiter().GetResult();
            }
            catch { }

            try
            {
                _localPeer?.DisposeAsync().AsTask().GetAwaiter().GetResult();
            }
            catch { }

            _serviceProvider?.Dispose();

            _channel = null;
            _remoteSession = null;
            _localPeer = null;
            _serviceProvider = null;
        }

        /// <summary>
        /// 重置内部连接状态
        /// </summary>
        private void ResetInternalState()
        {
            try { _channel?.CloseAsync().AsTask().GetAwaiter().GetResult(); } catch { }
            try { _remoteSession?.DisconnectAsync().GetAwaiter().GetResult(); } catch { }
            _channel = null;
            _remoteSession = null;
        }

        /// <summary>
        /// 构建 Libp2p 服务容器
        /// </summary>
        internal static ServiceProvider BuildServiceProvider(P2PNodeOptions options, BytesIOProtocol protocol)
        {
            return new ServiceCollection()
                .AddLibp2p(builder =>
                {
                    // 添加自定义协议
                    builder.AddProtocol<BytesIOProtocol>();

                    // 根据选项配置传输及功能
                    if (options.EnableQuic)
                    {
                        builder.WithQuic();
                    }

                    if (options.EnableRelay)
                    {
                        builder.WithRelay();
                    }

                    if (options.EnablePubSub)
                    {
                        builder.WithPubsub();
                    }

                    if (options.EnforcePlaintext)
                    {
                        builder.WithPlaintextEnforced();
                    }

                    return builder;
                })
                .AddSingleton(protocol)
                .BuildServiceProvider();
        }

        /// <summary>
        /// 内部状态枚举
        /// </summary>
        private enum InnerStatus
        {
            Free,
            Busy,
        }
    }

    // ===============================================================================
    // 
    //                                  属性
    // 
    // ===============================================================================

    public partial class PeerClient : IPeerClient
    {
        /// <summary>
        /// 远端 Multiaddr 地址
        /// </summary>
        [Category("P2P 属性")]
        [Description("远端节点的 Multiaddr 地址。")]
        public string RemoteAddress { get; set; } = string.Empty;

        /// <summary>
        /// 本地节点的 PeerId
        /// </summary>
        [Category("P2P 属性")]
        [Description("本地节点的唯一标识符 (PeerId)。")]
        public string? LocalPeerId { get; private set; }

        /// <summary>
        /// 远端节点的 PeerId
        /// </summary>
        [Category("P2P 属性")]
        [Description("远端节点的唯一标识符 (PeerId)。")]
        public string? RemotePeerId { get; private set; }

        /// <summary>
        /// P2P 节点配置选项
        /// </summary>
        [Category("P2P 配置")]
        [Description("P2P 节点的详细配置选项。")]
        public P2PNodeOptions Options { get; set; } = new();
    }
}
