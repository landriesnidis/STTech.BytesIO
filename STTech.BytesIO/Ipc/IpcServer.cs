using STTech.BytesIO.Core;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace STTech.BytesIO.Ipc
{
    /// <summary>
    /// IPC服务端 (基于命名管道)
    /// </summary>
    public class IpcServer : IpcServer<IpcClient>
    {
        /// <summary>
        /// 构造IPC服务端
        /// </summary>
        public IpcServer()
        {
            EncapsulateStream = pipeStream => new IpcClient(pipeStream);
        }
    }

    /// <summary>
    /// IPC服务端基类
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class IpcServer<T> : BytesServer<T>, IIpcServer where T : IpcClient
    {
        private CancellationTokenSource cts;

        /// <inheritdoc/>
        public string PipeName { get; set; } = "STTech.BytesIO.Ipc.Default";

        /// <summary>
        /// 最大连接数量
        /// </summary>
        public override uint MaxConnections { get; set; } = unchecked((uint)NamedPipeServerStream.MaxAllowedServerInstances);

        private Func<object, IpcClientAcceptedEventArgs, bool> clientConnectionAcceptedHandle = (s, e) => true;
        /// <summary>
        /// 接收客户端连接时的处理过程
        /// 默认允许连接
        /// </summary>
        public Func<object, IpcClientAcceptedEventArgs, bool> ClientConnectionAcceptedHandle
        {
            get => clientConnectionAcceptedHandle;
            set => clientConnectionAcceptedHandle = value ?? ((s, e) => true);
        }

        /// <summary>
        /// 封装管道流的处理过程
        /// </summary>
        protected Func<NamedPipeServerStream, T> EncapsulateStream { get; set; }

        /// <inheritdoc/>
        public override Task StartAsync()
        {
            if (IsListening) return Task.FromResult(0);

            lock (ServerStateLocker)
            {
                cts = new CancellationTokenSource();
                State = ServerState.Listening;
                OnStarted(EventArgs.Empty);
            }

            _ = AcceptLoopAsync(cts.Token);
            return Task.FromResult(0);
        }

        private async Task AcceptLoopAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested && IsListening)
            {
                NamedPipeServerStream pipeServerStream = null;
                try
                {
                    pipeServerStream = new NamedPipeServerStream(
                        PipeName,
                        PipeDirection.InOut,
                        (int)MaxConnections,
                        PipeTransmissionMode.Byte,
                        PipeOptions.Asynchronous);

                    await pipeServerStream.WaitForConnectionAsync(token).ConfigureAwait(false);

                    if (ClientConnectionAcceptedHandle(this, new IpcClientAcceptedEventArgs(pipeServerStream)))
                    {
                        // 若由于任何原因进入暂停或即将关闭状态，则拒绝新连接
                        if (!IsListening)
                        {
                            pipeServerStream.Dispose();
                            continue;
                        }
                        _ = ProcessNewClientAsync(pipeServerStream);
                    }
                    else
                    {
                        pipeServerStream.Dispose();
                    }
                }
                catch (OperationCanceledException)
                {
                    pipeServerStream?.Dispose();
                    break;
                }
                catch (Exception ex)
                {
                    pipeServerStream?.Dispose();
                    RaiseExceptionOccurs(ex);
                    // 避免硬连接循环导致的 CPU 飙升
                    try { await Task.Delay(100, token).ConfigureAwait(false); } catch { }
                }
            }
        }

        private async Task ProcessNewClientAsync(NamedPipeServerStream pipeServerStream)
        {
            try
            {
                T client = EncapsulateStream(pipeServerStream);
                OnClientConnected(client);
            }
            catch (Exception ex)
            {
                RaiseExceptionOccurs(ex);
                pipeServerStream.Dispose();
            }
        }

        /// <inheritdoc/>
        public override Task StopAsync()
        {
            if (!IsListening) return Task.FromResult(0);

            lock (ServerStateLocker)
            {
                State = ServerState.Paused;
                cts?.Cancel();
                OnPaused(EventArgs.Empty);
            }
            return Task.FromResult(0);
        }

        /// <inheritdoc/>
        public override Task CloseAsync()
        {
            if (!IsRunning) return Task.FromResult(0);

            lock (ServerStateLocker)
            {
                State = ServerState.Closed;
                cts?.Cancel();

                foreach (var client in InternalClients.Keys)
                {
                    try { client.Disconnect(); } catch { }
                }
                InternalClients.Clear();

                OnClosed(EventArgs.Empty);
            }
            return Task.FromResult(0);
        }

        /// <inheritdoc/>
        public override void Dispose()
        {
            base.Dispose();
            cts?.Dispose();
        }
    }
}
