using System;

namespace STTech.BytesIO.Core
{
    public class DataReceivedEventArgs : EventArgs
    {
        /// <summary>
        /// 接收到的数据上下文
        /// </summary>
        public virtual ReceiveContext Data { get; }

        /// <summary>
        /// 帧ID
        /// </summary>
        public uint FrameId { get; }

        protected DataReceivedEventArgs() { }

        /// <summary>
        /// 构造数据已接收事件参数
        /// </summary>
        /// <param name="data">数据上下文</param>
        /// <param name="frameId">帧 ID</param>
        public DataReceivedEventArgs(ReceiveContext data, uint frameId)
        {
            Data = data;
            FrameId = frameId;
        }
    }
}
