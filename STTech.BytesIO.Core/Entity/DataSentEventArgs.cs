using System;

namespace STTech.BytesIO.Core
{
    public class DataSentEventArgs : DataSentEventArgs<byte[]>
    {
        public DataSentEventArgs(byte[] data) : base(data)
        {
        }
    }

    public class DataSentEventArgs<T> : EventArgs
    {
        /// <summary>
        /// 发送出的数据
        /// </summary>
        public T Data { get; }

        public DataSentEventArgs(T data)
        {
            Data = data;
        }
    }
}
