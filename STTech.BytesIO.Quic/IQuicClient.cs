using STTech.BytesIO.Core;
using System.Collections.Generic;
using System.Net.Quic;
using System.Net.Security;
using System.Threading;
using System.Threading.Tasks;

namespace STTech.BytesIO.Quic
{
    /// <summary>
    /// QUIC 客户端接口
    /// </summary>
    public interface IQuicClient : IBytesClient
    {
        /// <summary>
        /// 远程主机（支持 IPv4、IPv6 及域名）
        /// </summary>
        string Host { get; set; }

        /// <summary>
        /// 远程端口
        /// </summary>
        int Port { get; set; }

        /// <summary>
        /// SNI (Server Name Indication) 目标主机名。若未显式指定，默认采用 <see cref="Host"/>。
        /// </summary>
        string? TargetHost { get; set; }

        /// <summary>
        /// 是否允许不受信任的服务器证书（自签名证书测试或内部环境使用，生产环境建议保持 false）
        /// </summary>
        bool AllowUntrustedCertificates { get; set; }

        /// <summary>
        /// 自定义远程证书验证回调
        /// </summary>
        RemoteCertificateValidationCallback? RemoteCertificateValidationCallback { get; set; }

        /// <summary>
        /// 应用层协议协商 (ALPN) 列表
        /// </summary>
        List<SslApplicationProtocol> AlpnProtocols { get; set; }

        /// <summary>
        /// 客户端连接选项模板
        /// </summary>
        QuicClientConnectionOptions ClientConnectionOptions { get; set; }

        /// <summary>
        /// 初始通信流的发起方式（默认为 Client 模式）
        /// </summary>
        QuicStreamInitiator StreamInitiator { get; set; }

        /// <summary>
        /// 连接建立后自动发送的初始握手/同步载荷（用于打通双向流并唤醒服务端）
        /// </summary>
        byte[]? InitialPayload { get; set; }

        /// <summary>
        /// 当前通信流类型（双向流或单向流）
        /// </summary>
        QuicStreamType StreamType { get; }

        /// <summary>
        /// 当前通信流是否支持写入数据
        /// </summary>
        bool CanWrite { get; }

        /// <summary>
        /// 当前通信流是否支持读取数据
        /// </summary>
        bool CanRead { get; }

        /// <summary>
        /// 在当前的物理 QUIC 连接上创建一个新的逻辑流客户端（多流复用）
        /// </summary>
        /// <param name="type">流类型（默认双向流）</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>新的 QUIC 流客户端实例</returns>
        Task<QuicClient> CreateStreamClientAsync(QuicStreamType type = QuicStreamType.Bidirectional, CancellationToken cancellationToken = default);
    }
}
