using System;
using System.Buffers;
using System.Threading.Tasks;

namespace STTech.BytesIO.Core
{
    /// <summary>
    /// 字节数组通信客户端
    /// </summary>
    public abstract partial class BytesClient : IBytesClient
    {
        /// <summary>
        /// 接受缓存区大小
        /// </summary>
        private int _receiveBufferSize = 64 * 1024;

        /// <summary>
        /// 当前是否已连接
        /// </summary>
        public abstract bool IsConnected { get; }

        /// <summary>
        /// 接受缓存区大小
        /// </summary>
        public virtual int ReceiveBufferSize
        {
            get => _receiveBufferSize;
            set
            {
                if (IsConnected)
                {
                    throw new InvalidOperationException("连接时不允许修改ReceiveBufferSize");
                }
                _receiveBufferSize = value;
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
        /// 记录本次正式建立通讯连线的时间刻度 (上线时间)
        /// </summary>
        protected DateTime? LastConnectedTime { get; set; }

        /// <summary>
        /// 当前活动会话的唯一凭证标识，每次重连后会刷新。
        /// </summary>
        public string ConnectionId { get; protected set; }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public abstract void Dispose();

        /// <summary>
        /// 更新最后一次通信的时间戳
        /// </summary>
        /// <param name="time">手动设置时间戳</param>
        private void UpdateLastMessageTimestamp(DateTime? time = null) => LastMessageReceivedTime = time ?? DateTime.Now;

        /// <summary>
        /// 从 ArrayPool 租借一个缓冲区
        /// </summary>
        /// <returns>池化的 byte 数组</returns>
        protected byte[] RentBuffer()
        {
            return ArrayPool<byte>.Shared.Rent(ReceiveBufferSize);
        }

        /// <summary>
        /// 创建接收上下文
        /// </summary>
        /// <param name="rentedArray">从 ArrayPool 租借的数组</param>
        /// <param name="offset">有效数据起始偏移</param>
        /// <param name="length">有效数据长度</param>
        /// <returns>接收上下文</returns>
        protected ReceiveContext CreateReceiveContext(byte[] rentedArray, int offset, int length)
        {
            return new ReceiveContext(rentedArray, offset, length);
        }

        /// <summary>
        /// 生成全新的连接会话标识
        /// </summary>
        protected void GenerateNewConnectionId()
        {
            ConnectionId = Guid.NewGuid().ToString("N");
            LastConnectedTime = DateTime.Now;
        }
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
