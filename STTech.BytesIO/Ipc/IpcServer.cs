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
    public abstract class IpcServer<T> : IIpcServer where T : IpcClient
    {
        private ConcurrentDictionary<T, byte> clients = new ConcurrentDictionary<T, byte>();
        private readonly object serverStateLocker = new object();
        private CancellationTokenSource cts;

        /// <inheritdoc/>
        public ServerState State { get; private set; } = ServerState.Closed;

        /// <inheritdoc/>
        public bool IsRunning => State != ServerState.Closed;

        /// <inheritdoc/>
        public string PipeName { get; set; } = "STTech.BytesIO.Ipc.Default";

        /// <summary>
        /// 最大连接数量
        /// </summary>
        public int MaxConnections { get; set; } = NamedPipeServerStream.MaxAllowedServerInstances;

        /// <summary>
        /// 客户端列表
        /// </summary>
        public T[] Clients => clients.Keys.ToArray();

        /// <summary>
        /// 接收客户端连接时的处理过程
        /// 默认允许连接
        /// </summary>
        public Func<object, IpcClientAcceptedEventArgs, bool> ClientConnectionAcceptedHandle { get; set; } = (s, e) => true;

        /// <summary>
        /// 封装管道流的处理过程
        /// </summary>
        protected Func<NamedPipeServerStream, T> EncapsulateStream { get; set; }

        /// <summary>
        /// 客户端建立连接事件
        /// </summary>
        public virtual event EventHandler<IpcClientConnectedEventArgs> ClientConnected;

        /// <summary>
        /// 客户端断开连接事件
        /// </summary>
        public virtual event EventHandler<IpcClientDisconnectedEventArgs> ClientDisconnected;

        /// <summary>
        /// 在产生异常时发生
        /// </summary>
        public virtual event EventHandler<ExceptionOccursEventArgs> OnExceptionOccurs;

        /// <summary>
        /// 服务器启动事件
        /// </summary>
        public virtual event EventHandler Started;

        /// <summary>
        /// 服务器关闭事件
        /// </summary>
        public virtual event EventHandler Closed;

        /// <inheritdoc/>
        public Task StartAsync()
        {
            if (State == ServerState.Listening) return Task.FromResult(0);

            lock (serverStateLocker)
            {
                cts = new CancellationTokenSource();
                State = ServerState.Listening;
                Started?.Invoke(this, EventArgs.Empty);
            }

            _ = AcceptLoopAsync(cts.Token);
            return Task.FromResult(0);
        }

        private async Task AcceptLoopAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested && State == ServerState.Listening)
            {
                NamedPipeServerStream pipeServerStream = null;
                try
                {
                    pipeServerStream = new NamedPipeServerStream(
                        PipeName,
                        PipeDirection.InOut,
                        MaxConnections,
                        PipeTransmissionMode.Byte,
                        PipeOptions.Asynchronous);

                    await pipeServerStream.WaitForConnectionAsync(token).ConfigureAwait(false);

                    if (ClientConnectionAcceptedHandle(this, new IpcClientAcceptedEventArgs(pipeServerStream)))
                    {
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
                    OnExceptionOccurs?.Invoke(this, new ExceptionOccursEventArgs(ex));
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
                clients.TryAdd(client, 0);
                client.OnDisconnected += Client_OnDisconnected;
                
                ClientConnected?.Invoke(this, new IpcClientConnectedEventArgs(pipeServerStream, client));
            }
            catch (Exception ex)
            {
                OnExceptionOccurs?.Invoke(this, new ExceptionOccursEventArgs(ex));
                pipeServerStream.Dispose();
            }
        }

        private void Client_OnDisconnected(object sender, DisconnectedEventArgs e)
        {
            if (sender is T client)
            {
                clients.TryRemove(client, out _);
                client.OnDisconnected -= Client_OnDisconnected;
                ClientDisconnected?.Invoke(this, new IpcClientDisconnectedEventArgs(client, e));
            }
        }

        /// <inheritdoc/>
        public Task StopAsync()
        {
            return CloseAsync();
        }

        /// <inheritdoc/>
        public Task CloseAsync()
        {
            lock (serverStateLocker)
            {
                if (State == ServerState.Closed) return Task.FromResult(0);

                State = ServerState.Closed;
                cts?.Cancel();

                foreach (var client in clients.Keys)
                {
                    client.Disconnect();
                }
                clients.Clear();

                Closed?.Invoke(this, EventArgs.Empty);
            }
            return Task.FromResult(0);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            CloseAsync().Wait();
            cts?.Dispose();
        }
    }
}
