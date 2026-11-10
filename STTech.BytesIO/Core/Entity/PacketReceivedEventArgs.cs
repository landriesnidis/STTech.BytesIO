using System;
using System.Text;

namespace STTech.BytesIO.Core
{

    /// <summary>
    /// 数据包接收事件参数
    /// </summary>
    /// <typeparam name="T">数据包类型</typeparam>
    public class PacketReceivedEventArgs<T> : EventArgs
    {
        /// <summary>
        /// 获取接收到的数据包实体
        /// </summary>
        public T Data { get; }

        /// <summary>
        /// 初始化 <see cref="PacketReceivedEventArgs{T}"/> 类的新实例。
        /// </summary>
        /// <param name="data">数据实体</param>
        public PacketReceivedEventArgs(T data)
        {
            Data = data;
        }
    }
}
