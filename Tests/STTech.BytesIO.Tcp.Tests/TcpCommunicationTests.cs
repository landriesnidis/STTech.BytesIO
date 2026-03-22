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
            server.Port = 50001;
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
                client.Port = 50001;
                var connectResult = client.Connect();

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
            server.Port = 50006;
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
                innerClient.Port = 50006;
                
                var virtualClient = new VirtualClient(innerClient);
                var connectResult = virtualClient.Connect();

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
            server.Port = 50002;
            var tcs = new TaskCompletionSource<byte[]>();

            await server.StartAsync();

            try
            {
                var client = new TcpClient();
                client.Host = "127.0.0.1";
                client.Port = 50002;
                
                client.OnDataReceived += (s, e) =>
                {
                    tcs.TrySetResult(e.Data.ToArray());
                };

                var connectResult = client.Connect();
                Assert.True(connectResult.IsSuccess);

                // Wait a bit for the server to process the new client and add it to the list
                await Task.Delay(200);

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
            server.Port = 50003;
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
                    client.Port = 50003;
                    client.Connect();
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
            server.Port = 50004;
            var serverDisconnectedTcs = new TaskCompletionSource<bool>();
            var clientDisconnectedTcs = new TaskCompletionSource<bool>();
            server.ClientConnected += (s, e) => {
                Console.WriteLine("[DEBUG Test] server.ClientConnected fired");
                e.Client.OnDataReceived += (s2, e2) => { }; // Trigger receive loop
            };
            server.ClientDisconnected += (s, e) => {
                Console.WriteLine("[DEBUG Test] server.ClientDisconnected fired");
                serverDisconnectedTcs.TrySetResult(true);
            };

            await server.StartAsync();

            try
            {
                var client = new TcpClient();
                client.Host = "127.0.0.1";
                client.Port = 50004;
                client.OnDataReceived += (s, e) => { }; // Trigger receive loop on client side too for symmetry
                client.OnDisconnected += (s, e) => {
                    Console.WriteLine("[DEBUG Test] client.OnDisconnected fired");
                    clientDisconnectedTcs.TrySetResult(true);
                };
                client.Connect();
                
                // Add a small delay to ensure everything is stable
                await Task.Delay(500);

                Console.WriteLine("[DEBUG Test] Calling client.Disconnect()");
                client.Disconnect();

                var clientTask = clientDisconnectedTcs.Task;
                var serverTask = serverDisconnectedTcs.Task;
                
                var completedClient = await Task.WhenAny(clientTask, Task.Delay(2000));
                Assert.True(completedClient == clientTask, "Client disconnect event timed out");

                var completedServer = await Task.WhenAny(serverTask, Task.Delay(2000));
                Assert.True(await Task.WhenAny(serverDisconnectedTcs.Task, Task.Delay(2000)) == serverDisconnectedTcs.Task, "Server disconnect event timed out");
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
            server.Port = 50005;
            var tcs = new TaskCompletionSource<bool>();

            server.ClientConnected += (s, e) =>
            {
                var client = (TcpClient)e.Client;
                // Add handler with a delay to simulate late binding
                Task.Delay(100).ContinueWith(_ =>
                {
                    client.OnDataReceived += (s2, e2) => tcs.TrySetResult(true);
                });
            };

            await server.StartAsync();

            try
            {
                var client = new TcpClient();
                client.Host = "127.0.0.1";
                client.Port = 50005;
                client.Connect();

                await Task.Delay(300); // Ensure the server-side handler is added
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
