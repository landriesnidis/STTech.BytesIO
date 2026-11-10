using System;

namespace STTech.BytesIO.Core
{
    /// <summary>
    /// 客户端连接成功事件参数
    /// </summary>
    public class ConnectedSuccessfullyEventArgs : EventArgs
    {
        /// <summary>
        /// 该连接会话在库内的全局唯一标识。
        /// </summary>
        public string ConnectionId { get; set; }

        /// <summary>
        /// 联机所耗费的微秒级别时间。
        /// </summary>
        public TimeSpan CostTime { get; set; }

        /// <summary>
        /// 由调用 ConnectAsync 时传入的使用者自定义附加穿越状态（Context）。
        /// </summary>
        public object State { get; set; }

        /// <summary>
        /// 初始化 <see cref="ConnectedSuccessfullyEventArgs"/> 类的新实例。
        /// </summary>
        public ConnectedSuccessfullyEventArgs()
        {
        }
    }
}
