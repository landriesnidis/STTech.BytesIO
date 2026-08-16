using STTech.BytesIO.Core;
using STTech.BytesIO.Ipc;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace STTech.BytesIO.Ipc.Tests
{
    /// <summary>
    /// 自定义共享内存客户端类型 (用于验证泛型服务端支持)
    /// </summary>
    public class CustomSharedMemoryClient : SharedMemoryIpcClient
    {
        public string CustomTag { get; set; } = "CustomTagValue";

        public CustomSharedMemoryClient() : base() { }

        public CustomSharedMemoryClient(System.IO.Pipes.PipeStream pipeStream) : base(pipeStream) { }
    }

    /// <summary>
    /// 自定义共享内存服务端类型
    /// </summary>
    public class CustomSharedMemoryServer : SharedMemoryIpcServer<CustomSharedMemoryClient>
    {
    }

    /// <summary>
    /// 共享内存 IPC 效果验证单元测试
    /// </summary>
    public class SharedMemoryIpcVerificationTests
    {
        private const long DefaultRegionSize = 128L * 1024 * 1024; // 128 MB

        private static async Task<(SharedMemoryIpcServer server, SharedMemoryIpcClient client, SharedMemoryIpcClient serverSideClient)> CreateChannelAsync(long regionSize = DefaultRegionSize)
        {
            var pipeName = "STTech.Tests.Verify." + Guid.NewGuid().ToString("N");
            var server = new SharedMemoryIpcServer { PipeName = pipeName, SharedMemoryRegionSize = regionSize };
            await server.StartAsync();

            var client = new SharedMemoryIpcClient { PipeName = pipeName, SharedMemoryRegionSize = regionSize, ServerName = "." };

            var result = await client.ConnectAsync(new ConnectArgument { Timeout = 5000 });
            if (!result.IsSuccess) throw new InvalidOperationException($"连接建立失败: {result.ErrorCode}");

            var serverSideClient = await WaitForServerClientAsync(server);
            return (server, client, serverSideClient);
        }

        private static byte[] GenerateTestData(int length, int seed = 42)
        {
            var data = new byte[length];
            new Random(seed).NextBytes(data);
            return data;
        }

        private static async Task<T> WithTimeout<T>(Task<T> task, int ms = 10000)
        {
            var completed = await Task.WhenAny(task, Task.Delay(ms));
            if (completed != task) throw new TimeoutException($"测试超时 ({ms}ms)");
            return await task;
        }

        private static async Task WithTimeout(Task task, int ms = 10000)
        {
            var completed = await Task.WhenAny(task, Task.Delay(ms));
            if (completed != task) throw new TimeoutException($"测试超时 ({ms}ms)");
            await task;
        }

        /// <summary>
        /// 验证 1: 双向高频并发数据与 ACK 交互，验证管道控制帧无交叉污染与数据竞争 (针对 BUG 1)
        /// </summary>
        [Fact]
        public async Task Verify_HighConcurrencyBidirectional_NoCorruption()
        {
            var (server, client, serverSide) = await CreateChannelAsync();
            try
            {
                const int iterations = 50;
                var clientReceivedCount = 0;
                var serverReceivedCount = 0;

                var clientTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                var serverTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

                client.OnDataReceived += (s, e) =>
                {
                    if (Interlocked.Increment(ref clientReceivedCount) == iterations)
                    {
                        clientTcs.TrySetResult(true);
                    }
                };

                serverSide.OnDataReceived += (s, e) =>
                {
                    if (Interlocked.Increment(ref serverReceivedCount) == iterations)
                    {
                        serverTcs.TrySetResult(true);
                    }
                };

                // 两端同时高频持续发送，同时接收并回送 ACK，极度考验 WriteFrameAsync 的线程安全性
                var clientSendTask = Task.Run(async () =>
                {
                    for (int i = 0; i < iterations; i++)
                    {
                        var data = GenerateTestData(4096, i);
                        await client.SendAsync(data);
                    }
                });

                var serverSendTask = Task.Run(async () =>
                {
                    for (int i = 0; i < iterations; i++)
                    {
                        var data = GenerateTestData(4096, i + 1000);
                        await serverSide.SendAsync(data);
                    }
                });

                await Task.WhenAll(clientSendTask, serverSendTask);
                await WithTimeout(Task.WhenAll(clientTcs.Task, serverTcs.Task), 20000);

                Assert.Equal(iterations, clientReceivedCount);
                Assert.Equal(iterations, serverReceivedCount);
            }
            finally
            {
                client.Disconnect();
                client.Dispose();
                await server.CloseAsync();
                server.Dispose();
            }
        }

        /// <summary>
        /// 验证 2: 泛型服务端支持自定义客户端类型实例化 (针对 BUG 2)
        /// </summary>
        [Fact]
        public async Task Verify_GenericServer_CustomClientTypeSupport()
        {
            var pipeName = "STTech.Tests.CustomServer." + Guid.NewGuid().ToString("N");
            var server = new CustomSharedMemoryServer { PipeName = pipeName, SharedMemoryRegionSize = DefaultRegionSize };
            await server.StartAsync();

            var client = new SharedMemoryIpcClient { PipeName = pipeName, SharedMemoryRegionSize = DefaultRegionSize, ServerName = "." };

            try
            {
                var result = await client.ConnectAsync(new ConnectArgument { Timeout = 5000 });
                Assert.True(result.IsSuccess);

                var serverSideClient = await WaitForServerClientAsync(server);

                Assert.NotNull(serverSideClient);
                Assert.Equal("CustomTagValue", serverSideClient.CustomTag);
                Assert.True(serverSideClient.IsConnected);
            }
            finally
            {
                client.Disconnect();
                client.Dispose();
                await server.CloseAsync();
                server.Dispose();
            }
        }

        /// <summary>
        /// 验证 3: 客户端断开连接后重新连接，协议序号与槽位状态正确复位 (针对 BUG 4)
        /// </summary>
        [Fact]
        public async Task Verify_ClientReconnect_SequenceAndSlotsReset()
        {
            var pipeName = "STTech.Tests.Reconnect." + Guid.NewGuid().ToString("N");
            var server = new SharedMemoryIpcServer { PipeName = pipeName, SharedMemoryRegionSize = DefaultRegionSize };
            await server.StartAsync();

            var client = new SharedMemoryIpcClient { PipeName = pipeName, SharedMemoryRegionSize = DefaultRegionSize, ServerName = "." };

            try
            {
                // 第一次连接
                var connectResult1 = await client.ConnectAsync(new ConnectArgument { Timeout = 5000 });
                Assert.True(connectResult1.IsSuccess);

                var serverSide1 = await WaitForServerClientAsync(server);

                var tcs1 = new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);
                serverSide1.OnDataReceived += (s, e) => tcs1.TrySetResult(e.Data.ToArray());

                var payload1 = GenerateTestData(8192, 1);
                await client.SendAsync(payload1);
                Assert.Equal(payload1, await WithTimeout(tcs1.Task, 5000));

                // 断开连接
                client.Disconnect();
                Assert.False(client.IsConnected);

                await Task.Delay(100);

                // 第二次重连
                var connectResult2 = await client.ConnectAsync(new ConnectArgument { Timeout = 5000 });
                Assert.True(connectResult2.IsSuccess);

                var serverSide2 = await WaitForServerClientAsync(server);

                var tcs2 = new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);
                serverSide2.OnDataReceived += (s, e) => tcs2.TrySetResult(e.Data.ToArray());

                var payload2 = GenerateTestData(16384, 2);
                await client.SendAsync(payload2);
                Assert.Equal(payload2, await WithTimeout(tcs2.Task, 5000));
            }
            finally
            {
                client.Disconnect();
                client.Dispose();
                await server.CloseAsync();
                server.Dispose();
            }
        }

        private static async Task<TClient> WaitForServerClientAsync<TClient>(BytesServer<TClient> server) where TClient : BytesClient
        {
            var deadline = DateTime.Now.AddSeconds(5);
            while (DateTime.Now < deadline)
            {
                var c = server.Clients.LastOrDefault();
                if (c != null && c.IsConnected) return c;
                await Task.Delay(10);
            }
            throw new TimeoutException("等待服务端客户端连接就绪超时");
        }

        /// <summary>
        /// 验证 4: 大数据零拷贝原生内存传输与数据一致性
        /// </summary>
        [Fact]
        public async Task Verify_LargePayload_ZeroCopyNativeMemory()
        {
            var (server, client, serverSide) = await CreateChannelAsync();
            try
            {
                var tcs = new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);
                serverSide.OnDataReceived += (s, e) =>
                {
                    // 验证非托管原生内存后端
                    Assert.False(MemoryMarshal.TryGetArray(e.Data.Memory, out _));
                    Assert.Equal(0xFE, e.Data[0]);
                    Assert.Equal(0xEF, e.Data[e.Data.Length - 1]);

                    tcs.TrySetResult(e.Data.ToArray());
                };

                // 发送 32MB 大数据
                var payload = GenerateTestData(32 * 1024 * 1024, 77);
                payload[0] = 0xFE;
                payload[payload.Length - 1] = 0xEF;

                await client.SendAsync(payload);
                var received = await WithTimeout(tcs.Task, 30000);
                Assert.Equal(payload, received);
            }
            finally
            {
                client.Disconnect();
                client.Dispose();
                await server.CloseAsync();
                server.Dispose();
            }
        }

        /// <summary>
        /// 验证 5: 单槽位背压机制与保序交付
        /// </summary>
        [Fact]
        public async Task Verify_SingleSlotBackpressure_OrderPreserved()
        {
            var (server, client, serverSide) = await CreateChannelAsync();
            try
            {
                var receivedList = new ConcurrentQueue<byte[]>();
                var allDone = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                const int totalFrames = 5;
                int count = 0;

                serverSide.OnDataReceived += (s, e) =>
                {
                    receivedList.Enqueue(e.Data.ToArray());
                    if (Interlocked.Increment(ref count) == totalFrames)
                    {
                        allDone.TrySetResult(true);
                    }
                };

                var payloads = Enumerable.Range(1, totalFrames)
                    .Select(i => GenerateTestData(i * 1024 * 1024, i))
                    .ToArray();

                // 快速并发触发发送，背压机制应自动按槽位串行等待 ACK
                foreach (var p in payloads)
                {
                    _ = client.SendAsync(p);
                }

                await WithTimeout(allDone.Task, 30000);
                var receivedArray = receivedList.ToArray();

                Assert.Equal(totalFrames, receivedArray.Length);
                for (int i = 0; i < totalFrames; i++)
                {
                    Assert.Equal(payloads[i], receivedArray[i]);
                }
            }
            finally
            {
                client.Disconnect();
                client.Dispose();
                await server.CloseAsync();
                server.Dispose();
            }
        }

        /// <summary>
        /// 验证 6: 发送超过单向容量上限时抛出明确异常且不破坏后续通信
        /// </summary>
        [Fact]
        public async Task Verify_ExceedCapacity_ThrowsArgumentOutOfRangeException()
        {
            const long regionSize = 16 * 1024 * 1024; // 16MB (单向 8MB)
            var (server, client, serverSide) = await CreateChannelAsync(regionSize);
            try
            {
                Assert.Equal(8 * 1024 * 1024, client.DataCapacity);

                // 发送 9MB 数据 (超出 8MB 上限)
                var overSizeData = new byte[9 * 1024 * 1024];
                await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () =>
                {
                    await client.SendAsync(overSizeData);
                });

                // 通道应未被损坏，合规大小数据仍可正常发送
                var validData = GenerateTestData(1024, 99);
                var tcs = new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);
                serverSide.OnDataReceived += (s, e) => tcs.TrySetResult(e.Data.ToArray());

                await client.SendAsync(validData);
                var received = await WithTimeout(tcs.Task, 5000);
                Assert.Equal(validData, received);
            }
            finally
            {
                client.Disconnect();
                client.Dispose();
                await server.CloseAsync();
                server.Dispose();
            }
        }

        /// <summary>
        /// 验证 7: 未连接时 DataCapacity 属性也能正确反映配置容量的一半
        /// </summary>
        [Fact]
        public void Verify_DataCapacity_BeforeConnect_ReturnsHalfRegionSize()
        {
            var client = new SharedMemoryIpcClient { SharedMemoryRegionSize = 64 * 1024 * 1024 };
            Assert.Equal(32 * 1024 * 1024, client.DataCapacity);
        }
    }
}
