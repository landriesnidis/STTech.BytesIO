using System;
using System.Threading.Tasks;

namespace STTech.BytesIO.Core
{
    /// <summary>
    /// 发送参数
    /// </summary>
    public class SendArgs
    {
        /// <summary>
        /// 构造发送参数
        /// </summary>
        /// <param name="data">发送数据</param>
        /// <param name="options">发送选项</param>
        public SendArgs(byte[] data, SendOptions options)
            : this(data, options, null)
        {
        }

        /// <summary>
        /// 构造发送参数
        /// </summary>
        /// <param name="data">发送数据</param>
        /// <param name="options">发送选项</param>
        /// <param name="tcs">任务完成源</param>
        public SendArgs(byte[] data, SendOptions options, TaskCompletionSource<bool> tcs)
        {
            Data = data;
            Options = options;
            Tcs = tcs;
            EnqueueTime = DateTime.Now;
        }

        /// <summary>
        /// 待发送数据
        /// </summary>
        public byte[] Data { get; }

        /// <summary>
        /// 发送选项
        /// </summary>
        public SendOptions Options { get; }

        /// <summary>
        /// 任务完成源
        /// </summary>
        public TaskCompletionSource<bool> Tcs { get; }

        /// <summary>
        /// 记录该包进入队列的初始化时间戳，以便于在 AsyncPump 发送前验证存活 TTL 过期时间
        /// </summary>
        public DateTime EnqueueTime { get; }
    }
}
