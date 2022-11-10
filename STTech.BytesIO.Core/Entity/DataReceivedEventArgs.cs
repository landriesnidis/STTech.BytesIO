using System;

namespace STTech.BytesIO.Core
{
    public class DataReceivedEventArgs : EventArgs
    {
        /// <summary>
        /// 接收到的数据
        /// </summary>
        public virtual byte[] Data { get; }

        /// <summary>
        /// 帧ID
        /// </summary>
        public uint FrameId { get; }

        protected DataReceivedEventArgs() { }

        [Obsolete]
        public DataReceivedEventArgs(byte[] data) : this(data, 0) { }

        public DataReceivedEventArgs(byte[] data, uint frameId)
        {
            Data = data;
            FrameId = frameId;
        }
    }
}
