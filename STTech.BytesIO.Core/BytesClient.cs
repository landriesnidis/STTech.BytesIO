using STTech.BytesIO.Core;
using STTech.BytesIO.Core.Exceptions;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace STTech.BytesIO.Core
{
    /// <summary>
    /// 字节数组通信客户端
    /// </summary>
    public abstract partial class BytesClient : IBytesClient
    {
        /// <summary>
        /// 当前是否已连接
        /// </summary>
        public virtual bool IsConnected => false;

        /// <summary>
        /// 接受缓存区大小
        /// </summary>
        public virtual int ReceiveBufferSize { get; set; }

        /// <summary>
        /// 发送缓存区大小
        /// </summary>
        public virtual int SendBufferSize { get; set; }

        /// <summary>
        /// 最后一次消息的时间戳
        /// </summary>
        public DateTime LastMessageReceivedTime { get; private set; }

        /// <summary>
        /// 更新最后一次通信的时间戳
        /// </summary>
        /// <param name="time">手动设置时间戳</param>
        protected void UpdateLastMessageTimestamp(DateTime? time = null) => LastMessageReceivedTime = time ?? DateTime.Now;
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
        /// 断开连接
        /// </summary>
        /// <param name="code">断开连接的原因</param>
        /// <param name="ex">导致连接断开的异常</param>
        public abstract DisconnectResult Disconnect(DisconnectArgument argument = null);

        /// <summary>
        /// 异步断开连接
        /// </summary>
        /// <param name="code">断开连接的原因</param>
        /// <param name="ex">导致连接断开的异常</param>
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
