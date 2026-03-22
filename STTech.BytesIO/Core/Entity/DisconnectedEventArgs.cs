using System;

namespace STTech.BytesIO.Core
{
    /// <summary>
    /// 通信已断开事件参数
    /// </summary>
    public class DisconnectedEventArgs : EventArgs
    {
        /// <summary>
        /// 是否是主动断开的连接
        /// </summary>
        public bool IsActively => ReasonCode == DisconnectionReasonCode.Active || ReasonCode == DisconnectionReasonCode.Timeout;

        /// <summary>
        /// 异常信息
        /// </summary>
        public Exception Exception { get; }

        /// <summary>
        /// 断开连接的原因
        /// </summary>
        public DisconnectionReasonCode ReasonCode { get; }

        /// <summary>
        /// 该通道的唯一身份特征流标签。
        /// </summary>
        public string ConnectionId { get; set; }

        /// <summary>
        /// 存活生命周期：从上一次正式上线到本次硬断连经历的时间跨度。
        /// </summary>
        public TimeSpan Duration { get; set; }

        /// <summary>
        /// 构造通信已断开事件参数
        /// </summary>
        public DisconnectedEventArgs()
        {
            ReasonCode = DisconnectionReasonCode.Active;
        }

        /// <summary>
        /// 构造通信已断开事件参数
        /// </summary>
        /// <param name="reasonCode">断开原因代码</param>
        /// <param name="exception">引发异常（如果有）</param>
        public DisconnectedEventArgs(DisconnectionReasonCode reasonCode, Exception exception = null)
        {
            Exception = exception;
            ReasonCode = reasonCode;
        }
    }

    /// <summary>
    /// 原因码
    /// </summary>
    public enum DisconnectionReasonCode
    {
        /// <summary>
        /// 主动断开连接
        /// </summary>
        Active,
        /// <summary>
        /// 被动断开连接（远端断开连接）
        /// </summary>
        Passive,
        /// <summary>
        /// 异常导致连接断开
        /// </summary>
        Error,
        /// <summary>
        /// 因超时(本地计时)而断开连接
        /// </summary>
        Timeout,
    }
}
