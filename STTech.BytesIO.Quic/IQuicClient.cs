using STTech.BytesIO.Core;
using System.Net.Quic;
using System.Net.Security;

namespace STTech.BytesIO.Quic
{
    /// <summary>
    /// QUIC 客户端接口
    /// </summary>
    public interface IQuicClient : IBytesClient
    {
        /// <summary>
        /// 远程主机
        /// </summary>
        string Host { get; set; }

        /// <summary>
        /// 远程端口
        /// </summary>
        int Port { get; set; }

        /// <summary>
        /// 应用层协议协商 (ALPN)
        /// </summary>
        List<SslApplicationProtocol> AlpnProtocols { get; set; }

        /// <summary>
        /// 客户端连接选项
        /// </summary>
        QuicClientConnectionOptions ClientConnectionOptions { get; set; }
    }
}
