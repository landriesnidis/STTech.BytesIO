using System;

namespace STTech.BytesIO.Core
{
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

        public ConnectedSuccessfullyEventArgs()
        {
        }
    }
}
