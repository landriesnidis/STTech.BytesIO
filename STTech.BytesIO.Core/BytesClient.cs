using STTech.BytesIO.Core.Entity;
using STTech.BytesIO.Core.Exceptions;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace STTech.BytesIO.Core
{
    /// <summary>
    /// 字节数组通信客户端
    /// </summary>
    public abstract partial class BytesClient: IBytesClient
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

        /// <summary>
        /// 异步任务取消令牌源
        /// </summary>
        private Lazy<CancellationTokenSource> _cancellationTokenSource = new Lazy<CancellationTokenSource>(true);
    }

    public abstract partial class BytesClient 
    {
        /// <summary>
        /// 异步连接任务取消令牌
        /// </summary>
        private CancellationToken _asyncConnectCancellationToken;

        /// <summary>
        /// 建立连接
        /// </summary>
        public abstract void Connect();

        /// <summary>
        /// 异步建立连接
        /// </summary>
        /// <returns></returns>
        public virtual Task ConnectAsync()
        {
            // 关闭异步连接任务
            if (_asyncConnectCancellationToken != null && _asyncConnectCancellationToken.IsCancellationRequested)
            {
                _asyncConnectCancellationToken.ThrowIfCancellationRequested();
            }

            _asyncConnectCancellationToken = _cancellationTokenSource.Value.Token;
            return Task.Run(() => Connect(), _asyncConnectCancellationToken);
        }

        /// <summary>
        /// 断开连接
        /// </summary>
        /// <param name="code">断开连接的原因</param>
        /// <param name="ex">导致连接断开的异常</param>
        public abstract void Disconnect(DisconnectionReasonCode code = DisconnectionReasonCode.Active, Exception ex = null);

        /// <summary>
        /// 异步断开连接
        /// </summary>
        /// <param name="code">断开连接的原因</param>
        /// <param name="ex">导致连接断开的异常</param>
        /// <returns></returns>
        public virtual Task DisconnectAsync(DisconnectionReasonCode code = DisconnectionReasonCode.Active, Exception ex = null) => Task.Run(() => Disconnect(code, ex));
    }
}
