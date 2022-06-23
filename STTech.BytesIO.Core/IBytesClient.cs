using STTech.BytesIO.Core.Entity;
using System;

namespace STTech.BytesIO.Core
{
    /// <summary>
    /// 字节数组通信客户端接口
    /// </summary>
    public interface IBytesClient : IDataSender<byte[]>, IDataSender<byte[],byte[]>
    {
        /// <summary>
        /// 建立连接
        /// </summary>
        void Connect();

        /// <summary>
        /// 断开连接
        /// </summary>
        void Disconnect(DisconnectionReasonCode code, Exception ex = null);

        /// <summary>
        /// 是否已连接
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// 接受缓存区大小
        /// </summary>
        int ReceiveBufferSize { get; set; }

        /// <summary>
        /// 发送缓存区大小
        /// </summary>
        int SendBufferSize { get; set; }

        /// <summary>
        /// 最后一次消息的时间戳
        /// </summary>
        DateTime LastMessageReceivedTime { get; }
    }
}
