using System;
using System.Threading.Tasks;

namespace STTech.BytesIO.Core
{
    /// <summary>
    /// 字节数组通信客户端
    /// </summary>
    public abstract partial class BytesClient : IBytesClient
    {
        /// <summary>
        /// 内存块池
        /// </summary>
        protected MemoryBlockPool MemoryBlockPool = new MemoryBlockPool(64 * 1024, 10);

        /// <summary>
        /// 当前是否已连接
        /// </summary>
        public abstract bool IsConnected { get; }

        /// <summary>
        /// 接受缓存区大小
        /// </summary>
        public virtual int ReceiveBufferSize
        {
            get => MemoryBlockPool.BlockSize;
            set
            {
                if (IsConnected)
                {
                    throw new InvalidOperationException("连接时不允许修改ReceiveBufferSize");
                }
                MemoryBlockPool = new MemoryBlockPool(value);
            }
        }

        /// <summary>
        /// 发送缓存区大小
        /// </summary>
        public virtual int SendBufferSize { get; set; }

        /// <summary>
        /// 最后一次消息的时间戳
        /// </summary>
        public DateTime LastMessageReceivedTime { get; private set; }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public abstract void Dispose();

        /// <summary>
        /// 更新最后一次通信的时间戳
        /// </summary>
        /// <param name="time">手动设置时间戳</param>
        private void UpdateLastMessageTimestamp(DateTime? time = null) => LastMessageReceivedTime = time ?? DateTime.Now;
    }

    public abstract partial class BytesClient
    {
        // 异步连接Locker
        private readonly object asyncConnectLocker = new();

        /// <summary>
        /// 建立连接
        /// </summary>
        public abstract ConnectResult Connect(ConnectArgument argument = null);

        /// <summary>
        /// 建立连接
        /// </summary>
        /// <param name="timeout">超时时间</param>
        /// <returns></returns>
        public virtual ConnectResult Connect(int timeout) => Connect(new ConnectArgument() { Timeout = timeout });

        /// <summary>
        /// 异步建立连接
        /// </summary>
        /// <returns></returns>
        public virtual Task<ConnectResult> ConnectAsync(ConnectArgument argument = null)
        {
            return Task.Run(() =>
            {
                lock (asyncConnectLocker)
                {
                    return Connect(argument);
                }
            });
        }

        /// <summary>
        /// 异步建立连接
        /// </summary>
        /// <param name="timeout">超时时间</param>
        /// <returns></returns>
        public virtual Task<ConnectResult> ConnectAsync(int timeout) => ConnectAsync(new ConnectArgument() { Timeout = timeout });

        /// <summary>
        /// 断开连接
        /// </summary>
        /// <param name="argument">断开连接携带的参数</param>
        public abstract DisconnectResult Disconnect(DisconnectArgument argument = null);

        /// <summary>
        /// 异步断开连接
        /// </summary>
        /// <param name="argument">断开连接携带的参数</param>
        /// <returns></returns>
        public virtual Task<DisconnectResult> DisconnectAsync(DisconnectArgument argument = null)
            => Task.Run(() =>
            {
                lock (asyncConnectLocker)
                {
                    return Disconnect(argument);
                }
            });
    }
}
