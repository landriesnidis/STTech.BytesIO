using STTech.BytesIO.Tcp.Entity;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

namespace STTech.BytesIO.Tcp
{
    /// <summary>
    /// TLS通信验证通过委托类型
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public delegate void TlsVerifySuccessfullyHandler(object sender, TlsVerifySuccessfullyEventArgs e);

    public interface ITcpSSL
    {
        /// <summary>
        /// TLS验证成功事件
        /// </summary>
        event TlsVerifySuccessfullyHandler OnTlsVerifySuccessfully;

        /// <summary>
        /// 启用SSL通信
        /// </summary>
        public bool UseSsl { get; set; }

        /// <summary>
        /// 服务端证书名称
        /// </summary>
        string ServerCertificateName { get; set; }

        /// <summary>
        /// 证书文件
        /// </summary>
        X509Certificate Certificate { get; set; }

        /// <summary>
        /// Tls协议版本
        /// 默认为Tls1.2
        /// </summary>
        SslProtocols SslProtocol { get; set; } 

        /// <summary>
        /// SSL通信流
        /// </summary>
        SslStream SslStream { get;  }


        /// <summary>
        /// 触发TLS通信验证通过事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void PerformTlsVerifySuccessfully(object sender, TlsVerifySuccessfullyEventArgs e);
    }


}
