using System;

namespace STTech.BytesIO.Core.Entity
{
    public class DataReceivedEventArgs : DataReceivedEventArgs<byte[]>
    {
        public DataReceivedEventArgs(byte[] data) : base(data)
        {
        }
    }

    public class DataReceivedEventArgs<T> : EventArgs
    {
        /// <summary>
        /// 接收到的数据
        /// </summary>
        public T Data { get; }

        public DataReceivedEventArgs(T data)
        {
            Data = data;
        }
    }
}
