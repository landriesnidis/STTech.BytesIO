using System;

namespace STTech.BytesIO.Core
{
    public class DataReceivedEventArgs : EventArgs
    {
        /// <summary>
        /// 接收到的数据
        /// </summary>
        public virtual byte[] Data { get; }

        protected DataReceivedEventArgs() { }

        public DataReceivedEventArgs(byte[] data)
        {
            Data = data;
        }
    }
}
