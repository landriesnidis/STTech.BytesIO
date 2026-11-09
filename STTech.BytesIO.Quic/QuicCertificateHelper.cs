using System;
using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace STTech.BytesIO.Quic
{
    /// <summary>
    /// QUIC 证书辅助类
    /// </summary>
    public static class QuicCertificateHelper
    {
        /// <summary>
        /// 生成用于测试与开发的自签名证书（遵循 RFC 6125 标准，内置 Subject Alternative Name）
        /// </summary>
        /// <param name="subjectName">主题名称 (默认: localhost)</param>
        /// <returns>包含私钥的 X509 证书实例</returns>
        public static X509Certificate2 CreateSelfSignedCertificate(string subjectName = "localhost")
        {
            using var rsa = RSA.Create(2048);
            var request = new CertificateRequest($"CN={subjectName}", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

            request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature, true));
            request.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(new OidCollection { new Oid("1.3.6.1.5.5.7.3.1") }, false)); // Server Auth

            // 补充 SAN (Subject Alternative Name) 扩展，确保现代 TLS 1.3 客户端验证通过
            var sanBuilder = new SubjectAlternativeNameBuilder();
            if (IPAddress.TryParse(subjectName, out var ip))
            {
                sanBuilder.AddIpAddress(ip);
            }
            else
            {
                sanBuilder.AddDnsName(subjectName);
            }

            if (subjectName.Equals("localhost", StringComparison.OrdinalIgnoreCase))
            {
                sanBuilder.AddDnsName("localhost");
                sanBuilder.AddIpAddress(IPAddress.Loopback);
                sanBuilder.AddIpAddress(IPAddress.IPv6Loopback);
            }

            request.CertificateExtensions.Add(sanBuilder.Build());

            var certificate = request.CreateSelfSigned(DateTimeOffset.Now.AddDays(-1), DateTimeOffset.Now.AddYears(1));

            // 注意：在 Windows 上，证书必须包含私钥，并且通常需要 Export 后重新 Import 才能被 msquic 正确识别
#if NET9_0_OR_GREATER
            return X509CertificateLoader.LoadPkcs12(certificate.Export(X509ContentType.Pfx), (string?)null, X509KeyStorageFlags.Exportable);
#else
            return new X509Certificate2(certificate.Export(X509ContentType.Pfx), (string?)null, X509KeyStorageFlags.Exportable);
#endif
        }
    }
}
