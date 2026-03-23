using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace STTech.BytesIO.Core
{
    /// <summary>
    /// 字节流服务器基类
    /// </summary>
    /// <typeparam name="TClient">客户端类型</typeparam>
    public abstract class BytesServer<TClient> : IBytesServer where TClient : BytesClient
    {
        /// <summary>
        /// 客户端列表字典
        /// </summary>
        protected readonly ConcurrentDictionary<TClient, byte> InternalClients = new ConcurrentDictionary<TClient, byte>();

        /// <summary>
        /// 服务器状态锁
        /// </summary>
        protected readonly object ServerStateLocker = new object();

        /// <summary>
        /// 服务器状态
        /// </summary>
        public ServerState State { get; protected set; } = ServerState.Closed;

        /// <summary>
        /// 是否正在运行
        /// </summary>
        public bool IsRunning => State != ServerState.Closed;

        /// <summary>
        /// 是否已暂停监听
        /// </summary>
        public bool IsPaused => State == ServerState.Paused;

        /// <summary>
        /// 是否正在监听新连接
        /// </summary>
        public bool IsListening => State == ServerState.Listening;

        /// <summary>
        /// 最大连接数
        /// </summary>
        public virtual uint MaxConnections { get; set; }

        /// <summary>
        /// 当前在线的客户端列表
        /// </summary>
        public virtual TClient[] Clients => InternalClients.Keys.ToArray();

        /// <summary>
        /// 客户端已连接事件
        /// </summary>
        public virtual event EventHandler<ServerClientConnectedEventArgs<TClient>> ClientConnected;

        /// <summary>
        /// 客户端已断开连接事件
        /// </summary>
        public virtual event EventHandler<ServerClientDisconnectedEventArgs<TClient>> ClientDisconnected;

        /// <summary>
        /// 发生异常事件
        /// </summary>
        public virtual event EventHandler<ExceptionOccursEventArgs> OnExceptionOccurs;

        /// <summary>
        /// 服务器已启动事件
        /// </summary>
        public virtual event EventHandler Started;

        /// <summary>
        /// 服务器已关闭事件
        /// </summary>
        public virtual event EventHandler Closed;

        /// <summary>
        /// 服务器已暂停监听事件
        /// </summary>
        public virtual event EventHandler Paused;

        /// <summary>
        /// 异步启动服务器
        /// </summary>
        public abstract Task StartAsync();

        /// <summary>
        /// 异步停止监听（不释放现有连接）
        /// </summary>
        public abstract Task StopAsync();

        /// <summary>
        /// 异步关闭服务器（释放所有连接）
        /// </summary>
        public abstract Task CloseAsync();

        /// <summary>
        /// 释放资源
        /// </summary>
        public virtual void Dispose()
        {
            CloseAsync().Wait();
        }

        protected virtual void OnStarted(EventArgs e)
        {
            Task.Run(() => Started?.Invoke(this, e));
        }

        protected virtual void OnClosed(EventArgs e)
        {
            Task.Run(() => Closed?.Invoke(this, e));
        }

        protected virtual void OnPaused(EventArgs e)
        {
            Task.Run(() => Paused?.Invoke(this, e));
        }

        protected virtual void OnClientConnected(TClient client)
        {
            InternalClients.TryAdd(client, 0);
            client.OnDisconnected += InternalClient_OnDisconnected;
            Task.Run(() => ClientConnected?.Invoke(this, new ServerClientConnectedEventArgs<TClient>(client)));
        }

        protected virtual void OnClientDisconnected(TClient client, DisconnectedEventArgs e)
        {
            InternalClients.TryRemove(client, out _);
            client.OnDisconnected -= InternalClient_OnDisconnected;
            Task.Run(() => ClientDisconnected?.Invoke(this, new ServerClientDisconnectedEventArgs<TClient>(client, e)));
        }

        private void InternalClient_OnDisconnected(object sender, DisconnectedEventArgs e)
        {
            if (sender is TClient client)
            {
                OnClientDisconnected(client, e);
            }
        }

        public void RaiseExceptionOccurs(Exception ex)
        {
            OnExceptionOccurs?.Invoke(this, new ExceptionOccursEventArgs(ex));
        }

        /// <inheritdoc/>
        public IEnumerable<BytesClient> GetClients() => InternalClients.Keys;
    }

    /// <summary>
    /// 服务器接口
    /// </summary>
    public interface IBytesServer : IDisposable
    {
        ServerState State { get; }
        bool IsRunning { get; }

        event EventHandler Started;
        event EventHandler Closed;
        event EventHandler Paused;
        event EventHandler<ExceptionOccursEventArgs> OnExceptionOccurs;

        Task StartAsync();
        Task StopAsync();
        Task CloseAsync();

        /// <summary>
        /// 获取所有客户端
        /// </summary>
        /// <returns></returns>
        IEnumerable<BytesClient> GetClients();
    }
}
