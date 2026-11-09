using STTech.BytesIO.Core;
using System;
using System.Collections.Generic;
using System.Net.Quic;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace STTech.BytesIO.Quic
{
    /// <summary>
    /// QUIC 服务器接口
    /// </summary>
    public interface IQuicServer : IBytesServer
    {
        /// <summary>
        /// 监听地址
        /// </summary>
        string Host { get; set; }

        /// <summary>
        /// 监听端口
        /// </summary>
        int Port { get; set; }

        /// <summary>
        /// 应用层协议协商 (ALPN) 列表
        /// </summary>
        List<SslApplicationProtocol> AlpnProtocols { get; set; }

        /// <summary>
        /// 服务器证书
        /// </summary>
        X509Certificate2? ServerCertificate { get; set; }

        /// <summary>
        /// 服务端 TLS 身份认证选项生成回调（支持根据 SNI 路由不同证书及 mTLS 双向认证配置）
        /// </summary>
        Func<QuicConnection, SslClientHelloInfo, SslServerAuthenticationOptions>? ServerAuthenticationOptionsCallback { get; set; }

        /// <summary>
        /// 每个物理连接允许的最大入站双向流数量
        /// </summary>
        int MaxInboundBidirectionalStreams { get; set; }

        /// <summary>
        /// 每个物理连接允许的最大入站单向流数量
        /// </summary>
        int MaxInboundUnidirectionalStreams { get; set; }

        /// <summary>
        /// 连接空闲超时时间
        /// </summary>
        TimeSpan? IdleTimeout { get; set; }

        /// <summary>
        /// 服务端在物理连接建立后是否主动开辟出站流（适用于“服务端先发言/下发问候令牌”模式）
        /// </summary>
        bool ServerInitiatedStreams { get; set; }

        /// <summary>
        /// 是否接受入站单向流（默认 false，仅接受双向流）
        /// </summary>
        bool AcceptUnidirectionalStreams { get; set; }

        /// <summary>
        /// 当物理 QUIC 连接建立时触发（在接受具体通信流之前）
        /// </summary>
        event EventHandler<QuicConnectionAcceptedEventArgs>? ConnectionAccepted;
    }
}
