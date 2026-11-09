using STTech.BytesIO.Tcp;
using STTech.BytesIO.Core;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace STTech.BytesIO.Tcp.Tests
{
    public class TcpCommunicationTests
    {
        [Fact]
        public async Task TestClientToServerCommunication()
        {
            var server = new TcpServer();
            int port = PortHelper.GetAvailablePort();
            server.Port = port;
            var tcs = new TaskCompletionSource<byte[]>();

            server.ClientConnected += (s, e) =>
            {
                e.Client.OnDataReceived += (s2, e2) =>
                {
                    tcs.TrySetResult(e2.Data.ToArray());
                };
            };

            await server.StartAsync();

            try
            {
                var client = new TcpClient();
                client.Host = "127.0.0.1";
                client.Port = port;
                var connectResult = client.Connect(10000);

                Assert.True(connectResult.IsSuccess, $"Connection failed: {connectResult.ErrorCode}");
                Assert.True(client.IsConnected);

                byte[] sendData = Encoding.UTF8.GetBytes("Hello Server");
                client.Send(sendData);

                var timeoutTask = Task.Delay(5000);
                var completedTask = await Task.WhenAny(tcs.Task, timeoutTask);
                
                Assert.Same(tcs.Task, completedTask);
                byte[] receivedData = await tcs.Task;
                Assert.Equal(sendData, receivedData);

                client.Disconnect();
            }
            finally
            {
                await server.CloseAsync();
            }
        }

        [Fact]
        public async Task TestVirtualClientDelegation()
        {
            var server = new TcpServer();
            int port = PortHelper.GetAvailablePort();
            server.Port = port;
            var tcs = new TaskCompletionSource<byte[]>();

            server.ClientConnected += (s, e) =>
            {
                e.Client.OnDataReceived += (s2, e2) =>
                {
                    tcs.TrySetResult(e2.Data.ToArray());
                };
            };

            await server.StartAsync();

            try
            {
                var innerClient = new TcpClient();
                innerClient.Host = "127.0.0.1";
                innerClient.Port = port;
                
                var virtualClient = new VirtualClient(innerClient);
                var connectResult = virtualClient.Connect(10000);

                Assert.True(connectResult.IsSuccess);
                Assert.True(virtualClient.IsConnected);

                byte[] sendData = new byte[] { 0xDE, 0xAD, 0xBE, 0xEF };
                await virtualClient.SendAsync(sendData);

                var timeoutTask = Task.Delay(2000);
                var completedTask = await Task.WhenAny(tcs.Task, timeoutTask);
                
                Assert.Same(tcs.Task, completedTask);
                byte[] receivedData = await tcs.Task;
                Assert.Equal(sendData, receivedData);

                virtualClient.Disconnect();
            }
            finally
            {
                await server.CloseAsync();
            }
        }

        [Fact]
        public async Task TestServerToClientCommunication()
        {
            var server = new TcpServer();
            int port = PortHelper.GetAvailablePort();
            server.Port = port;
            var tcs = new TaskCompletionSource<byte[]>();
            var clientConnectedTcs = new TaskCompletionSource<bool>();

            server.ClientConnected += (s, e) =>
            {
                clientConnectedTcs.TrySetResult(true);
            };

            await server.StartAsync();

            try
            {
                var client = new TcpClient();
                client.Host = "127.0.0.1";
                client.Port = port;
                
                client.OnDataReceived += (s, e) =>
                {
                    tcs.TrySetResult(e.Data.ToArray());
                };

                var connectResult = client.Connect(10000);
                Assert.True(connectResult.IsSuccess);

                // 使用事件完成同步，消除死等延时
                await Task.WhenAny(clientConnectedTcs.Task, Task.Delay(2000));

                byte[] sendData = Encoding.UTF8.GetBytes("Hello Client");
                
                // Server sends to all clients
                Assert.NotEmpty(server.Clients);
                foreach (var c in server.Clients)
                {
                    c.Send(sendData);
                }

                var timeoutTask = Task.Delay(5000);
                var completedTask = await Task.WhenAny(tcs.Task, timeoutTask);
                
                Assert.Same(tcs.Task, completedTask);
                byte[] receivedData = await tcs.Task;
                Assert.Equal(sendData, receivedData);

                client.Disconnect();
            }
            finally
            {
                await server.CloseAsync();
            }
        }

        [Fact]
        public async Task TestMultipleClientsCommunication()
        {
            var server = new TcpServer();
            int port = PortHelper.GetAvailablePort();
            server.Port = port;
            var receivedCounts = new ConcurrentDictionary<TcpClient, int>();
            var allDataReceivedTcs = new TaskCompletionSource<bool>();
            int expectedClients = 5;
            int receivedClients = 0;

            server.ClientConnected += (s, e) =>
            {
                e.Client.OnDataReceived += (s2, e2) =>
                {
                    receivedCounts.AddOrUpdate((TcpClient)e.Client, 1, (c, v) => v + 1);
                    if (Interlocked.Increment(ref receivedClients) == expectedClients)
                    {
                        allDataReceivedTcs.TrySetResult(true);
                    }
                };
            };

            await server.StartAsync();

            try
            {
                var clients = new List<TcpClient>();
                for (int i = 0; i < expectedClients; i++)
                {
                    var client = new TcpClient();
                    client.Host = "127.0.0.1";
                    client.Port = port;
                    var res = client.Connect(10000);
                    Assert.True(res.IsSuccess, $"Client {i} 连接失败: {res.ErrorCode}, ex: {res.Exception}");
                    clients.Add(client);
                }

                foreach (var client in clients)
                {
                    client.Send(new byte[] { 0xAA });
                }

                var timeoutTask = Task.Delay(5000);
                var completedTask = await Task.WhenAny(allDataReceivedTcs.Task, timeoutTask);

                Assert.Equal(allDataReceivedTcs.Task, completedTask);
                Assert.Equal(expectedClients, receivedCounts.Count);

                foreach (var client in clients)
                {
                    client.Disconnect();
                }
            }
            finally
            {
                await server.CloseAsync();
            }
        }

        [Fact]
        public async Task TestDisconnectionEvents()
        {
            var server = new TcpServer();
            int port = PortHelper.GetAvailablePort();
            server.Port = port;
            var serverDisconnectedTcs = new TaskCompletionSource<bool>();
            var clientDisconnectedTcs = new TaskCompletionSource<bool>();
            var clientConnectedTcs = new TaskCompletionSource<bool>();
            
            server.ClientConnected += (s, e) => {
                clientConnectedTcs.TrySetResult(true);
                e.Client.OnDataReceived += (s2, e2) => { }; // Trigger receive loop
            };
            server.ClientDisconnected += (s, e) => {
                serverDisconnectedTcs.TrySetResult(true);
            };

            await server.StartAsync();

            try
            {
                var client = new TcpClient();
                client.Host = "127.0.0.1";
                client.Port = port;
                client.OnDataReceived += (s, e) => { }; // Trigger receive loop on client side too
                client.OnDisconnected += (s, e) => {
                    clientDisconnectedTcs.TrySetResult(true);
                };
                client.Connect(10000);
                
                // 等待服务端建立连接就绪
                await Task.WhenAny(clientConnectedTcs.Task, Task.Delay(2000));

                client.Disconnect();

                var clientTask = clientDisconnectedTcs.Task;
                var serverTask = serverDisconnectedTcs.Task;
                
                var completedClient = await Task.WhenAny(clientTask, Task.Delay(2000));
                Assert.True(completedClient == clientTask, "Client disconnect event timed out");

                var completedServer = await Task.WhenAny(serverTask, Task.Delay(2000));
                Assert.True(completedServer == serverTask, "Server disconnect event timed out");
            }
            finally
            {
                await server.CloseAsync();
            }
        }

        [Fact]
        public async Task TestAcceptedClientReceiveLoopStart()
        {
            var server = new TcpServer();
            int port = PortHelper.GetAvailablePort();
            server.Port = port;
            var tcs = new TaskCompletionSource<bool>();
            var serverClientReadyTcs = new TaskCompletionSource<bool>();

            server.ClientConnected += (s, e) =>
            {
                var client = (TcpClient)e.Client;
                client.OnDataReceived += (s2, e2) => tcs.TrySetResult(true);
                serverClientReadyTcs.TrySetResult(true);
            };

            await server.StartAsync();

            try
            {
                var client = new TcpClient();
                client.Host = "127.0.0.1";
                client.Port = port;
                client.Connect(10000);

                await Task.WhenAny(serverClientReadyTcs.Task, Task.Delay(2000));
                client.Send(new byte[] { 1 });

                var timeoutTask = Task.Delay(2000);
                var completedTask = await Task.WhenAny(tcs.Task, timeoutTask);
                Assert.Equal(tcs.Task, completedTask);
            }
            finally
            {
                await server.CloseAsync();
            }
        }
    }
}
