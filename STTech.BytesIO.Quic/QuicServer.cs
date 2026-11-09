using STTech.BytesIO.Core;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Quic;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace STTech.BytesIO.Quic
{
    /// <summary>
    /// QUIC 通信服务端
    /// </summary>
    public class QuicServer : BytesServer<QuicClient>, IQuicServer
    {
        private QuicListener? _listener;
        private CancellationTokenSource? _serverCts;
        private CancellationTokenSource? _listenerCts;
        private int _activeConnectionCount = 0;
        private readonly ConcurrentDictionary<QuicConnection, ConcurrentDictionary<QuicClient, byte>> _connectionClients 
            = new ConcurrentDictionary<QuicConnection, ConcurrentDictionary<QuicClient, byte>>();

        /// <summary>
        /// 监听地址
        /// </summary>
        public string Host { get; set; } = "0.0.0.0";

        /// <summary>
        /// 监听端口
        /// </summary>
        public int Port { get; set; } = 443;

        /// <summary>
        /// 应用层协议协商 (ALPN) 列表
        /// </summary>
        public List<SslApplicationProtocol> AlpnProtocols { get; set; } = new() { new SslApplicationProtocol("sttech-bytesio") };

        /// <summary>
        /// 服务器默认证书
        /// </summary>
        public X509Certificate2? ServerCertificate { get; set; }

        /// <summary>
        /// 服务端 TLS 身份认证选项生成回调（支持根据 SNI 路由不同证书及 mTLS 双向认证配置）
        /// </summary>
        public Func<QuicConnection, SslClientHelloInfo, SslServerAuthenticationOptions>? ServerAuthenticationOptionsCallback { get; set; }

        /// <summary>
        /// 每个物理连接允许的最大入站双向流数量。默认 100。
        /// </summary>
        public int MaxInboundBidirectionalStreams { get; set; } = 100;

        /// <summary>
        /// 每个物理连接允许的最大入站单向流数量。默认 100。
        /// </summary>
        public int MaxInboundUnidirectionalStreams { get; set; } = 100;

        /// <summary>
        /// 连接空闲超时时间
        /// </summary>
        public TimeSpan? IdleTimeout { get; set; }

        /// <summary>
        /// 服务端在物理连接建立后是否主动开辟出站流（适用于“服务端先发言/下发问候令牌”模式）。默认 false。
        /// </summary>
        public bool ServerInitiatedStreams { get; set; } = false;

        /// <summary>
        /// 服务端主动开辟出站流时发送的初始问候/握手载荷（用于激活网络层帧并直接向客户端下发欢迎问候报文）。
        /// </summary>
        public byte[]? ServerInitiatedStreamPayload { get; set; }

        /// <summary>
        /// 是否接受入站单向流。若为 false（默认），服务端仅接受双向流，单向流将安全释放并忽略。
        /// </summary>
        public bool AcceptUnidirectionalStreams { get; set; } = false;

        /// <summary>
        /// 当物理 QUIC 连接建立时触发（在接受具体通信流之前）
        /// </summary>
        public event EventHandler<QuicConnectionAcceptedEventArgs>? ConnectionAccepted;

        /// <inheritdoc/>
        public override async Task StartAsync()
        {
            lock (ServerStateLocker)
            {
                // 仅当正在监听时退出；若处于 Paused 状态，则支持重新启动恢复监听
                if (IsListening) return;
                State = ServerState.Listening;
            }

            try
            {
                if (!QuicListener.IsSupported)
                {
                    throw new PlatformNotSupportedException("当前运行环境不支持 QUIC 监听。请检查操作系统是否支持 TLS 1.3 并且已安装 msquic 支持库。");
                }

                if (ServerCertificate == null && ServerAuthenticationOptionsCallback == null)
                {
                    string hostSubject = (Host == "0.0.0.0" || Host == "::" || Host == "127.0.0.1") ? "localhost" : Host;
                    ServerCertificate = QuicCertificateHelper.CreateSelfSignedCertificate(hostSubject);
                }

                IPAddress ipAddress;
                if (!IPAddress.TryParse(Host, out ipAddress!))
                {
                    var addresses = await Dns.GetHostAddressesAsync(Host).ConfigureAwait(false);
                    if (addresses.Length == 0)
                    {
                        throw new SocketException((int)SocketError.HostNotFound);
                    }
                    ipAddress = addresses.FirstOrDefault(a => a.AddressFamily == AddressFamily.InterNetwork) ?? addresses[0];
                }

                var options = new QuicListenerOptions
                {
                    ListenEndPoint = new IPEndPoint(ipAddress, Port),
                    ApplicationProtocols = AlpnProtocols,
                    ConnectionOptionsCallback = (connection, helloInfo, cancellationToken) =>
                    {
                        SslServerAuthenticationOptions serverAuth;
                        if (ServerAuthenticationOptionsCallback != null)
                        {
                            serverAuth = ServerAuthenticationOptionsCallback(connection, helloInfo);
                        }
                        else
                        {
                            serverAuth = new SslServerAuthenticationOptions
                            {
                                ServerCertificate = ServerCertificate,
                                ApplicationProtocols = AlpnProtocols
                            };
                        }

                        return ValueTask.FromResult(new QuicServerConnectionOptions
                        {
                            ServerAuthenticationOptions = serverAuth,
                            MaxInboundBidirectionalStreams = MaxInboundBidirectionalStreams,
                            MaxInboundUnidirectionalStreams = MaxInboundUnidirectionalStreams,
                            IdleTimeout = IdleTimeout ?? TimeSpan.Zero,
                            DefaultStreamErrorCode = 0,
                            DefaultCloseErrorCode = 0
                        });
                    }
                };

                _listener = await QuicListener.ListenAsync(options).ConfigureAwait(false);
                _serverCts ??= new CancellationTokenSource();
                _listenerCts = new CancellationTokenSource();

                _ = AcceptConnectionsAsync(_listenerCts.Token, _serverCts.Token);

                OnStarted(EventArgs.Empty);
            }
            catch (Exception ex)
            {
                lock (ServerStateLocker)
                {
                    State = ServerState.Closed;
                }
                RaiseExceptionOccurs(ex);
                throw;
            }
        }

        private async Task AcceptConnectionsAsync(CancellationToken listenerToken, CancellationToken serverToken)
        {
            if (_listener == null) return;
            try
            {
                while (!listenerToken.IsCancellationRequested && IsListening)
                {
                    var connection = await _listener.AcceptConnectionAsync(listenerToken).ConfigureAwait(false);

                    // 原子自增检查连接数阈值，杜绝瞬间突发穿透与线程池 DoS
                    if (MaxConnections > 0 && Interlocked.Increment(ref _activeConnectionCount) > MaxConnections)
                    {
                        Interlocked.Decrement(ref _activeConnectionCount);
                        try
                        {
                            await connection.CloseAsync(0x10, CancellationToken.None).ConfigureAwait(false);
                            await connection.DisposeAsync().ConfigureAwait(false);
                        }
                        catch { }
                        continue;
                    }

                    _ = HandleConnectionAsync(connection, serverToken);
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                if (!listenerToken.IsCancellationRequested)
                {
                    RaiseExceptionOccurs(ex);
                }
            }
        }

        private async Task HandleConnectionAsync(QuicConnection connection, CancellationToken serverToken)
        {
            var streamClients = new ConcurrentDictionary<QuicClient, byte>();
            _connectionClients.TryAdd(connection, streamClients);

            try
            {
                try
                {
                    ConnectionAccepted?.Invoke(this, new QuicConnectionAcceptedEventArgs(connection));
                }
                catch (Exception ex)
                {
                    RaiseExceptionOccurs(ex);
                }

                // 若启用了服务端主动发流模式，立即向客户端开辟首条双向通信流
                if (ServerInitiatedStreams)
                {
                    var serverOutboundStream = await connection.OpenOutboundStreamAsync(QuicStreamType.Bidirectional, serverToken).ConfigureAwait(false);
                    if (ServerInitiatedStreamPayload != null && ServerInitiatedStreamPayload.Length > 0)
                    {
                        await serverOutboundStream.WriteAsync(ServerInitiatedStreamPayload.AsMemory(), serverToken).ConfigureAwait(false);
                        await serverOutboundStream.FlushAsync(serverToken).ConfigureAwait(false);
                    }

                    var serverOutboundClient = new QuicClient(serverOutboundStream, connection, ownsConnection: false);
                    streamClients.TryAdd(serverOutboundClient, 0);

                    serverOutboundClient.OnDisconnected += (s, e) =>
                    {
                        streamClients.TryRemove(serverOutboundClient, out _);
                    };

                    OnClientConnected(serverOutboundClient);
                    serverOutboundClient.EnsureReceiveTaskStarted();
                }

                while (!serverToken.IsCancellationRequested)
                {
                    QuicStream stream;
                    try
                    {
                        stream = await connection.AcceptInboundStreamAsync(serverToken).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch (QuicException)
                    {
                        // 对端已断开或连接关闭
                        break;
                    }

                    // 过滤不接受的单向只读流
                    if (!AcceptUnidirectionalStreams && stream.Type == QuicStreamType.Unidirectional)
                    {
                        try { await stream.DisposeAsync().ConfigureAwait(false); } catch { }
                        continue;
                    }

                    // 创建服务端专用的 QuicClient，设定 ownsConnection 为 false
                    var client = new QuicClient(stream, connection, ownsConnection: false);
                    streamClients.TryAdd(client, 0);

                    client.OnDisconnected += (s, e) =>
                    {
                        streamClients.TryRemove(client, out _);
                    };

                    OnClientConnected(client);
                    client.EnsureReceiveTaskStarted();
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                if (!serverToken.IsCancellationRequested)
                {
                    RaiseExceptionOccurs(ex);
                }
            }
            finally
            {
                Interlocked.Decrement(ref _activeConnectionCount);
                _connectionClients.TryRemove(connection, out _);

                // 清理本物理连接上残余的所有流客户端
                foreach (var client in streamClients.Keys)
                {
                    try
                    {
                        client.Disconnect(new DisconnectArgument(DisconnectionReasonCode.Passive));
                    }
                    catch { }
                }

                try
                {
                    await connection.CloseAsync(0).ConfigureAwait(false);
                    await connection.DisposeAsync().ConfigureAwait(false);
                }
                catch { }
            }
        }

        /// <inheritdoc/>
        public override async Task StopAsync()
        {
            lock (ServerStateLocker)
            {
                if (!IsListening) return;
                State = ServerState.Paused;
            }

            // 仅停止监听新连接，绝不中断现有在线客户端的通信会话
            _listenerCts?.Cancel();
            if (_listener != null)
            {
                await _listener.DisposeAsync().ConfigureAwait(false);
                _listener = null;
            }

            OnPaused(EventArgs.Empty);
        }

        /// <inheritdoc/>
        public override async Task CloseAsync()
        {
            await StopAsync().ConfigureAwait(false);

            lock (ServerStateLocker)
            {
                State = ServerState.Closed;
            }

            _serverCts?.Cancel();
            _serverCts = null;

            // 全异步高并发断开所有客户端，避免串行同步调用造成线程池饥饿
            var clientTasks = Clients.Select(async client =>
            {
                try
                {
                    await client.DisconnectAsync().ConfigureAwait(false);
                }
                catch { }
            });
            await Task.WhenAll(clientTasks).ConfigureAwait(false);

            // 主动释放所有底层物理连接
            foreach (var conn in _connectionClients.Keys)
            {
                try
                {
                    await conn.CloseAsync(0).ConfigureAwait(false);
                    await conn.DisposeAsync().ConfigureAwait(false);
                }
                catch { }
            }
            _connectionClients.Clear();

            OnClosed(EventArgs.Empty);
        }

        /// <inheritdoc/>
        public override void Dispose()
        {
            if (State != ServerState.Closed)
            {
                try
                {
                    Task.Run(async () => await CloseAsync().ConfigureAwait(false)).GetAwaiter().GetResult();
                }
                catch { }
            }
            // 注意：不调用 base.Dispose()，避免基类中二次执行 CloseAsync().Wait() 导致死锁
        }
    }
}
