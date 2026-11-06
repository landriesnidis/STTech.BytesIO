using STTech.BytesIO.Core;
using System.Threading.Tasks;

namespace STTech.BytesIO.P2P
{
    /// <summary>
    /// P2P 客户端接口
    /// </summary>
    public interface IPeerClient
    {
        /// <summary>
        /// 远程节点的 Multiaddr 地址
        /// <para>示例: /ip4/127.0.0.1/tcp/4001/p2p/QmXxx...</para>
        /// </summary>
        string RemoteAddress { get; set; }

        /// <summary>
        /// 本地节点的 PeerId
        /// </summary>
        string? LocalPeerId { get; }

        /// <summary>
        /// 远程节点的 PeerId
        /// </summary>
        string? RemotePeerId { get; }

        /// <summary>
        /// P2P 节点配置选项
        /// </summary>
        P2PNodeOptions Options { get; set; }

        /// <summary>
        /// 通过 Multiaddr 地址异步连接目标对等节点
        /// </summary>
        /// <param name="remoteAddress">目标节点的 Multiaddr 字符串</param>
        /// <param name="timeout">连接超时时间（毫秒），默认 5000ms</param>
        Task<ConnectResult> ConnectAsync(string remoteAddress, int timeout = 5000);
    }
}
