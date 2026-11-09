using STTech.BytesIO.Tcp;
using System;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace STTech.BytesIO.Tcp.Tests
{
    public class TcpSslTests
    {
        private static X509Certificate2 GenerateSelfSignedCertificate(string hostName = "localhost")
        {
            using var rsa = RSA.Create(2048);
            var req = new CertificateRequest($"CN={hostName}", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            var cert = req.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1));
            // 导出并重新导入以包含可导出的私钥
            return new X509Certificate2(cert.Export(X509ContentType.Pfx));
        }

        [Fact]
        public async Task TcpSsl_EndToEnd_SecureCommunication_ShouldSucceed()
        {
            using var cert = GenerateSelfSignedCertificate("localhost");
            int port = PortHelper.GetAvailablePort();

            var server = new TcpServer
            {
                Port = port,
                UseSsl = true,
                Certificate = cert,
                ServerCertificateName = "localhost",
                SslProtocol = SslProtocols.Tls12
            };

            var serverReceivedTcs = new TaskCompletionSource<byte[]>();
            var serverConnectedTcs = new TaskCompletionSource<bool>();
            Exception serverEx = null;
            server.OnExceptionOccurs += (s, e) => serverEx = e.Exception;
            server.ClientConnected += (s, e) =>
            {
                serverConnectedTcs.TrySetResult(true);
                e.Client.OnDataReceived += (s2, e2) => serverReceivedTcs.TrySetResult(e2.Data.ToArray());
            };

            await server.StartAsync();

            try
            {
                var client = new TcpClient
                {
                    Host = "127.0.0.1",
                    Port = port,
                    UseSsl = true,
                    ServerCertificateName = "localhost",
                    SslProtocol = SslProtocols.Tls12,
                    // 接受测试自签名证书
                    RemoteCertificateValidationHandle = (sender, certificate, chain, errors) => true
                };

                var connectResult = client.Connect(10000);
                Assert.True(connectResult.IsSuccess, $"SSL 连接应该成功: {connectResult.ErrorCode}, ex: {connectResult.Exception}");
                Assert.True(client.IsConnected);
                Assert.NotNull(client.SslStream);

                // 等待服务端也完成异步 TLS 握手并加入客户端池
                var completedServer = await Task.WhenAny(serverConnectedTcs.Task, Task.Delay(5000));
                Assert.True(completedServer == serverConnectedTcs.Task, $"服务端握手超时未就绪，服务端异常: {serverEx}");

                byte[] sendData = Encoding.UTF8.GetBytes("Encrypted Hello over TLS");
                client.Send(sendData);

                var completed = await Task.WhenAny(serverReceivedTcs.Task, Task.Delay(5000));
                Assert.Same(serverReceivedTcs.Task, completed);
                byte[] serverData = await serverReceivedTcs.Task;
                Assert.Equal(sendData, serverData);

                client.Disconnect();
            }
            finally
            {
                await server.CloseAsync();
            }
        }

        [Fact]
        public async Task TcpSsl_WhenCertificateRejected_ConnectionShouldFail()
        {
            using var cert = GenerateSelfSignedCertificate("localhost");
            int port = PortHelper.GetAvailablePort();

            var server = new TcpServer
            {
                Port = port,
                UseSsl = true,
                Certificate = cert,
                ServerCertificateName = "localhost",
                SslProtocol = SslProtocols.Tls12
            };

            await server.StartAsync();

            try
            {
                var client = new TcpClient
                {
                    Host = "127.0.0.1",
                    Port = port,
                    UseSsl = true,
                    ServerCertificateName = "localhost",
                    SslProtocol = SslProtocols.Tls12,
                    // 故意拒绝证书验证
                    RemoteCertificateValidationHandle = (sender, certificate, chain, errors) => false
                };

                var connectResult = client.Connect(5000);
                Assert.False(connectResult.IsSuccess, "当证书校验拒绝时，连接应当失败");
            }
            finally
            {
                await server.CloseAsync();
            }
        }
    }
}
