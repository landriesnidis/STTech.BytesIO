using STTech.BytesIO.Core;
using STTech.BytesIO.P2P;
using Nethermind.Libp2p;
using Multiformats.Address;
using Microsoft.Extensions.Logging;
using System;
using System.Buffers;
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

                // 异步发起协议协商，不要 await 它阻塞通道建立
                var protoDialTask = session.DialAsync<BytesIOProtocol>(cts.Token);

                // 等待双方 channel 建立
                await Task.WhenAll(ch1Tcs.Task, ch2Tcs.Task);
                var ch1 = await ch1Tcs.Task;
                var ch2 = await ch2Tcs.Task;

                _output.WriteLine("Channels established! Testing data transmission...");

                // 从 Peer2 发送数据给 Peer1
                byte[] sendFrom2 = Encoding.UTF8.GetBytes("Hello from Peer2");
                await ch2.WriteAsync(new System.Buffers.ReadOnlySequence<byte>(sendFrom2));

                // Peer1 读取数据
                byte[]? receivedBy1 = null;
                await foreach (var data in ch1.ReadAllAsync(cts.Token))
                {
                    receivedBy1 = data.ToArray();
                    break;
                }
                Assert.Equal(sendFrom2, receivedBy1);
                _output.WriteLine($"Peer1 received: {Encoding.UTF8.GetString(receivedBy1)}");

                // 从 Peer1 发送数据给 Peer2
                byte[] sendFrom1 = Encoding.UTF8.GetBytes("Hello from Peer1");
                await ch1.WriteAsync(new System.Buffers.ReadOnlySequence<byte>(sendFrom1));

                // Peer2 读取数据
                byte[]? receivedBy2 = null;
                await foreach (var data in ch2.ReadAllAsync(cts.Token))
                {
                    receivedBy2 = data.ToArray();
                    break;
                }
                Assert.Equal(sendFrom1, receivedBy2);
                _output.WriteLine($"Peer2 received: {Encoding.UTF8.GetString(receivedBy2)}");

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
                    _output.WriteLine($"Server: Received data from client: {Encoding.UTF8.GetString(e2.Data.ToArray())}");
                    serverReceivedTcs.TrySetResult(e2.Data.ToArray());
                };
            };

            await server.StartAsync();
            _output.WriteLine($"Step 2: Server started, ListenAddresses: {string.Join(", ", server.ListenAddresses)}");

            try
            {
                Assert.NotNull(server.LocalPeerId);
                Assert.NotEmpty(server.ListenAddresses);

                var client = new PeerClient();
                // 监听地址本身已包含 /p2p/{PeerId}
                client.RemoteAddress = server.ListenAddresses.First();
                client.OnDataReceived += (s, e) =>
                {
                    _output.WriteLine($"Client: Received data from server: {Encoding.UTF8.GetString(e.Data.ToArray())}");
                    clientReceivedTcs.TrySetResult(e.Data.ToArray());
                };

                _output.WriteLine($"Step 3: Client connecting to {client.RemoteAddress}");

                var connectResult = await client.ConnectAsync(new ConnectArgument { Timeout = 10000 });
                _output.WriteLine($"Step 4: Client connected result: {connectResult.IsSuccess}, ErrorCode: {connectResult.ErrorCode}");

                Assert.True(connectResult.IsSuccess, $"Client connection failed: {connectResult.ErrorCode}, ex: {connectResult.Exception}");
                Assert.True(client.IsConnected, "Client should be connected");

                // 等待服务端建立连接
                var acceptedClient = await serverConnectedTcs.Task.WaitAsync(TimeSpan.FromSeconds(5));
                Assert.NotNull(acceptedClient);
                Assert.True(acceptedClient.IsConnected, "Server-side accepted client should be connected");

                // 客户端向服务端发送数据
                byte[] clientSendData = Encoding.UTF8.GetBytes("Hello Libp2p Server from Client");
                _output.WriteLine("Step 5: Client sending data");
                await client.SendAsync(clientSendData);

                _output.WriteLine("Step 6: Waiting for server to receive data");
                var serverReceived = await serverReceivedTcs.Task.WaitAsync(TimeSpan.FromSeconds(5));
                Assert.Equal(clientSendData, serverReceived);

                // 服务端向客户端发送数据
                byte[] serverSendData = Encoding.UTF8.GetBytes("Hello Libp2p Client from Server");
                _output.WriteLine("Step 7: Server sending data to client");
                await acceptedClient.SendAsync(serverSendData);

                _output.WriteLine("Step 8: Waiting for client to receive data");
                var clientReceived = await clientReceivedTcs.Task.WaitAsync(TimeSpan.FromSeconds(5));
                Assert.Equal(serverSendData, clientReceived);

                _output.WriteLine("Step 9: Disconnecting client");
                client.Disconnect();
                Assert.False(client.IsConnected, "Client should be disconnected");
            }
            finally
            {
                _output.WriteLine("Step 10: Closing server");
                await server.CloseAsync();
                _output.WriteLine("Step 11: Server closed");
            }
        }
    }
}
