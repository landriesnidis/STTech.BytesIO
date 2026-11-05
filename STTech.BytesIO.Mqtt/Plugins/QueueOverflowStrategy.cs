using System;

namespace STTech.BytesIO.Mqtt
{
    /// <summary>
    /// 队列溢出处理策略。
    /// </summary>
    public enum QueueOverflowStrategy
    {
        /// <summary>
        /// 丢弃队列中最旧的消息以腾出空间。
        /// </summary>
        DropOldest,

        /// <summary>
        /// 丢弃当前最新的消息（拒绝当前消息入队）。
        /// </summary>
        DropNewest,

        /// <summary>
        /// 抛出 InvalidOperationException 异常拒绝入队。
        /// </summary>
        ThrowException,

        /// <summary>
        /// 阻塞等待队列腾出空位（受超时控制）。
        /// </summary>
        Block
    }

    /// <summary>
    /// 队列溢出事件参数。
    /// </summary>
    public class MqttQueueOverflowEventArgs : EventArgs
    {
        /// <summary>
        /// 被丢弃的消息（若策略为丢弃）。
        /// </summary>
        public MqttApplicationMessage DroppedMessage { get; }

        /// <summary>
        /// 溢出策略。
        /// </summary>
        public QueueOverflowStrategy Strategy { get; }

        /// <summary>
        /// 构造队列溢出事件参数。
        /// </summary>
        public MqttQueueOverflowEventArgs(MqttApplicationMessage droppedMessage, QueueOverflowStrategy strategy)
        {
            DroppedMessage = droppedMessage;
            Strategy = strategy;
        }
    }
}
