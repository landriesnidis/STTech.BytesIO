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
        public bool IsPaused => State == ServerState.Paused;

        /// <inheritdoc/>
        public bool IsListening => State == ServerState.Listening;

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

        /// <summary>
        /// 服务器暂停监听事件
        /// </summary>
        public virtual event EventHandler Paused;

        /// <inheritdoc/>
        public Task StartAsync()
        {
            if (IsListening) return Task.FromResult(0);

            lock (serverStateLocker)
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
                        MaxConnections,
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
                
                OnClientConnected(new IpcClientConnectedEventArgs(pipeServerStream, client));
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
                client.OnDisconnected -= Client_OnDisconnected;
                clients.TryRemove(client, out _);
                OnClientDisconnected(new IpcClientDisconnectedEventArgs(client, e));
            }
        }

        /// <summary>
        /// 在产生启动事件时
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnStarted(EventArgs e) => Task.Run(() => Started?.Invoke(this, e));

        /// <summary>
        /// 在产生关闭事件时
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnClosed(EventArgs e) => Task.Run(() => Closed?.Invoke(this, e));

        /// <summary>
        /// 在产生暂停事件时
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnPaused(EventArgs e) => Task.Run(() => Paused?.Invoke(this, e));

        /// <summary>
        /// 在客户端建立连接时
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnClientConnected(IpcClientConnectedEventArgs e) => Task.Run(() => ClientConnected?.Invoke(this, e));

        /// <summary>
        /// 在客户端断开连接时
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnClientDisconnected(IpcClientDisconnectedEventArgs e) => Task.Run(() => ClientDisconnected?.Invoke(this, e));

        /// <inheritdoc/>
        public Task StopAsync()
        {
            if (!IsListening) return Task.FromResult(0);

            lock (serverStateLocker)
            {
                State = ServerState.Paused;
                cts?.Cancel();
                OnPaused(EventArgs.Empty);
            }
            return Task.FromResult(0);
        }

        /// <inheritdoc/>
        public Task CloseAsync()
        {
            if (!IsRunning) return Task.FromResult(0);

            lock (serverStateLocker)
            {
                State = ServerState.Closed;
                cts?.Cancel();

                foreach (var client in clients.Keys)
                {
                    try { client.Disconnect(); } catch { }
                }
                clients.Clear();

                OnClosed(EventArgs.Empty);
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
