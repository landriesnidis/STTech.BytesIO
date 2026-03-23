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
        /// 生成用于测试的自签名证书
        /// </summary>
        /// <param name="subjectName">主题名称 (默认: localhost)</param>
        /// <returns></returns>
        public static X509Certificate2 CreateSelfSignedCertificate(string subjectName = "localhost")
        {
            using var rsa = RSA.Create(2048);
            var request = new CertificateRequest($"CN={subjectName}", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

            request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature, true));
            request.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(new OidCollection { new Oid("1.3.6.1.5.5.7.3.1") }, false)); // Server Auth

            var certificate = request.CreateSelfSigned(DateTimeOffset.Now, DateTimeOffset.Now.AddYears(1));

            // 注意：在 Windows 上，证书必须包含私钥，并且通常需要 Export 后重新 Import 才能被 msquic 正确识别
            return new X509Certificate2(certificate.Export(X509ContentType.Pfx), (string?)null, X509KeyStorageFlags.Exportable);
        }
    }
}
