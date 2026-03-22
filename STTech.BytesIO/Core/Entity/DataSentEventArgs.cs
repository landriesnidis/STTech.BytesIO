using System;

namespace STTech.BytesIO.Core
{
    /// <summary>
    /// 数据已发送事件参数
    /// </summary>
    public class DataSentEventArgs : DataSentEventArgs<byte[]>
    {
        /// <summary>
        /// 构造数据已发送事件参数
        /// </summary>
        /// <param name="data">发送的数据</param>
        public DataSentEventArgs(byte[] data) : base(data)
        {
        }
    }

    /// <summary>
    /// 数据已发送事件参数
    /// </summary>
    /// <typeparam name="T">发送数据的类型</typeparam>
    public class DataSentEventArgs<T> : EventArgs
    {
        /// <summary>
        /// 发送出的数据
        /// </summary>
        public T Data { get; }

        /// <summary>
        /// 构造数据已发送事件参数
        /// </summary>
        /// <param name="data">发送的数据</param>
        public DataSentEventArgs(T data)
        {
            Data = data;
        }
    }
}
