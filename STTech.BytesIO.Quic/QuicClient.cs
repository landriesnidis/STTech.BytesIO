using STTech.BytesIO.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Quic;
using System.Net.Security;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace STTech.BytesIO.Quic
{
    /// <summary>
    /// QUIC 通信客户端
    /// </summary>
    public class QuicClient : BytesClient, IQuicClient
    {
        private QuicConnection? _connection;
        private QuicStream? _stream;
        private readonly bool _ownsConnection;
        private volatile bool _isConnected;
        private bool _isConnecting;
        private readonly object _statusLock = new object();

        /// <summary>
        /// 底层物理 QUIC 连接
        /// </summary>
        public QuicConnection? Connection => _connection;

        /// <summary>
        /// 当前通信逻辑流
        /// </summary>
        public QuicStream? Stream => _stream;

        /// <summary>
        /// 远程主机（支持 IPv4/IPv6 或域名）
        /// </summary>
        public string Host { get; set; } = "127.0.0.1";

        /// <summary>
        /// 远程端口
        /// </summary>
        public int Port { get; set; } = 443;

        /// <summary>
        /// SNI (Server Name Indication) 目标主机名。若未显式指定，默认采用 <see cref="Host"/>。
        /// </summary>
        public string? TargetHost { get; set; }

        /// <summary>
        /// 是否允许不受信任的服务器证书（自签名证书测试或内部环境使用，生产环境建议保持 false 以防中间人劫持）
        /// </summary>
        public bool AllowUntrustedCertificates { get; set; } = false;

        /// <summary>
        /// 自定义远程证书验证回调
        /// </summary>
        public RemoteCertificateValidationCallback? RemoteCertificateValidationCallback { get; set; }

        /// <summary>
        /// 应用层协议协商 (ALPN) 列表
        /// </summary>
        public List<SslApplicationProtocol> AlpnProtocols { get; set; } = new() { new SslApplicationProtocol("sttech-bytesio") };

        /// <summary>
        /// 客户端连接选项模板
        /// </summary>
        public QuicClientConnectionOptions ClientConnectionOptions { get; set; } = new();

        /// <summary>
        /// 初始通信流的发起方式（默认为 Client 模式）
        /// </summary>
        public QuicStreamInitiator StreamInitiator { get; set; } = QuicStreamInitiator.Client;

        /// <summary>
        /// 连接建立后自动发送的初始握手/同步载荷（若非 null，将在连接建立后立即写入 Stream，用于唤醒服务端并建立双向流会话，避免在“服务端先发言”协议模式下发生死锁）。
        /// </summary>
        public byte[]? InitialPayload { get; set; }

        /// <summary>
        /// 当前通信流类型（双向流或单向流）
        /// </summary>
        public QuicStreamType StreamType => _stream?.Type ?? QuicStreamType.Bidirectional;

        /// <summary>
        /// 当前通信流是否支持写入数据
        /// </summary>
        public bool CanWrite => _stream?.CanWrite ?? false;

        /// <summary>
        /// 当前通信流是否支持读取数据
        /// </summary>
        public bool CanRead => _stream?.CanRead ?? false;

        /// <inheritdoc/>
        public override bool IsConnected => _isConnected && _stream != null && _connection != null;

        /// <inheritdoc/>
        public override event EventHandler<STTech.BytesIO.Core.DataReceivedEventArgs> OnDataReceived
        {
            add
            {
                base.OnDataReceived += value;

                if (IsConnected && ReceiveTaskCancellationTokenSource == null)
                {
                    StartReceiveDataTask();
                }
            }
            remove { base.OnDataReceived -= value; }
        }

        /// <summary>
        /// 默认构造函数（客户端模式，拥有物理连接的所有权）
        /// </summary>
        public QuicClient()
        {
            _ownsConnection = true;
        }

        /// <summary>
        /// 基于已建立的 QuicStream 和 QuicConnection 构造客户端（服务端入站会话或多流复用模式）
        /// </summary>
        /// <param name="stream">已建立的 QUIC 流</param>
        /// <param name="connection">宿主物理连接</param>
        /// <param name="ownsConnection">是否拥有物理连接的所有权（默认服务端子 Client 不拥有，避免单个流断开导致整条物理连接被腰斩）</param>
        public QuicClient(QuicStream stream, QuicConnection connection, bool ownsConnection = false)
        {
            _stream = stream ?? throw new ArgumentNullException(nameof(stream));
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
            _ownsConnection = ownsConnection;

            // 正确提取并赋值真实的远端 IP 和端口
            if (connection.RemoteEndPoint is IPEndPoint ipEndPoint)
            {
                Host = ipEndPoint.Address.ToString();
                Port = ipEndPoint.Port;
            }
            else if (connection.RemoteEndPoint != null)
            {
                Host = connection.RemoteEndPoint.ToString() ?? "127.0.0.1";
            }

            _isConnected = true;
            GenerateNewConnectionId();
        }

        /// <summary>
        /// 确保数据接收后台任务已启动
        /// </summary>
        public void EnsureReceiveTaskStarted()
        {
            if (IsConnected && ReceiveTaskCancellationTokenSource == null)
            {
                StartReceiveDataTask();
            }
        }

        /// <summary>
        /// 在当前的物理 QUIC 连接上创建一个新的逻辑流客户端（多流复用）。
        /// 新客户端共享同一个底层的物理连接，但拥有独立的发送与接收数据管道。
        /// </summary>
        /// <param name="type">流类型（默认双向流）</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>新的 QUIC 流客户端实例</returns>
        public async Task<QuicClient> CreateStreamClientAsync(QuicStreamType type = QuicStreamType.Bidirectional, CancellationToken cancellationToken = default)
        {
            if (!IsConnected || _connection == null)
            {
                throw new InvalidOperationException("当前客户端未连接，无法创建新的 QUIC 流。");
            }

            var stream = await _connection.OpenOutboundStreamAsync(type, cancellationToken).ConfigureAwait(false);
            var streamClient = new QuicClient(stream, _connection, ownsConnection: false)
            {
                Host = this.Host,
                Port = this.Port,
                TargetHost = this.TargetHost,
                AllowUntrustedCertificates = this.AllowUntrustedCertificates
            };
            return streamClient;
        }

        /// <inheritdoc/>
        public override ConnectResult Connect(ConnectArgument? argument = null)
        {
            return Task.Run(() => ConnectAsync(argument)).GetAwaiter().GetResult();
        }

        /// <inheritdoc/>
        public override async Task<ConnectResult> ConnectAsync(ConnectArgument? argument = null)
        {
            argument ??= new ConnectArgument();
            var sw = Stopwatch.StartNew();

            lock (_statusLock)
            {
                if (IsConnected || _isConnecting)
                {
                    var failedArgs = new ConnectionFailedEventArgs(ConnectErrorCode.IsConnected) { CostTime = sw.Elapsed, State = argument.State };
                    RaiseConnectionFailed(this, failedArgs);
                    return new ConnectResult(ConnectErrorCode.IsConnected) { CostTime = sw.Elapsed };
                }
                _isConnecting = true;
            }

            try
            {
                if (!QuicConnection.IsSupported)
                {
                    throw new PlatformNotSupportedException("当前运行环境不支持 QUIC。请检查操作系统是否支持 TLS 1.3 并且已安装 msquic 支持库。");
                }

                // 异步解析 Host 为 IPAddress（支持纯数值 IP 以及域名/localhost）
                IPAddress ipAddress;
                if (!IPAddress.TryParse(Host, out ipAddress!))
                {
                    var addresses = await Dns.GetHostAddressesAsync(Host, argument.CancellationToken).ConfigureAwait(false);
                    if (addresses.Length == 0)
                    {
                        throw new SocketException((int)SocketError.HostNotFound);
                    }
                    ipAddress = addresses.FirstOrDefault(a => a.AddressFamily == AddressFamily.InterNetwork) ?? addresses[0];
                }

                // 计算 SNI TargetHost
                string sniHost = !string.IsNullOrEmpty(TargetHost)
                    ? TargetHost
                    : (!string.IsNullOrEmpty(ClientConnectionOptions.ClientAuthenticationOptions?.TargetHost)
                        ? ClientConnectionOptions.ClientAuthenticationOptions.TargetHost
                        : Host);

                // 完整继承与深拷贝用户 SslClientAuthenticationOptions，保留用户定制的证书选择、吊销检查与加密套件策略
                SslClientAuthenticationOptions clientAuth;
                if (ClientConnectionOptions.ClientAuthenticationOptions != null)
                {
                    clientAuth = new SslClientAuthenticationOptions
                    {
                        ApplicationProtocols = ClientConnectionOptions.ClientAuthenticationOptions.ApplicationProtocols ?? AlpnProtocols,
                        TargetHost = sniHost,
                        ClientCertificates = ClientConnectionOptions.ClientAuthenticationOptions.ClientCertificates,
                        CertificateRevocationCheckMode = ClientConnectionOptions.ClientAuthenticationOptions.CertificateRevocationCheckMode,
                        EncryptionPolicy = ClientConnectionOptions.ClientAuthenticationOptions.EncryptionPolicy,
                        LocalCertificateSelectionCallback = ClientConnectionOptions.ClientAuthenticationOptions.LocalCertificateSelectionCallback,
                        CipherSuitesPolicy = ClientConnectionOptions.ClientAuthenticationOptions.CipherSuitesPolicy,
                        AllowRenegotiation = ClientConnectionOptions.ClientAuthenticationOptions.AllowRenegotiation,
                        RemoteCertificateValidationCallback = RemoteCertificateValidationCallback
                            ?? ClientConnectionOptions.ClientAuthenticationOptions.RemoteCertificateValidationCallback
                            ?? (AllowUntrustedCertificates ? (sender, certificate, chain, sslPolicyErrors) => true : null)
                    };
                }
                else
                {
                    clientAuth = new SslClientAuthenticationOptions
                    {
                        ApplicationProtocols = AlpnProtocols,
                        TargetHost = sniHost,
                        RemoteCertificateValidationCallback = RemoteCertificateValidationCallback
                            ?? (AllowUntrustedCertificates ? (sender, certificate, chain, sslPolicyErrors) => true : null)
                    };
                }

                var clientOptions = new QuicClientConnectionOptions
                {
                    RemoteEndPoint = new IPEndPoint(ipAddress, Port),
                    DefaultCloseErrorCode = ClientConnectionOptions.DefaultCloseErrorCode >= 0 ? ClientConnectionOptions.DefaultCloseErrorCode : 0,
                    DefaultStreamErrorCode = ClientConnectionOptions.DefaultStreamErrorCode >= 0 ? ClientConnectionOptions.DefaultStreamErrorCode : 0,
                    IdleTimeout = ClientConnectionOptions.IdleTimeout,
                    MaxInboundBidirectionalStreams = ClientConnectionOptions.MaxInboundBidirectionalStreams > 0 ? ClientConnectionOptions.MaxInboundBidirectionalStreams : 100,
                    MaxInboundUnidirectionalStreams = ClientConnectionOptions.MaxInboundUnidirectionalStreams > 0 ? ClientConnectionOptions.MaxInboundUnidirectionalStreams : 100,
                    LocalEndPoint = ClientConnectionOptions.LocalEndPoint,
                    ClientAuthenticationOptions = clientAuth
                };

                using var cts = CancellationTokenSource.CreateLinkedTokenSource(argument.CancellationToken);
                if (argument.Timeout > 0)
                {
                    cts.CancelAfter(argument.Timeout);
                }

                _connection = await QuicConnection.ConnectAsync(clientOptions, cts.Token).ConfigureAwait(false);

                // 根据流发起模式确定开流行为
                if (StreamInitiator == QuicStreamInitiator.Client)
                {
                    _stream = await _connection.OpenOutboundStreamAsync(QuicStreamType.Bidirectional, cts.Token).ConfigureAwait(false);

                    // 若配置了初始握手/同步载荷，立即写入以激活网络层帧，避免“服务端先发言”时的死锁
                    if (InitialPayload != null && InitialPayload.Length > 0)
                    {
                        await _stream.WriteAsync(InitialPayload.AsMemory(), cts.Token).ConfigureAwait(false);
                        await _stream.FlushAsync(cts.Token).ConfigureAwait(false);
                    }
                }
                else
                {
                    // 服务端主动发流模式：等待服务端开辟入站流
                    _stream = await _connection.AcceptInboundStreamAsync(cts.Token).ConfigureAwait(false);
                }

                lock (_statusLock)
                {
                    _isConnected = true;
                }

                GenerateNewConnectionId();
                sw.Stop();
                RaiseConnectedSuccessfully(this, new ConnectedSuccessfullyEventArgs { CostTime = sw.Elapsed, State = argument.State });
                StartReceiveDataTask();

                return new ConnectResult { CostTime = sw.Elapsed };
            }
            catch (OperationCanceledException ex)
            {
                await CleanupFailedConnectionAsync().ConfigureAwait(false);
                sw.Stop();
                ConnectErrorCode code = argument.CancellationToken.IsCancellationRequested ? ConnectErrorCode.Error : ConnectErrorCode.Timeout;
                var failedArgs = new ConnectionFailedEventArgs(code, ex) { CostTime = sw.Elapsed, State = argument.State };
                RaiseConnectionFailed(this, failedArgs);
                return new ConnectResult(code, ex) { CostTime = sw.Elapsed };
            }
            catch (Exception ex)
            {
                await CleanupFailedConnectionAsync().ConfigureAwait(false);
                sw.Stop();
                var failedArgs = new ConnectionFailedEventArgs(ConnectErrorCode.Error, ex) { CostTime = sw.Elapsed, State = argument.State };
                RaiseConnectionFailed(this, failedArgs);
                return new ConnectResult(ConnectErrorCode.Error, ex) { CostTime = sw.Elapsed };
            }
            finally
            {
                lock (_statusLock)
                {
                    _isConnecting = false;
                }
            }
        }

        private async Task CleanupFailedConnectionAsync()
        {
            if (_stream != null)
            {
                try { await _stream.DisposeAsync().ConfigureAwait(false); } catch { }
                _stream = null;
            }

            if (_connection != null)
            {
                try
                {
                    await _connection.CloseAsync(0).ConfigureAwait(false);
                    await _connection.DisposeAsync().ConfigureAwait(false);
                }
                catch { }
                _connection = null;
            }
        }

        /// <inheritdoc/>
        public override DisconnectResult Disconnect(DisconnectArgument? argument = null)
        {
            return Task.Run(() => DisconnectAsync(argument)).GetAwaiter().GetResult();
        }

        /// <inheritdoc/>
        public override async Task<DisconnectResult> DisconnectAsync(DisconnectArgument? argument = null)
        {
            argument ??= new DisconnectArgument();
            lock (_statusLock)
            {
                if (!IsConnected)
                {
                    return new DisconnectResult(DisconnectErrorCode.NoConnection);
                }
                _isConnected = false;
            }

            try
            {
                if (_stream != null)
                {
                    await _stream.DisposeAsync().ConfigureAwait(false);
                    _stream = null;
                }

                if (_ownsConnection && _connection != null)
                {
                    try
                    {
                        await _connection.CloseAsync(0).ConfigureAwait(false);
                        await _connection.DisposeAsync().ConfigureAwait(false);
                    }
                    catch { }
                    _connection = null;
                }
            }
            catch (Exception ex)
            {
                RaiseExceptionOccurs(this, new ExceptionOccursEventArgs(ex));
            }

            RaiseDisconnected(this, new DisconnectedEventArgs(argument.ReasonCode, argument.Exception));
            return new DisconnectResult();
        }

        /// <inheritdoc/>
        protected override async Task SendHandlerAsync(SendArgs args)
        {
            if (_stream == null)
            {
                throw new InvalidOperationException("QUIC 客户端尚未连接或流已关闭。");
            }

            if (!_stream.CanWrite)
            {
                throw new InvalidOperationException("当前 QUIC 流为单向入站流（Unidirectional Inbound Stream），不支持写入数据。");
            }

            var cancellationToken = args.Options.CancellationToken;

            try
            {
                // 传入 cancellationToken，应对 QUIC 流控背压阻塞时支持及时超时/取消，防止挂死发送队列
                await _stream.WriteAsync(args.Data.AsMemory(), cancellationToken).ConfigureAwait(false);
                if (args.Options.FlushImmediately)
                {
                    await _stream.FlushAsync(cancellationToken).ConfigureAwait(false);
                }

                RaiseDataSent(this, new DataSentEventArgs(args.Data));

                if (args.Options.PauseTime > 0)
                {
                    await Task.Delay(args.Options.PauseTime, cancellationToken).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                RaiseExceptionOccurs(this, new ExceptionOccursEventArgs(ex));
                throw; // 必须向上抛出，由基类发送队列通知调用方 Task 失败，防止报假成功
            }
        }

        /// <inheritdoc/>
        protected override async Task ReceiveDataHandleAsync(CancellationToken cancellationToken)
        {
            if (_stream == null) return;
            try
            {
                while (IsConnected && !cancellationToken.IsCancellationRequested)
                {
                    byte[] buffer = RentBuffer();
                    try
                    {
                        int len = await _stream.ReadAsync(buffer, 0, ReceiveBufferSize, cancellationToken).ConfigureAwait(false);
                        if (len == 0)
                        {
                            System.Buffers.ArrayPool<byte>.Shared.Return(buffer);
                            _ = DisconnectAsync(new DisconnectArgument(DisconnectionReasonCode.Passive));
                            return;
                        }

                        var context = CreateReceiveContext(buffer, 0, len);
                        InvokeDataReceivedEventCallback(context);
                    }
                    catch (OperationCanceledException)
                    {
                        System.Buffers.ArrayPool<byte>.Shared.Return(buffer);
                        return;
                    }
                    catch (Exception ex)
                    {
                        System.Buffers.ArrayPool<byte>.Shared.Return(buffer);
                        if (!cancellationToken.IsCancellationRequested)
                        {
                            RaiseExceptionOccurs(this, new ExceptionOccursEventArgs(ex));
                            _ = DisconnectAsync(new DisconnectArgument(DisconnectionReasonCode.Passive, ex));
                        }
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                if (!cancellationToken.IsCancellationRequested)
                {
                    RaiseExceptionOccurs(this, new ExceptionOccursEventArgs(ex));
                }
            }
        }

        /// <inheritdoc/>
        protected override void ReceiveDataCompletedHandle()
        {
            _stream?.Dispose();
            _stream = null;
        }

        /// <inheritdoc/>
        public override void Dispose()
        {
            try
            {
                Disconnect();
            }
            catch { }
        }
    }
}
