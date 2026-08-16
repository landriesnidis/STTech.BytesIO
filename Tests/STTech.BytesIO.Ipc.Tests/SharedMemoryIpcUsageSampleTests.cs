using STTech.BytesIO.Core;
using STTech.BytesIO.Ipc;
using System;
using System.Buffers;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace STTech.BytesIO.Ipc.Tests
{
    /// <summary>
    /// 自定义客户端类型示例 (可添加自定义业务字段与方法)
    /// </summary>
    public class MyBusinessIpcClient : SharedMemoryIpcClient
    {
        public string ClientSessionId { get; set; } = Guid.NewGuid().ToString("N");

        public MyBusinessIpcClient() : base() { }

        public MyBusinessIpcClient(PipeStream pipeStream) : base(pipeStream) { }
    }

    /// <summary>
    /// 自定义服务端类型示例
    /// </summary>
    public class MyBusinessIpcServer : SharedMemoryIpcServer<MyBusinessIpcClient>
    {
    }

    /// <summary>
    /// 共享内存 IPC 使用范例与演示单元测试
    /// 该测试类展示了 SharedMemoryIpcClient / SharedMemoryIpcServer 的典型用法与最佳实践。
    /// </summary>
    public class SharedMemoryIpcUsageSampleTests
    {
        /// <summary>
        /// 示例 1: 基础起步 —— 服务端启动、客户端连接与双向数据收发
        /// </summary>
        [Fact]
        public async Task Sample01_QuickStart_ServerAndClient()
        {
            var pipeName = "STTech.Sample.QuickStart." + Guid.NewGuid().ToString("N");

            // 1. 创建并启动服务端
            var server = new SharedMemoryIpcServer
            {
                PipeName = pipeName,
                // 可选配置：共享内存区域总容量 (默认 1GB, 每个方向各占一半)
                SharedMemoryRegionSize = 64 * 1024 * 1024 // 64 MB
            };

            // 注册服务端收到客户端连接事件
            server.ClientConnected += (s, e) =>
            {
                Console.WriteLine($"[服务端] 客户端已连接: {e.Client.ConnectionId}");

                // 监听该客户端发来的数据
                e.Client.OnDataReceived += (sender, args) =>
                {
                    // 转换为 byte[] (若需长期保留) 或直接读取
                    byte[] data = args.Data.ToArray();
                    string message = Encoding.UTF8.GetString(data);
                    Console.WriteLine($"[服务端] 收到消息: {message}");

                    // 服务端回传数据
                    byte[] reply = Encoding.UTF8.GetBytes($"[ACK] 已收到: {message}");
                    e.Client.Send(reply);
                };
            };

            await server.StartAsync();

            // 2. 创建并连接客户端
            var client = new SharedMemoryIpcClient
            {
                PipeName = pipeName,
                SharedMemoryRegionSize = 64 * 1024 * 1024, // 需与服务端一致
                ServerName = "." // 本机
            };

            var replyTcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);

            // 注册客户端数据接收回调
            client.OnDataReceived += (s, e) =>
            {
                string replyMsg = Encoding.UTF8.GetString(e.Data.ToArray());
                replyTcs.TrySetResult(replyMsg);
            };

            // 异步连接
            var connectResult = await client.ConnectAsync(new ConnectArgument { Timeout = 3000 });
            Assert.True(connectResult.IsSuccess, "连接服务端成功");

            // 3. 客户端发送数据
            byte[] sendPayload = Encoding.UTF8.GetBytes("Hello, STTech.BytesIO SharedMemory IPC!");
            await client.SendAsync(sendPayload);

            // 等待服务端回包
            var completedTask = await Task.WhenAny(replyTcs.Task, Task.Delay(5000));
            Assert.Same(replyTcs.Task, completedTask);

            string receivedReply = await replyTcs.Task;
            Assert.Equal("[ACK] 已收到: Hello, STTech.BytesIO SharedMemory IPC!", receivedReply);

            // 4. 清理与断开
            client.Disconnect();
            client.Dispose();
            await server.CloseAsync();
            server.Dispose();
        }

        /// <summary>
        /// 示例 2: 零拷贝接收 —— 直接读取非托管原生内存，零 GC 堆分配
        /// 当传输超大数据（例如图像、点云、视频帧、超大二进制模型）时，
        /// 使用 ReceiveContext 直接读取共享内存视图，性能最高。
        /// </summary>
        [Fact]
        public async Task Sample02_ZeroCopy_DirectSpanReading()
        {
            var pipeName = "STTech.Sample.ZeroCopy." + Guid.NewGuid().ToString("N");
            var server = new SharedMemoryIpcServer { PipeName = pipeName, SharedMemoryRegionSize = 64 * 1024 * 1024 };

            var verifiedTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            // 【最佳实践】通过 ClientConnected 事件获取新连接的客户端并注册数据接收
            server.ClientConnected += (s, e) =>
            {
                e.Client.OnDataReceived += (sender, args) =>
                {
                    // 使用 ReadOnlyMemory<byte> 或 Span<byte> 进行直接计算，无需调用 ToArray()
                    ReadOnlyMemory<byte> memory = args.Data.Memory;
                    ReadOnlySpan<byte> span = memory.Span;

                    // 1. 通过 ReceiveContext 内置类型转换直接读取头部信息 (零拷贝)
                    int magic = args.Data.ToInt32(0);
                    int payloadSize = args.Data.ToInt32(4);

                    // 2. 直接对 Span 进行切片和校验
                    ReadOnlySpan<byte> body = span.Slice(8, payloadSize);
                    byte checksum = 0;
                    for (int i = 0; i < body.Length; i++)
                    {
                        checksum ^= body[i];
                    }

                    // 校验通过
                    if (magic == 0x12345678 && checksum == 0xAA)
                    {
                        verifiedTcs.TrySetResult(true);
                    }
                };
            };

            await server.StartAsync();

            var client = new SharedMemoryIpcClient { PipeName = pipeName, SharedMemoryRegionSize = 64 * 1024 * 1024, ServerName = "." };
            await client.ConnectAsync(new ConnectArgument { Timeout = 3000 });

            // 客户端构造 4MB 数据包 (4字节魔术字 + 4字节长度 + 4MB Body)
            const int bodyLength = 4 * 1024 * 1024;
            byte[] package = new byte[8 + bodyLength];
            BitConverter.GetBytes(0x12345678).CopyTo(package, 0);
            BitConverter.GetBytes(bodyLength).CopyTo(package, 4);
            // 填充数据使其异或和为 0xAA
            package[8] = 0xAA;

            await client.SendAsync(package);

            var completed = await Task.WhenAny(verifiedTcs.Task, Task.Delay(10000));
            Assert.Same(verifiedTcs.Task, completed);
            Assert.True(await verifiedTcs.Task);

            client.Disconnect();
            client.Dispose();
            await server.CloseAsync();
            server.Dispose();
        }

        /// <summary>
        /// 示例 3: 自定义配置 —— 配置共享内存大小、名称与超时
        /// </summary>
        [Fact]
        public async Task Sample03_CustomConfiguration()
        {
            var customPipeName = "STTech.Sample.Config." + Guid.NewGuid().ToString("N");
            var customMapName = "Global\\STTech.Sample.CustomShm." + Guid.NewGuid().ToString("N");
            const long regionSize = 256L * 1024 * 1024; // 256MB

            // 服务端配置
            var server = new SharedMemoryIpcServer
            {
                PipeName = customPipeName,
                MapName = customMapName,
                SharedMemoryRegionSize = regionSize
            };
            await server.StartAsync();

            // 客户端配置 (需与服务端一致)
            var client = new SharedMemoryIpcClient
            {
                PipeName = customPipeName,
                MapName = customMapName,
                SharedMemoryRegionSize = regionSize,
                AckTimeoutMs = 10000 // 等待对端确认的超时时间为 10 秒
            };

            // 连接前即可读取单向数据容量 (等于 RegionSize / 2)
            Assert.Equal(128L * 1024 * 1024, client.DataCapacity);

            var result = await client.ConnectAsync(new ConnectArgument { Timeout = 3000 });
            Assert.True(result.IsSuccess);
            Assert.True(client.IsSharedMemoryReady);

            client.Disconnect();
            client.Dispose();
            await server.CloseAsync();
            server.Dispose();
        }

        /// <summary>
        /// 示例 4: 请求-响应模式 (Request-Reply Pattern)
        /// 使用 SendAsync(data, timeout) 异步等待远端的回包
        /// </summary>
        [Fact]
        public async Task Sample04_RequestResponsePattern()
        {
            var pipeName = "STTech.Sample.ReqRep." + Guid.NewGuid().ToString("N");
            var server = new SharedMemoryIpcServer { PipeName = pipeName };

            // 服务端回显处理
            server.ClientConnected += (s, e) =>
            {
                e.Client.OnDataReceived += (sender, args) =>
                {
                    // 接收到请求并响应
                    byte[] response = Encoding.UTF8.GetBytes("PONG");
                    e.Client.Send(response);
                };
            };
            await server.StartAsync();

            var client = new SharedMemoryIpcClient { PipeName = pipeName, ServerName = "." };
            await client.ConnectAsync(new ConnectArgument { Timeout = 3000 });

            // 发送 PING 请求并等待响应 (超时 3000ms)
            byte[] request = Encoding.UTF8.GetBytes("PING");
            var reply = await client.SendAsync(request, timeout: 3000);

            Assert.Equal(ReplyStatus.Completed, reply.Status);
            Assert.False(reply.GetBytes().IsEmpty);
            Assert.Equal("PONG", Encoding.UTF8.GetString(reply.GetBytes().Span));

            client.Disconnect();
            client.Dispose();
            await server.CloseAsync();
            server.Dispose();
        }

        /// <summary>
        /// 示例 5: 扩展派生自定义客户端与服务端类型
        /// </summary>
        [Fact]
        public async Task Sample05_CustomServerAndClient()
        {
            var pipeName = "STTech.Sample.CustomTypes." + Guid.NewGuid().ToString("N");

            var server = new MyBusinessIpcServer { PipeName = pipeName };
            var clientConnectedTcs = new TaskCompletionSource<MyBusinessIpcClient>(TaskCreationOptions.RunContinuationsAsynchronously);

            server.ClientConnected += (s, e) =>
            {
                // e.Client 强类型为 MyBusinessIpcClient
                Console.WriteLine($"自定义客户端已连接，Session: {e.Client.ClientSessionId}");
                clientConnectedTcs.TrySetResult(e.Client);
            };

            await server.StartAsync();

            var client = new MyBusinessIpcClient { PipeName = pipeName, ServerName = "." };
            var result = await client.ConnectAsync(new ConnectArgument { Timeout = 3000 });
            Assert.True(result.IsSuccess);

            var completedTask = await Task.WhenAny(clientConnectedTcs.Task, Task.Delay(5000));
            Assert.Same(clientConnectedTcs.Task, completedTask);

            var acceptedClient = await clientConnectedTcs.Task;
            Assert.NotNull(acceptedClient);
            Assert.NotEmpty(acceptedClient.ClientSessionId);

            client.Disconnect();
            client.Dispose();
            await server.CloseAsync();
            server.Dispose();
        }
    }
}
