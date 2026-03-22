using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace STTech.BytesIO.Core
{
    /// <summary>
    /// 发送选项
    /// </summary>
    public class SendOptions
    {
        /// <summary>
        /// 停顿时长 (发送完成后的补偿挂起时长)，默认 0 毫秒。
        /// </summary>
        public int PauseTime { get; set; } = 0;

        /// <summary>
        /// 发送超时 (TTL)：此数据包在队列堆积期间如果超过此时长(毫秒)仍未被底层物理发出，
        /// 则直接抛弃并引发 TimeoutException。0 代表永不过期。
        /// </summary>
        public int ExpireTimeout { get; set; } = 0;

        /// <summary>
        /// 取消令牌：允许外部调用取消该独立发包任务。当且仅当该包尚未被底层发出时有效。
        /// </summary>
        public CancellationToken CancellationToken { get; set; } = CancellationToken.None;

        /// <summary>
        /// 允许网卡缓存黏连批处理：如果设为 true (默认)，则在发包后立刻强制 Flush 直达网卡（高实时性）。
        /// 如果设为 false，允许内核决定发送时机，可能导致多个包合并发送以换取极高吞吐量。
        /// </summary>
        public bool FlushImmediately { get; set; } = true;
    }
}
