using System;

namespace STTech.BytesIO.Core.Entity
{
    public class DisconnectedEventArgs : EventArgs
    {
        /// <summary>
        /// 是否是主动断开的连接
        /// </summary>
        public bool IsActively => ReasonCode == DisconnectionReasonCode.Active || ReasonCode == DisconnectionReasonCode.Timeout;

        /// <summary>
        /// 断开连接的原因
        /// </summary>
        public DisconnectionReasonCode ReasonCode { get; set; }
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
