using STTech.BytesIO.Core;
using STTech.BytesIO.P2P;
using Nethermind.Libp2p;
using Multiformats.Address;
using System;
using System.Buffers;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace STTech.BytesIO.P2P.Tests
{
    public class P2PCommunicationTests
    {
        private readonly ITestOutputHelper _output;

        public P2PCommunicationTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public async Task TestDirectTwoWayCommunication()
        {
            var protocol1 = new BytesIOProtocol();
            var sp1 = PeerClient.BuildServiceProvider(new P2PNodeOptions(), protocol1);
            var peerFactory1 = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<Nethermind.Libp2p.Core.IPeerFactory>(sp1);
            var peer1 = peerFactory1.Create();

            await peer1.StartListenAsync(new Multiaddress[] { $"/ip4/127.0.0.1/tcp/48888" }, CancellationToken.None);

            var protocol2 = new BytesIOProtocol();
            var sp2 = PeerClient.BuildServiceProvider(new P2PNodeOptions(), protocol2);
            var peerFactory2 = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<Nethermind.Libp2p.Core.IPeerFactory>(sp2);
            var peer2 = peerFactory2.Create();

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            try
            {
                var ch1Tcs = new TaskCompletionSource<Nethermind.Libp2p.Core.IChannel>();
                protocol1.OnChannelEstablished += (ch, ctx, isDialer) =>
                {
                    ch1Tcs.TrySetResult(ch);
                };

                var ch2Tcs = new TaskCompletionSource<Nethermind.Libp2p.Core.IChannel>();
                protocol2.OnChannelEstablished += (ch, ctx, isDialer) =>
                {
                    ch2Tcs.TrySetResult(ch);
                };

                Multiaddress targetAddr = peer1.ListenAddresses.First();
                var session = await peer2.DialAsync(targetAddr, cts.Token);
                Assert.NotNull(session);

                var protoDialTask = session.DialAsync<BytesIOProtocol>(cts.Token);

                await Task.WhenAll(ch1Tcs.Task, ch2Tcs.Task);
                var ch1 = await ch1Tcs.Task;
                var ch2 = await ch2Tcs.Task;

                _output.WriteLine("Channels established! Testing data transmission...");

                // 从 Peer2 发送数据给 Peer1
                byte[] sendFrom2 = Encoding.UTF8.GetBytes("Hello from Peer2");
                await ch2.WriteAsync(new System.Buffers.ReadOnlySequence<byte>(sendFrom2));

                byte[]? receivedBy1 = null;
                await foreach (var data in ch1.ReadAllAsync(cts.Token))
                {
                    receivedBy1 = data.ToArray();
                    break;
                }
                Assert.Equal(sendFrom2, receivedBy1);

                // 从 Peer1 发送数据给 Peer2
                byte[] sendFrom1 = Encoding.UTF8.GetBytes("Hello from Peer1");
                await ch1.WriteAsync(new System.Buffers.ReadOnlySequence<byte>(sendFrom1));

                byte[]? receivedBy2 = null;
                await foreach (var data in ch2.ReadAllAsync(cts.Token))
                {
                    receivedBy2 = data.ToArray();
                    break;
                }
                Assert.Equal(sendFrom1, receivedBy2);

                _output.WriteLine("Two-way communication succeeded!");
            }
            finally
            {
                await peer1.DisposeAsync();
                await peer2.DisposeAsync();
            }
        }

        [Fact]
        public async Task TestP2PClientToServerCommunication()
        {
            _output.WriteLine("Step 1: Starting server");
            var server = new BootstrapServer();
            server.Options.ListenAddress = "127.0.0.1";
            server.Options.TcpPort = 48889;

            var serverReceivedTcs = new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);
            var clientReceivedTcs = new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);
            var serverConnectedTcs = new TaskCompletionSource<PeerClient>(TaskCreationOptions.RunContinuationsAsynchronously);

            server.PeerConnected += (s, e) =>
            {
                _output.WriteLine($"Server: PeerConnected event fired! Client PeerId: {e.Client.RemotePeerId}");
                serverConnectedTcs.TrySetResult(e.Client);
                e.Client.OnDataReceived += (s2, e2) =>
                {
                    serverReceivedTcs.TrySetResult(e2.Data.ToArray());
                };
            };

            await server.StartAsync();

            try
            {
                Assert.NotNull(server.LocalPeerId);
                Assert.NotEmpty(server.ListenAddresses);

                var client = new PeerClient();
                client.OnDataReceived += (s, e) =>
                {
                    clientReceivedTcs.TrySetResult(e.Data.ToArray());
                };

                // 测试便捷重载 ConnectAsync(string)
                var connectResult = await client.ConnectAsync(server.ListenAddresses.First(), 10000);
                Assert.True(connectResult.IsSuccess, $"Client connection failed: {connectResult.ErrorCode}, ex: {connectResult.Exception}");
                Assert.True(client.IsConnected, "Client should be connected");

                var acceptedClient = await serverConnectedTcs.Task.WaitAsync(TimeSpan.FromSeconds(5));
                Assert.NotNull(acceptedClient);
                Assert.True(acceptedClient.IsConnected, "Server-side accepted client should be connected");

                // 客户端发给服务端
                byte[] clientSendData = Encoding.UTF8.GetBytes("Hello Libp2p Server from Client");
                await client.SendAsync(clientSendData);
                var serverReceived = await serverReceivedTcs.Task.WaitAsync(TimeSpan.FromSeconds(5));
                Assert.Equal(clientSendData, serverReceived);

                // 服务端发给客户端
                byte[] serverSendData = Encoding.UTF8.GetBytes("Hello Libp2p Client from Server");
                await acceptedClient.SendAsync(serverSendData);
                var clientReceived = await clientReceivedTcs.Task.WaitAsync(TimeSpan.FromSeconds(5));
                Assert.Equal(serverSendData, clientReceived);

                client.Disconnect();
                Assert.False(client.IsConnected, "Client should be disconnected");
            }
            finally
            {
                await server.CloseAsync();
            }
        }

        [Fact]
        public async Task TestLargeDataTransferWithoutTruncation()
        {
            // 验证单次大于 64KB（例如 128KB）的数据包不会被静默截断丢弃
            var server = new BootstrapServer();
            server.Options.ListenAddress = "127.0.0.1";
            server.Options.TcpPort = 48890;

            const int dataSize = 128 * 1024; // 128 KB
            byte[] largeData = new byte[dataSize];
            new Random(42).NextBytes(largeData);

            using var receivedStream = new MemoryStream();
            var allDataReceivedTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            server.PeerConnected += (s, e) =>
            {
                e.Client.OnDataReceived += (s2, e2) =>
                {
                    lock (receivedStream)
                    {
                        receivedStream.Write(e2.Data.ToArray());
                        if (receivedStream.Length >= dataSize)
                        {
                            allDataReceivedTcs.TrySetResult(true);
                        }
                    }
                };
            };

            await server.StartAsync();

            try
            {
                var client = new PeerClient();
                var connectResult = await client.ConnectAsync(server.ListenAddresses.First(), 10000);
                Assert.True(connectResult.IsSuccess);

                // 发送 128KB 的大数据
                await client.SendAsync(largeData);

                // 等待服务端完全接收
                await allDataReceivedTcs.Task.WaitAsync(TimeSpan.FromSeconds(10));

                byte[] receivedBytes;
                lock (receivedStream)
                {
                    receivedBytes = receivedStream.ToArray();
                }

                Assert.Equal(dataSize, receivedBytes.Length);
                Assert.True(largeData.SequenceEqual(receivedBytes), "接收到的大数据内容必须与发送数据完全一致");

                client.Disconnect();
            }
            finally
            {
                await server.CloseAsync();
            }
        }

        [Fact]
        public async Task TestServerCloseDisconnectEvent()
        {
            // 验证服务端 CloseAsync 时不会吞掉客户端的 PeerDisconnected 事件
            var server = new BootstrapServer();
            server.Options.ListenAddress = "127.0.0.1";
            server.Options.TcpPort = 48891;

            var disconnectedTcs = new TaskCompletionSource<PeerDisconnectedEventArgs>(TaskCreationOptions.RunContinuationsAsynchronously);

            server.PeerDisconnected += (s, e) =>
            {
                disconnectedTcs.TrySetResult(e);
            };

            await server.StartAsync();

            try
            {
                var client = new PeerClient();
                var connectResult = await client.ConnectAsync(server.ListenAddresses.First(), 10000);
                Assert.True(connectResult.IsSuccess);

                // 服务端直接关闭
                await server.CloseAsync();

                // 验证 PeerDisconnected 被正确捕获
                var disconnectedEvent = await disconnectedTcs.Task.WaitAsync(TimeSpan.FromSeconds(5));
                Assert.NotNull(disconnectedEvent);
                Assert.NotNull(disconnectedEvent.Client);
            }
            finally
            {
                await server.CloseAsync();
            }
        }

        [Fact]
        public async Task TestClientReconnectWithoutLeak()
        {
            // 验证客户端断开重连能力及容器无泄漏正常工作
            var server = new BootstrapServer();
            server.Options.ListenAddress = "127.0.0.1";
            server.Options.TcpPort = 48892;

            await server.StartAsync();

            try
            {
                var client = new PeerClient();

                // 第一次连接
                var res1 = await client.ConnectAsync(server.ListenAddresses.First(), 10000);
                Assert.True(res1.IsSuccess);
                Assert.True(client.IsConnected);

                // 断开连接
                client.Disconnect();
                Assert.False(client.IsConnected);

                // 延时确保底层完全清理
                await Task.Delay(200);

                // 第二次重连
                var res2 = await client.ConnectAsync(server.ListenAddresses.First(), 10000);
                Assert.True(res2.IsSuccess, $"第二次重连失败: {res2.ErrorCode}, ex: {res2.Exception}");
                Assert.True(client.IsConnected);

                // 发送数据验证信道正常
                byte[] testMsg = Encoding.UTF8.GetBytes("Reconnect ping");
                await client.SendAsync(testMsg);

                client.Disconnect();
            }
            finally
            {
                await server.CloseAsync();
            }
        }
    }
}
