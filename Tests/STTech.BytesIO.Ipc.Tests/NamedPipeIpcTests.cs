using STTech.BytesIO.Ipc;
using System;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace STTech.BytesIO.Ipc.Tests
{
    public class NamedPipeIpcTests
    {
        [Fact]
        public async Task NamedPipe_EndToEnd_Communication_ShouldSucceed()
        {
            string pipeName = $"STTech_Pipe_{Guid.NewGuid():N}";

            var server = new IpcServer
            {
                PipeName = pipeName
            };

            var serverReceivedTcs = new TaskCompletionSource<byte[]>();
            var clientConnectedTcs = new TaskCompletionSource<IpcClient>();

            server.ClientConnected += (s, e) =>
            {
                clientConnectedTcs.TrySetResult(e.Client);
                e.Client.OnDataReceived += (s2, e2) =>
                {
                    serverReceivedTcs.TrySetResult(e2.Data.ToArray());
                };
            };

            await server.StartAsync();

            try
            {
                var client = new IpcClient
                {
                    PipeName = pipeName
                };

                var clientReceivedTcs = new TaskCompletionSource<byte[]>();
                client.OnDataReceived += (s, e) =>
                {
                    clientReceivedTcs.TrySetResult(e.Data.ToArray());
                };

                var connectResult = client.Connect(5000);
                Assert.True(connectResult.IsSuccess, $"命名管道连接应该成功: {connectResult.ErrorCode}, ex: {connectResult.Exception}");
                Assert.True(client.IsConnected);

                // 验证服务端感知到连接
                var serverClient = await clientConnectedTcs.Task.WaitAsync(TimeSpan.FromSeconds(5));
                Assert.NotNull(serverClient);

                // 客户端发往服务端
                byte[] clientData = Encoding.UTF8.GetBytes("Hello from NamedPipe Client");
                client.Send(clientData);

                var receivedOnServer = await serverReceivedTcs.Task.WaitAsync(TimeSpan.FromSeconds(5));
                Assert.Equal(clientData, receivedOnServer);

                // 服务端发往客户端
                byte[] serverData = Encoding.UTF8.GetBytes("Hello from NamedPipe Server");
                serverClient.Send(serverData);

                var receivedOnClient = await clientReceivedTcs.Task.WaitAsync(TimeSpan.FromSeconds(5));
                Assert.Equal(serverData, receivedOnClient);

                client.Disconnect();
            }
            finally
            {
                await server.CloseAsync();
            }
        }
    }
}
