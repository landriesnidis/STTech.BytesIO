using System;

namespace STTech.BytesIO.Core
{
    public class DataReceivedEventArgs : EventArgs
    {
        /// <summary>
        /// 接收到的数据块
        /// </summary>
        public virtual MemoryBlock Data { get; }

        /// <summary>
        /// 帧ID
        /// </summary>
        public uint FrameId { get; }

        protected DataReceivedEventArgs() { }

        [Obsolete]
        public DataReceivedEventArgs(MemoryBlock data) : this(data, 0) { }

        public DataReceivedEventArgs(MemoryBlock data, uint frameId)
        {
            Data = data;
            FrameId = frameId;
        }
    }
}
