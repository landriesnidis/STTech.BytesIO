using STTech.BytesIO.Tcp.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace STTech.BytesIO.Tcp
{
    // ===============================================================================
    // 
    //                                  SSL加密通信实现部分
    // 
    // ===============================================================================
    public partial class TcpClient : ITcpSSL
    {
        /// <summary>
        /// TLS通信验证通过委托事件
        /// </summary>
        public event TlsVerifySuccessfullyHandler OnTlsVerifySuccessfully;

        /// <summary>
        /// 启用SSL通信
        /// </summary>
        public bool UseSsl { get; set; }

        /// <summary>
        /// 服务端证书名称
        /// </summary>
        public string ServerCertificateName { get; set; }

        /// <summary>
        /// 证书文件
        /// </summary>
        public X509Certificate Certificate { get; set; }

        /// <summary>
        /// Tls协议版本
        /// 默认为Tls1.2
        /// </summary>
        public SslProtocols SslProtocol { get; set; } = SslProtocols.Tls12;

        /// <summary>
        /// SSL通信流
        /// </summary>
        public SslStream SslStream { get; private set; }

        /// <summary>
        /// 远端证书验证委托
        /// </summary>
        public RemoteCertificateValidationCallback RemoteCertificateValidationHandle { get; set; }

        /// <summary>
        /// 本地证书验证委托
        /// </summary>
        public LocalCertificateSelectionCallback LocalCertificateSelectionHandle { get; set; }

        /// <summary>
        /// 执行TLS通信验证通过事件的委托回调
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void PerformTlsVerifySuccessfully(object sender, TlsVerifySuccessfullyEventArgs e)
        {
            SafelyInvokeCallback(() => { OnTlsVerifySuccessfully?.Invoke(sender, e); });
        }

        /// <summary>
        /// 验证远端证书
        /// 关于安全策略错误的枚举，可以参考文档：https://docs.microsoft.com/zh-cn/dotnet/api/system.net.security.sslpolicyerrors
        /// </summary>
        /// <returns>验证是否通过</returns>
        protected virtual bool RemoteCertificateValidateCallback(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            //if (sslPolicyErrors == SslPolicyErrors.None)
            //    return true;
            //if (sslPolicyErrors == SslPolicyErrors.RemoteCertificateChainErrors)
            //    return true;
            return true;
        }

        /// <summary>
        /// 选择用于身份验证的本地安全套接字层 (SSL) 证书
        /// 关于LocalCertificateSelectionCallback委托的文档：https://docs.microsoft.com/zh-cn/dotnet/api/system.net.security.localcertificateselectioncallback?view=netstandard-2.0
        /// </summary>
        /// <param name="sender">此验证的状态信息</param>
        /// <param name="targetHost">客户端指定的主机服务器</param>
        /// <param name="localCertificates">包含本地证书</param>
        /// <param name="remoteCertificate">用于对远程方进行身份验证的证书</param>
        /// <param name="acceptableIssuers">远程方可接受的证书颁发者的 String 数组</param>
        /// <returns></returns>
        public X509Certificate LocalCertificateSelectionCallback(object sender, string targetHost, X509CertificateCollection localCertificates, X509Certificate remoteCertificate, string[] acceptableIssuers)
        {
            return Certificate;
        }
    }
}
