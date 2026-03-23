using STTech.BytesIO.Core;
using System.Net.Quic;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace STTech.BytesIO.Quic
{
    /// <summary>
    /// QUIC 通信服务端
    /// </summary>
    public class QuicServer : BytesServer<QuicClient>
    {
        private QuicListener? _listener;
        private CancellationTokenSource? _cts;

        /// <summary>
        /// 监听地址
        /// </summary>
        public string Host { get; set; } = "0.0.0.0";

        /// <summary>
        /// 监听端口
        /// </summary>
        public int Port { get; set; } = 443;

        /// <summary>
        /// 应用层协议协商 (ALPN)
        /// </summary>
        public List<SslApplicationProtocol> AlpnProtocols { get; set; } = new () { new SslApplicationProtocol("sttech-bytesio") };

        /// <summary>
        /// 服务器证书
        /// </summary>
        public X509Certificate2? ServerCertificate { get; set; }

        public override async Task StartAsync()
        {
            lock (ServerStateLocker)
            {
                if (IsRunning) return;
                State = ServerState.Listening;
            }

            try
            {
                if (ServerCertificate == null)
                {
                    throw new InvalidOperationException("QUIC 服务端必须提供有效的服务器证书。");
                }

                var options = new QuicListenerOptions
                {
                    ListenEndPoint = new System.Net.IPEndPoint(System.Net.IPAddress.Parse(Host), Port),
                    ApplicationProtocols = AlpnProtocols,
                    ConnectionOptionsCallback = (connection, helloInfo, cancellationToken) =>
                    {
                        return ValueTask.FromResult(new QuicServerConnectionOptions
                        {
                            ServerAuthenticationOptions = new SslServerAuthenticationOptions
                            {
                                ServerCertificate = ServerCertificate,
                                ApplicationProtocols = AlpnProtocols
                            },
                            DefaultStreamErrorCode = 0,
                            DefaultCloseErrorCode = 0
                        });
                    }
                };

                _listener = await QuicListener.ListenAsync(options).ConfigureAwait(false);
                _cts = new CancellationTokenSource();

                _ = AcceptConnectionsAsync(_cts.Token);

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

        private async Task AcceptConnectionsAsync(CancellationToken token)
        {
            if (_listener == null) return;
            try
            {
                while (!token.IsCancellationRequested && IsListening)
                {
                    var connection = await _listener.AcceptConnectionAsync(token).ConfigureAwait(false);
                    _ = HandleConnectionAsync(connection, token);
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                if (!token.IsCancellationRequested)
                {
                    RaiseExceptionOccurs(ex);
                }
            }
        }

        private async Task HandleConnectionAsync(QuicConnection connection, CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    var stream = await connection.AcceptInboundStreamAsync(token).ConfigureAwait(false);
                    var client = new QuicClient(stream, connection);
                    OnClientConnected(client);
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                if (!token.IsCancellationRequested)
                {
                    RaiseExceptionOccurs(ex);
                }
            }
            finally
            {
                await connection.DisposeAsync();
            }
        }

        public override async Task StopAsync()
        {
            lock (ServerStateLocker)
            {
                if (!IsListening) return;
                State = ServerState.Paused;
            }

            _cts?.Cancel();
            if (_listener != null)
            {
                await _listener.DisposeAsync().ConfigureAwait(false);
                _listener = null;
            }

            OnPaused(EventArgs.Empty);
        }

        public override async Task CloseAsync()
        {
            await StopAsync();

            lock (ServerStateLocker)
            {
                State = ServerState.Closed;
            }

            foreach (var client in Clients)
            {
                client.Disconnect();
            }

            OnClosed(EventArgs.Empty);
        }

        public override void Dispose()
        {
            CloseAsync().Wait();
            base.Dispose();
        }
    }
}
