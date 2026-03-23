using STTech.BytesIO.Core;
using System.Net.Quic;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace STTech.BytesIO.Quic
{
    /// <summary>
    /// QUIC 通信客户端
    /// </summary>
    public class QuicClient : BytesClient, IQuicClient
    {
        private QuicConnection? _connection;
        private QuicStream? _stream;

        /// <summary>
        /// 远程主机
        /// </summary>
        public string Host { get; set; } = "127.0.0.1";

        /// <summary>
        /// 远程端口
        /// </summary>
        public int Port { get; set; } = 443;

        /// <summary>
        /// 应用层协议协商 (ALPN)
        /// </summary>
        public List<SslApplicationProtocol> AlpnProtocols { get; set; } = new () { new SslApplicationProtocol("sttech-bytesio") };

        /// <summary>
        /// 客户端认证选项
        /// </summary>
        public QuicClientConnectionOptions ClientConnectionOptions { get; set; } = new ();

        /// <inheritdoc/>
        public override bool IsConnected => _stream != null && _connection != null;

        public QuicClient()
        {
        }

        public QuicClient(QuicStream stream, QuicConnection connection)
        {
            _stream = stream;
            _connection = connection;
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
            try
            {
                if (IsConnected) return new ConnectResult(ConnectErrorCode.IsConnected);

                var clientOptions = new QuicClientConnectionOptions
                {
                    RemoteEndPoint = new System.Net.IPEndPoint(System.Net.IPAddress.Parse(Host), Port),
                    ClientAuthenticationOptions = new SslClientAuthenticationOptions
                    {
                        ApplicationProtocols = AlpnProtocols,
                        RemoteCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true // 默认信任所有证书，生产环境建议配置
                    },
                    DefaultStreamErrorCode = 0,
                    DefaultCloseErrorCode = 0
                };

                _connection = await QuicConnection.ConnectAsync(clientOptions).ConfigureAwait(false);
                _stream = await _connection.OpenOutboundStreamAsync(QuicStreamType.Bidirectional).ConfigureAwait(false);

                GenerateNewConnectionId();
                sw.Stop();
                RaiseConnectedSuccessfully(this, new ConnectedSuccessfullyEventArgs { CostTime = sw.Elapsed });
                StartReceiveDataTask();

                return new ConnectResult { CostTime = sw.Elapsed };
            }
            catch (Exception ex)
            {
                sw.Stop();
                var failedArgs = new ConnectionFailedEventArgs(ConnectErrorCode.Error, ex) { CostTime = sw.Elapsed };
                RaiseConnectionFailed(this, failedArgs);
                return new ConnectResult(ConnectErrorCode.Error, ex) { CostTime = sw.Elapsed };
            }
        }

        /// <inheritdoc/>
        public override DisconnectResult Disconnect(DisconnectArgument? argument = null)
        {
            argument ??= new DisconnectArgument();
            if (IsConnected)
            {
                try
                {
                    _stream?.Dispose();
                    _connection?.CloseAsync(0).AsTask().GetAwaiter().GetResult();
                    _connection?.DisposeAsync().AsTask().GetAwaiter().GetResult();
                }
                catch (Exception ex)
                {
                    RaiseExceptionOccurs(this, new ExceptionOccursEventArgs(ex));
                }
                finally
                {
                    _stream = null;
                    _connection = null;
                }

                RaiseDisconnected(this, new DisconnectedEventArgs(argument.ReasonCode, argument.Exception));
                return new DisconnectResult();
            }
            return new DisconnectResult(DisconnectErrorCode.NoConnection);
        }

        /// <inheritdoc/>
        protected override async Task SendHandlerAsync(SendArgs args)
        {
            if (_stream == null) return;
            try
            {
                await _stream.WriteAsync(args.Data, 0, args.Data.Length).ConfigureAwait(false);
                if (args.Options.FlushImmediately)
                {
                    await _stream.FlushAsync().ConfigureAwait(false);
                }
                RaiseDataSent(this, new DataSentEventArgs(args.Data));
            }
            catch (Exception ex)
            {
                RaiseExceptionOccurs(this, new ExceptionOccursEventArgs(ex));
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
                            Disconnect(new DisconnectArgument(DisconnectionReasonCode.Passive));
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
                            Disconnect(new DisconnectArgument(DisconnectionReasonCode.Passive, ex));
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
            _stream?.Dispose();
            _connection?.DisposeAsync().AsTask().GetAwaiter().GetResult();
        }
    }
}
