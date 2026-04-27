using Nethermind.Libp2p.Core;
using System.Buffers;

namespace STTech.BytesIO.P2P
{
    /// <summary>
    /// STTech.BytesIO 字节流传输协议
    /// <para>这是一个基于 Libp2p 的应用层协议，用于在 P2P 节点之间传输原始字节流。</para>
    /// </summary>
    internal class BytesIOProtocol : ISessionProtocol
    {
        /// <summary>
        /// 协议标识符
        /// </summary>
        public string Id => "/sttech/bytesio/1.0.0";

        /// <summary>
        /// 通道已建立时触发
        /// </summary>
        internal event Action<IChannel, ISessionContext, bool>? OnChannelEstablished;

        /// <summary>
        /// 主动拨号方 —— 协商协议后获得 channel
        /// </summary>
        public async Task DialAsync(IChannel downChannel, ISessionContext context)
        {
            var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

            // 通知外部 PeerClient：通道已就绪
            OnChannelEstablished?.Invoke(downChannel, context, true);

            // 保持 channel 活跃，等到 channel 关闭
            await tcs.Task.WaitAsync(downChannel.CancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// 监听方 —— 远端拨入时获得 channel
        /// </summary>
        public async Task ListenAsync(IChannel downChannel, ISessionContext context)
        {
            var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

            // 通知外部 BootstrapServer / PeerClient：有新通道到来
            OnChannelEstablished?.Invoke(downChannel, context, false);

            // 保持 channel 活跃，等到 channel 关闭
            await tcs.Task.WaitAsync(downChannel.CancellationToken).ConfigureAwait(false);
        }
    }
}
