using STTech.BytesIO.Core;
using System;
using System.Net;
using System.Net.Quic;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace STTech.BytesIO.Quic.Tests
{
    public class QuicCommunicationTests
    {
        private static int GetRandomFreeUdpPort()
        {
            using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            socket.Bind(new IPEndPoint(IPAddress.Loopback, 0));
            return ((IPEndPoint)socket.LocalEndPoint!).Port;
        }

        [Fact]
        public void Test_Quic_Certificate_SAN_Generation()
        {
            using var cert = QuicCertificateHelper.CreateSelfSignedCertificate("localhost");
            Assert.NotNull(cert);
            Assert.True(cert.HasPrivateKey);

            // 验证 SAN 扩展
            bool hasSan = false;
            foreach (var ext in cert.Extensions)
            {
                if (ext.Oid?.Value == "2.5.29.17") // Subject Alternative Name OID
                {
                    hasSan = true;
                    string rawData = ext.Format(true);
                    Assert.Contains("localhost", rawData, StringComparison.OrdinalIgnoreCase);
                    break;
                }
            }
            Assert.True(hasSan, "证书应包含 RFC 6125 兼容的 Subject Alternative Name (SAN) 扩展");
        }

        [Fact]
        public async Task Test_Quic_Basic_Communication_Loopback()
        {
            if (!QuicConnection.IsSupported || !QuicListener.IsSupported)
            {
                return;
            }

            int port = GetRandomFreeUdpPort();
            using var server = new QuicServer
            {
                Host = "127.0.0.1",
                Port = port
            };

            var serverReceivedTcs = new TaskCompletionSource<byte[]>();
            var clientConnectedTcs = new TaskCompletionSource<QuicClient>();

            server.ClientConnected += (s, e) =>
            {
                clientConnectedTcs.TrySetResult(e.Client);
                e.Client.OnDataReceived += (cs, ce) =>
                {
                    serverReceivedTcs.TrySetResult(ce.Data.ToArray());
                };
            };

            await server.StartAsync();

            using var client = new QuicClient
            {
                Host = "127.0.0.1",
                Port = port,
                AllowUntrustedCertificates = true
            };

            var clientReceivedTcs = new TaskCompletionSource<byte[]>();
            client.OnDataReceived += (s, e) =>
            {
                clientReceivedTcs.TrySetResult(e.Data.ToArray());
            };

            var connectResult = await client.ConnectAsync(new ConnectArgument { Timeout = 5000 });
            Assert.True(connectResult.IsSuccess, $"客户端连接失败: {connectResult.Exception?.Message}");

            // 客户端发送数据
            byte[] clientMsg = Encoding.UTF8.GetBytes("Hello from QuicClient");
            await client.SendAsync(clientMsg);

            // 服务端应收到数据
            var completedTask = await Task.WhenAny(serverReceivedTcs.Task, Task.Delay(5000));
            Assert.Same(serverReceivedTcs.Task, completedTask);
            var serverData = await serverReceivedTcs.Task;
            Assert.Equal("Hello from QuicClient", Encoding.UTF8.GetString(serverData));

            // 服务端拿到连入的客户端后，向客户端回发数据
            var serverSideClient = await clientConnectedTcs.Task;
            Assert.NotNull(serverSideClient);

            // 验证服务端子客户端拿到的远端端口不是写死的 443，而是合法的动态端口
            Assert.Equal("127.0.0.1", serverSideClient.Host);
            Assert.NotEqual(443, serverSideClient.Port);

            byte[] serverReply = Encoding.UTF8.GetBytes("Welcome from QuicServer");
            await serverSideClient.SendAsync(serverReply);

            // 客户端应收到服务端回发的数据
            var clientCompletedTask = await Task.WhenAny(clientReceivedTcs.Task, Task.Delay(5000));
            Assert.Same(clientReceivedTcs.Task, clientCompletedTask);
            var clientData = await clientReceivedTcs.Task;
            Assert.Equal("Welcome from QuicServer", Encoding.UTF8.GetString(clientData));

            // 清理
            await client.DisconnectAsync();
            await server.CloseAsync();
        }

        [Fact]
        public async Task Test_Quic_Dns_Domain_Resolution_And_SNI()
        {
            if (!QuicConnection.IsSupported || !QuicListener.IsSupported)
            {
                return;
            }

            int port = GetRandomFreeUdpPort();
            using var server = new QuicServer
            {
                Host = "127.0.0.1",
                Port = port
            };

            var serverReceivedTcs = new TaskCompletionSource<byte[]>();
            server.ClientConnected += (s, e) =>
            {
                e.Client.OnDataReceived += (cs, ce) =>
                {
                    serverReceivedTcs.TrySetResult(ce.Data.ToArray());
                };
            };

            await server.StartAsync();

            // 测试传入域名 "localhost"，验证不会抛出 FormatException 并能正确握手
            using var client = new QuicClient
            {
                Host = "localhost",
                Port = port,
                AllowUntrustedCertificates = true
            };

            var connectResult = await client.ConnectAsync(new ConnectArgument { Timeout = 5000 });
            Assert.True(connectResult.IsSuccess, $"域名解析连接失败: {connectResult.Exception?.Message}");

            byte[] testData = Encoding.UTF8.GetBytes("Test DNS Resolution");
            await client.SendAsync(testData);

            var completed = await Task.WhenAny(serverReceivedTcs.Task, Task.Delay(5000));
            Assert.Same(serverReceivedTcs.Task, completed);
            var received = await serverReceivedTcs.Task;
            Assert.Equal("Test DNS Resolution", Encoding.UTF8.GetString(received));

            await client.DisconnectAsync();
            await server.CloseAsync();
        }

        [Fact]
        public async Task Test_Quic_Send_Throws_When_Disconnected()
        {
            using var client = new QuicClient
            {
                Host = "127.0.0.1",
                Port = 9999
            };

            // 未连接时发送，应该抛出异常或返回失败 Task，绝不能静默报成功
            await Assert.ThrowsAnyAsync<Exception>(async () =>
            {
                await client.SendAsync(new byte[] { 1, 2, 3 });
            });
        }

        [Fact]
        public async Task Test_Quic_Server_StopAsync_Preserves_Existing_Clients()
        {
            if (!QuicConnection.IsSupported || !QuicListener.IsSupported)
            {
                return;
            }

            int port = GetRandomFreeUdpPort();
            using var server = new QuicServer
            {
                Host = "127.0.0.1",
                Port = port
            };

            var serverReceivedTcs = new TaskCompletionSource<byte[]>();
            server.ClientConnected += (s, e) =>
            {
                e.Client.OnDataReceived += (cs, ce) =>
                {
                    serverReceivedTcs.TrySetResult(ce.Data.ToArray());
                };
            };

            await server.StartAsync();

            using var client = new QuicClient
            {
                Host = "127.0.0.1",
                Port = port,
                AllowUntrustedCertificates = true
            };

            var connectResult = await client.ConnectAsync(new ConnectArgument { Timeout = 5000 });
            Assert.True(connectResult.IsSuccess);

            // 先发一条建立会话
            await client.SendAsync(new byte[] { 0xAA });
            await Task.WhenAny(serverReceivedTcs.Task, Task.Delay(3000));

            // 调用 StopAsync，暂停监听
            await server.StopAsync();
            Assert.False(server.IsListening);
            Assert.True(server.IsPaused);

            // 现有客户端应依然保持连接且能够正常通信
            var secondMessageTcs = new TaskCompletionSource<byte[]>();
            foreach (var sc in server.Clients)
            {
                sc.OnDataReceived += (s, e) => secondMessageTcs.TrySetResult(e.Data.ToArray());
            }

            Assert.True(client.IsConnected);
            await client.SendAsync(new byte[] { 0xBB, 0xCC });

            var completed = await Task.WhenAny(secondMessageTcs.Task, Task.Delay(5000));
            Assert.Same(secondMessageTcs.Task, completed);
            var secondData = await secondMessageTcs.Task;
            Assert.Equal(new byte[] { 0xBB, 0xCC }, secondData);

            await server.CloseAsync();
        }

        [Fact]
        public async Task Test_Quic_Resume_Listening_After_StopAsync()
        {
            if (!QuicConnection.IsSupported || !QuicListener.IsSupported)
            {
                return;
            }

            int port = GetRandomFreeUdpPort();
            using var server = new QuicServer { Host = "127.0.0.1", Port = port };
            await server.StartAsync();
            Assert.True(server.IsListening);

            // 暂停监听
            await server.StopAsync();
            Assert.False(server.IsListening);
            Assert.True(server.IsPaused);

            // 恢复监听：验证能重新启动监听且状态切换正常
            await server.StartAsync();
            Assert.True(server.IsListening);
            Assert.False(server.IsPaused);

            // 新客户端应当能正常接入
            using var client = new QuicClient { Host = "127.0.0.1", Port = port, AllowUntrustedCertificates = true };
            var res = await client.ConnectAsync(new ConnectArgument { Timeout = 5000 });
            Assert.True(res.IsSuccess, $"Failed: {res.Exception}");

            await client.DisconnectAsync();
            await server.CloseAsync();
        }

        [Fact]
        public async Task Test_Quic_MultiStream_Multiplexing_On_Single_Connection()
        {
            if (!QuicConnection.IsSupported || !QuicListener.IsSupported)
            {
                return;
            }

            int port = GetRandomFreeUdpPort();
            using var server = new QuicServer { Host = "127.0.0.1", Port = port };
            await server.StartAsync();

            // 主客户端建立物理连接和首个逻辑流
            using var client = new QuicClient { Host = "127.0.0.1", Port = port, AllowUntrustedCertificates = true };
            var res = await client.ConnectAsync(new ConnectArgument { Timeout = 5000 });
            Assert.True(res.IsSuccess, $"Failed: {res.Exception}");

            // 在同一条物理 QUIC 连接上并发开辟第二条流与第三条流
            using var streamClient2 = await client.CreateStreamClientAsync();
            using var streamClient3 = await client.CreateStreamClientAsync();

            Assert.True(streamClient2.IsConnected);
            Assert.True(streamClient3.IsConnected);
            Assert.Same(client.Connection, streamClient2.Connection);
            Assert.Same(client.Connection, streamClient3.Connection);
            Assert.NotSame(client.Stream, streamClient2.Stream);
            Assert.NotSame(streamClient2.Stream, streamClient3.Stream);

            // 三条流各自独立发送数据
            await client.SendAsync(new byte[] { 0x01 });
            await streamClient2.SendAsync(new byte[] { 0x02 });
            await streamClient3.SendAsync(new byte[] { 0x03 });

            await Task.Delay(500);
            Assert.Equal(3, server.Clients.Length);

            // 关闭第二条流，不应影响主客户端与第三条流
            await streamClient2.DisconnectAsync();
            Assert.False(streamClient2.IsConnected);
            Assert.True(client.IsConnected);
            Assert.True(streamClient3.IsConnected);

            await client.DisconnectAsync();
            await server.CloseAsync();
        }

        [Fact]
        public async Task Test_Quic_Server_Speaks_First_Via_InitialPayload()
        {
            if (!QuicConnection.IsSupported || !QuicListener.IsSupported)
            {
                return;
            }

            int port = GetRandomFreeUdpPort();
            using var server = new QuicServer { Host = "127.0.0.1", Port = port };
            var serverConnectedTcs = new TaskCompletionSource<QuicClient>();
            server.ClientConnected += (s, e) => serverConnectedTcs.TrySetResult(e.Client);
            await server.StartAsync();

            // 客户端配置初始同步载荷，建连时自动发帧激活流
            using var client = new QuicClient
            {
                Host = "127.0.0.1",
                Port = port,
                AllowUntrustedCertificates = true,
                InitialPayload = new byte[] { 0xFE }
            };

            await client.ConnectAsync(new ConnectArgument { Timeout = 5000 });

            // 服务端能在客户端无需额外调用 SendAsync 的情况下直接感知连接
            var completed = await Task.WhenAny(serverConnectedTcs.Task, Task.Delay(3000));
            Assert.Same(serverConnectedTcs.Task, completed);
            var serverClient = await serverConnectedTcs.Task;
            Assert.NotNull(serverClient);

            // 服务端直接先发言
            var clientReceivedTcs = new TaskCompletionSource<byte[]>();
            client.OnDataReceived += (s, e) => clientReceivedTcs.TrySetResult(e.Data.ToArray());

            await serverClient.SendAsync(Encoding.UTF8.GetBytes("Welcome from server first!"));
            var clientCompleted = await Task.WhenAny(clientReceivedTcs.Task, Task.Delay(3000));
            Assert.Same(clientReceivedTcs.Task, clientCompleted);

            await client.DisconnectAsync();
            await server.CloseAsync();
        }

        [Fact]
        public async Task Test_Quic_Server_Speaks_First_Via_ServerInitiatedStreams()
        {
            if (!QuicConnection.IsSupported || !QuicListener.IsSupported)
            {
                return;
            }

            int port = GetRandomFreeUdpPort();
            using var server = new QuicServer
            {
                Host = "127.0.0.1",
                Port = port,
                ServerInitiatedStreams = true, // 服务端主动开辟出站流模式
                ServerInitiatedStreamPayload = Encoding.UTF8.GetBytes("Hello client, server speaks first!")
            };

            var serverConnectedTcs = new TaskCompletionSource<QuicClient>();
            server.ClientConnected += (s, e) => serverConnectedTcs.TrySetResult(e.Client);
            await server.StartAsync();

            using var client = new QuicClient
            {
                Host = "127.0.0.1",
                Port = port,
                AllowUntrustedCertificates = true,
                StreamInitiator = QuicStreamInitiator.Server // 客户端等待服务端主动开流
            };

            var clientReceivedTcs = new TaskCompletionSource<byte[]>();
            client.OnDataReceived += (s, e) => clientReceivedTcs.TrySetResult(e.Data.ToArray());

            var res = await client.ConnectAsync(new ConnectArgument { Timeout = 5000 });
            Assert.True(res.IsSuccess, $"Failed: {res.Exception}");

            // 服务端感知并拿到主动开辟的流客户端
            var serverClient = await serverConnectedTcs.Task;
            Assert.NotNull(serverClient);

            // 服务端率先发送欢迎数据
            await serverClient.SendAsync(Encoding.UTF8.GetBytes("Hello client, server speaks first!"));

            var clientCompleted = await Task.WhenAny(clientReceivedTcs.Task, Task.Delay(3000));
            Assert.Same(clientReceivedTcs.Task, clientCompleted);
            var receivedData = await clientReceivedTcs.Task;
            Assert.Equal("Hello client, server speaks first!", Encoding.UTF8.GetString(receivedData));

            await client.DisconnectAsync();
            await server.CloseAsync();
        }

        [Fact]
        public async Task Test_Quic_MultiClient_Isolation_On_Disconnect()
        {
            if (!QuicConnection.IsSupported || !QuicListener.IsSupported)
            {
                return;
            }

            int port = GetRandomFreeUdpPort();
            using var server = new QuicServer
            {
                Host = "127.0.0.1",
                Port = port
            };

            await server.StartAsync();

            using var client1 = new QuicClient { Host = "127.0.0.1", Port = port, AllowUntrustedCertificates = true };
            using var client2 = new QuicClient { Host = "127.0.0.1", Port = port, AllowUntrustedCertificates = true };

            var conn1 = await client1.ConnectAsync(new ConnectArgument { Timeout = 5000 });
            var conn2 = await client2.ConnectAsync(new ConnectArgument { Timeout = 5000 });
            Assert.True(conn1.IsSuccess);
            Assert.True(conn2.IsSuccess);

            // 两端分别发送一包激活服务端 Stream 管道
            await client1.SendAsync(new byte[] { 0x01 });
            await client2.SendAsync(new byte[] { 0x02 });

            await Task.Delay(500);
            Assert.Equal(2, server.Clients.Length);

            // Client 1 主动断开
            await client1.DisconnectAsync();
            await Task.Delay(500);

            // Client 2 必须依然处于连接状态，且能继续正常发送数据
            Assert.True(client2.IsConnected);
            var client2ReceivedOnServer = new TaskCompletionSource<byte[]>();
            foreach (var sc in server.Clients)
            {
                if (sc.IsConnected)
                {
                    sc.OnDataReceived += (s, e) => client2ReceivedOnServer.TrySetResult(e.Data.ToArray());
                }
            }

            await client2.SendAsync(new byte[] { 0x99 });
            var completed = await Task.WhenAny(client2ReceivedOnServer.Task, Task.Delay(5000));
            Assert.Same(client2ReceivedOnServer.Task, completed);
            var data = await client2ReceivedOnServer.Task;
            Assert.Equal(new byte[] { 0x99 }, data);

            await client2.DisconnectAsync();
            await server.CloseAsync();
        }

        [Fact]
        public async Task Test_Quic_ConnectionAccepted_Event_Fires()
        {
            if (!QuicConnection.IsSupported || !QuicListener.IsSupported)
            {
                return;
            }

            int port = GetRandomFreeUdpPort();
            using var server = new QuicServer
            {
                Host = "127.0.0.1",
                Port = port
            };

            var connectionAcceptedTcs = new TaskCompletionSource<QuicConnectionAcceptedEventArgs>();
            server.ConnectionAccepted += (s, e) =>
            {
                connectionAcceptedTcs.TrySetResult(e);
            };

            await server.StartAsync();

            using var client = new QuicClient
            {
                Host = "127.0.0.1",
                Port = port,
                AllowUntrustedCertificates = true
            };

            var res = await client.ConnectAsync(new ConnectArgument { Timeout = 5000 });
            Assert.True(res.IsSuccess, $"Failed: {res.Exception}");

            // 握手完成后立即触发 ConnectionAccepted，无需等待 stream 发送数据
            var completed = await Task.WhenAny(connectionAcceptedTcs.Task, Task.Delay(5000));
            Assert.Same(connectionAcceptedTcs.Task, completed);
            var acceptedArgs = await connectionAcceptedTcs.Task;
            Assert.NotNull(acceptedArgs.Connection);
            Assert.NotNull(acceptedArgs.RemoteEndPoint);

            await client.DisconnectAsync();
            await server.CloseAsync();
        }

        [Fact]
        public async Task Test_Quic_PauseTime_Execution()
        {
            if (!QuicConnection.IsSupported || !QuicListener.IsSupported)
            {
                return;
            }

            int port = GetRandomFreeUdpPort();
            using var server = new QuicServer { Host = "127.0.0.1", Port = port };
            await server.StartAsync();

            using var client = new QuicClient { Host = "127.0.0.1", Port = port, AllowUntrustedCertificates = true };
            await client.ConnectAsync(new ConnectArgument { Timeout = 5000 });

            var sw = System.Diagnostics.Stopwatch.StartNew();
            await client.SendAsync(new byte[] { 1, 2, 3 }, new SendOptions { PauseTime = 300 });
            sw.Stop();

            Assert.True(sw.ElapsedMilliseconds >= 250, $"PauseTime 应至少延迟 ~300ms，实际耗时: {sw.ElapsedMilliseconds}ms");

            await client.DisconnectAsync();
            await server.CloseAsync();
        }

        [Fact]
        public async Task Test_Quic_Send_With_Cancelled_Token_Throws()
        {
            if (!QuicConnection.IsSupported || !QuicListener.IsSupported)
            {
                return;
            }

            int port = GetRandomFreeUdpPort();
            using var server = new QuicServer { Host = "127.0.0.1", Port = port };
            await server.StartAsync();

            using var client = new QuicClient { Host = "127.0.0.1", Port = port, AllowUntrustedCertificates = true };
            await client.ConnectAsync(new ConnectArgument { Timeout = 5000 });

            using var cts = new CancellationTokenSource();
            cts.Cancel(); // 立即取消

            await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            {
                await client.SendAsync(new byte[] { 1, 2, 3 }, new SendOptions { CancellationToken = cts.Token });
            });

            await client.DisconnectAsync();
            await server.CloseAsync();
        }
    }
}
