using STTech.BytesIO.Core.Entity;
using System;

namespace STTech.BytesIO.Core
{
    /// <summary>
    /// 数据包接受完成事件委托
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public delegate void PacketReceivedHandler<T>(object sender, PacketReceivedEventArgs<T> e);

    /// <summary>
    /// 数据包接收器
    /// </summary>
    /// <typeparam name="T">指定类型</typeparam>
    public interface IPacketReceiver<T>
    {
        /// <summary>
        /// 在接收到经模块管道处理过的数据包时发生
        /// </summary>
        public event PacketReceivedHandler<T> OnPacketReceived;

        /// <summary>
        /// 执行数据包接受完成的响应事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void RaisePacketReceived(object sender, PacketReceivedEventArgs<T> e);
    }
}
