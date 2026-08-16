using STTech.BytesIO.Core;
using System;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;

namespace STTech.BytesIO.Ipc
{
    /// <summary>
    /// IPC通信客户端 (基于命名管道)
    /// </summary>
    public class IpcClient : BytesClient, IIpcClient
    {
        private PipeStream innerClient;

        /// <summary>
        /// 内部管道客户端
        /// </summary>
        protected PipeStream InnerClient => innerClient;

        /// <summary>
        /// 连接建立后（生成连接标识之后、启动接收任务之前）的扩展钩子。
        /// 供子类初始化传输层资源（如打开共享内存映射）。
        /// </summary>
        protected virtual void OnConnectionEstablished()
        {
        }

        /// <inheritdoc/>
        public string PipeName { get; set; } = "STTech.BytesIO.Ipc.Default";

        /// <inheritdoc/>
        public string ServerName { get; set; } = ".";

        /// <inheritdoc/>
        public override bool IsConnected => innerClient != null && innerClient.IsConnected;

        /// <summary>
        /// 构造IPC客户端
        /// </summary>
        public IpcClient()
        {
        }

        /// <summary>
        /// 构造IPC客户端
        /// </summary>
        /// <param name="pipeStream">内部管道流</param>
        public IpcClient(PipeStream pipeStream)
        {
            innerClient = pipeStream;
                if (innerClient.IsConnected)
                {
                    GenerateNewConnectionId();
                    OnConnectionEstablished();

                    if (pipeStream is NamedPipeServerStream serverStream)
                    {
                        // 无法直接从流中获取管道名称，通常由服务端在创建后赋值，或者保持默认
                    }

                    StartReceiveDataTask();
                }
        }

        /// <inheritdoc/>
        public override event EventHandler<DataReceivedEventArgs> OnDataReceived
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

        /// <inheritdoc/>
        public override ConnectResult Connect(ConnectArgument argument = null)
        {
            argument ??= new ConnectArgument();
            var sw = System.Diagnostics.Stopwatch.StartNew();

            if (IsConnected)
            {
                return new ConnectResult(ConnectErrorCode.IsConnected);
            }

            try
            {
                var client = new NamedPipeClientStream(ServerName, PipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
                client.Connect(argument.Timeout);
                innerClient = client;

                GenerateNewConnectionId();
                OnConnectionEstablished();
                sw.Stop();
                RaiseConnectedSuccessfully(this, new ConnectedSuccessfullyEventArgs() { CostTime = sw.Elapsed, State = argument.State });

                StartReceiveDataTask();

                return new ConnectResult() { CostTime = sw.Elapsed };
            }
            catch (TimeoutException ex)
            {
                sw.Stop();
                var failedArgs = new ConnectionFailedEventArgs(ConnectErrorCode.Timeout, ex) { CostTime = sw.Elapsed, State = argument.State };
                RaiseConnectionFailed(this, failedArgs);
                return new ConnectResult(ConnectErrorCode.Timeout, ex) { CostTime = sw.Elapsed };
            }
            catch (Exception ex)
            {
                sw.Stop();
                var failedArgs = new ConnectionFailedEventArgs(ConnectErrorCode.Error, ex) { CostTime = sw.Elapsed, State = argument.State };
                RaiseConnectionFailed(this, failedArgs);
                return new ConnectResult(ConnectErrorCode.Error, ex) { CostTime = sw.Elapsed };
            }
        }

        /// <inheritdoc/>
        public override async Task<ConnectResult> ConnectAsync(ConnectArgument argument = null)
        {
             argument ??= new ConnectArgument();
            var sw = System.Diagnostics.Stopwatch.StartNew();

            if (IsConnected)
            {
                return new ConnectResult(ConnectErrorCode.IsConnected);
            }

            try
            {
                var client = new NamedPipeClientStream(ServerName, PipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
                await client.ConnectAsync(argument.Timeout, argument.CancellationToken).ConfigureAwait(false);
                innerClient = client;

                GenerateNewConnectionId();
                OnConnectionEstablished();
                sw.Stop();
                RaiseConnectedSuccessfully(this, new ConnectedSuccessfullyEventArgs() { CostTime = sw.Elapsed, State = argument.State });

                StartReceiveDataTask();

                return new ConnectResult() { CostTime = sw.Elapsed };
            }
            catch (TimeoutException ex)
            {
                sw.Stop();
                var failedArgs = new ConnectionFailedEventArgs(ConnectErrorCode.Timeout, ex) { CostTime = sw.Elapsed, State = argument.State };
                RaiseConnectionFailed(this, failedArgs);
                return new ConnectResult(ConnectErrorCode.Timeout, ex) { CostTime = sw.Elapsed };
            }
            catch (Exception ex)
            {
                sw.Stop();
                var failedArgs = new ConnectionFailedEventArgs(ConnectErrorCode.Error, ex) { CostTime = sw.Elapsed, State = argument.State };
                RaiseConnectionFailed(this, failedArgs);
                return new ConnectResult(ConnectErrorCode.Error, ex) { CostTime = sw.Elapsed };
            }
        }

        /// <inheritdoc/>
        public override DisconnectResult Disconnect(DisconnectArgument argument = null)
        {
            argument ??= new DisconnectArgument();
            if (IsConnected)
            {
                try
                {
                    innerClient?.Close();
                }
                catch (Exception ex)
                {
                    RaiseExceptionOccurs(this, new ExceptionOccursEventArgs(ex));
                }
                finally
                {
                    innerClient?.Dispose();
                    innerClient = null;
                }

                RaiseDisconnected(this, new DisconnectedEventArgs(argument.ReasonCode, argument.Exception));
                return new DisconnectResult();
            }
            return new DisconnectResult(DisconnectErrorCode.NoConnection);
        }

        /// <inheritdoc/>
        protected override async Task SendHandlerAsync(SendArgs args)
        {
            try
            {
                if (IsConnected)
                {
                    await innerClient.WriteAsync(args.Data, 0, args.Data.Length).ConfigureAwait(false);
                    if (args.Options.FlushImmediately)
                    {
                        await innerClient.FlushAsync().ConfigureAwait(false);
                    }

                    RaiseDataSent(this, new DataSentEventArgs(args.Data));

                    if (args.Options.PauseTime > 0)
                    {
                        await Task.Delay(args.Options.PauseTime).ConfigureAwait(false);
                    }
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
            try
            {
                while (IsConnected && !cancellationToken.IsCancellationRequested)
                {
                    byte[] buffer = RentBuffer();
                    try
                    {
                        int len = await innerClient.ReadAsync(buffer, 0, ReceiveBufferSize, cancellationToken).ConfigureAwait(false);

                        if (len == 0)
                        {
                            System.Buffers.ArrayPool<byte>.Shared.Return(buffer);
                            Disconnect(new DisconnectArgument(DisconnectionReasonCode.Passive));
                            return;
                        }

                        var context = CreateReceiveContext(buffer, 0, len);
                        InvokeDataReceivedEventCallback(context);
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
                 if (cancellationToken.IsCancellationRequested) return;
                 RaiseExceptionOccurs(this, new ExceptionOccursEventArgs(ex));
            }
        }

        /// <inheritdoc/>
        protected override void ReceiveDataCompletedHandle()
        {
            ResetInnerClient();
        }

        private void ResetInnerClient()
        {
            try
            {
                innerClient?.Close();
            }
            catch { }
            finally
            {
                innerClient?.Dispose();
                innerClient = null;
            }
        }

        /// <inheritdoc/>
        public override void Dispose()
        {
            innerClient?.Dispose();
            innerClient = null;
        }
    }
}
