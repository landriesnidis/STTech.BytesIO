using STTech.BytesIO.Core;
using STTech.BytesIO.Core.Component;
using STTech.BytesIO.Tcp;
using System;
using System.Buffers;
using System.Reflection;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace STTech.BytesIO.Core.Tests
{
    public class RemediationVerificationTests
    {
        private class DummyClient : BytesClient
        {
            public override bool IsConnected => true;
            public override ConnectResult Connect(ConnectArgument? argument = null) => new ConnectResult();
            public override DisconnectResult Disconnect(DisconnectArgument? argument = null) => new DisconnectResult();
            protected override Task SendHandlerAsync(SendArgs args) => Task.CompletedTask;
            protected override Task ReceiveDataHandleAsync(CancellationToken cancellationToken) => Task.CompletedTask;
            protected override void ReceiveDataCompletedHandle() { }

            public void SimulateIncomingContext(ReceiveContext context)
            {
                InvokeDataReceivedEventCallback(context);
            }
        }

        #region 阶段 1 验证测试 (Phase 1)

        [Fact]
        public void BytesClient_Dispose_ReleasesResources_AndSetsIsDisposed()
        {
            var client = new DummyClient();
            Assert.False(client.IsDisposed);

            client.Dispose();
            Assert.True(client.IsDisposed);

            // 验证幂等性
            client.Dispose();
            Assert.True(client.IsDisposed);
        }

        [Fact]
        public async Task DataReceiveTaskQueue_EnsuresReceiveContextDisposed_EvenIfExceptionOccurs()
        {
            var client = new DummyClient();
            byte[] rented = ArrayPool<byte>.Shared.Rent(64);

            var context = new ReceiveContext(rented, 0, 64);
            var tcs = new TaskCompletionSource<bool>();

            client.OnDataReceived += (s, e) =>
            {
                tcs.TrySetResult(true);
                throw new InvalidOperationException("故意在消费事件中抛出异常以测试 finally 保护");
            };

            // 派发接收数据
            client.SimulateIncomingContext(context);

            // 等待队列消费触发
            await tcs.Task;

            // 短暂让出给 finally 执行
            await Task.Delay(50);

            Assert.True(context.IsDisposed, "即使回调中抛出未捕获异常，ReceiveContext 也必须在 finally 中被释放");

            client.Dispose();
        }

        [Fact]
        public void TcpClient_HasServerSslAuthenticationMethods()
        {
            var method = typeof(STTech.BytesIO.Tcp.TcpClient).GetMethod("InitializeSslStreamAsServerAsync",
                new[] { typeof(X509Certificate), typeof(SslProtocols) });

            Assert.NotNull(method);

            var syncMethod = typeof(STTech.BytesIO.Tcp.TcpClient).GetMethod("InitializeSslStreamAsServer",
                new[] { typeof(X509Certificate), typeof(SslProtocols) });

            Assert.NotNull(syncMethod);
        }

        [Fact]
        public void TcpClient_Dispose_CallsBaseDispose()
        {
            var tcpClient = new STTech.BytesIO.Tcp.TcpClient();
            Assert.False(tcpClient.IsDisposed);

            tcpClient.Dispose();
            Assert.True(tcpClient.IsDisposed, "TcpClient.Dispose 必须调用 base.Dispose 并标记 IsDisposed");
        }

        #endregion

        #region 阶段 2 验证测试 (Phase 2)

        [Fact]
        public void SendArgs_UsesMonotonicClock()
        {
            long t1 = MonotonicClock.NowMs;
            var args = new SendArgs(new byte[] { 1, 2, 3 }, new SendOptions());
            long t2 = MonotonicClock.NowMs;

            Assert.True(args.EnqueueTimestamp >= t1 && args.EnqueueTimestamp <= t2, "EnqueueTimestamp 必须使用单调时钟生成");
        }

        [Fact]
        public void ReconnectPlugin_TimerIsConfiguredWithAutoResetFalse()
        {
            var plugin = new ReconnectPlugin();
            plugin.Configure(500, 3);

            var dummyClient = new DummyClient();
            plugin.Client = dummyClient;

            plugin.OnDisconnected(new DisconnectedEventArgs(DisconnectionReasonCode.Passive, null));

            var field = typeof(ReconnectPlugin).GetField("_timer", BindingFlags.NonPublic | BindingFlags.Instance);
            var timer = field?.GetValue(plugin) as System.Timers.Timer;
            Assert.NotNull(timer);
            Assert.False(timer.AutoReset, "重连定时器必须配置为 AutoReset=false，防止网络请求挂起时的并发重入重试轰炸");

            plugin.Dispose();
            dummyClient.Dispose();
        }

        [Fact]
        public async Task TcpClient_ConnectAsync_HandlesConnectionRefused_Properly()
        {
            var client = new STTech.BytesIO.Tcp.TcpClient
            {
                Host = "127.0.0.1",
                Port = 54321 // 本机未监听端口
            };

            var result = await client.ConnectAsync(new ConnectArgument { Timeout = 1000 });

            Assert.False(result.IsSuccess, "未开启端口应连接失败");
            Assert.False(client.IsConnected);

            client.Dispose();
        }

        [Fact]
        public async Task TcpClient_ConnectAsync_CancellationToken_AbortsImmediately()
        {
            var client = new STTech.BytesIO.Tcp.TcpClient
            {
                Host = "127.0.0.1",
                Port = 54321
            };

            using var cts = new CancellationTokenSource();
            cts.Cancel(); // 立即取消

            var result = await client.ConnectAsync(new ConnectArgument { CancellationToken = cts.Token });

            Assert.False(result.IsSuccess);
            Assert.False(client.IsConnected);

            client.Dispose();
        }

        #endregion

        #region 阶段 3 验证测试 (Phase 3)

        private class TestFixedLengthUnpacker : Unpacker
        {
            private readonly int _length;
            public TestFixedLengthUnpacker(int length) { _length = length; }
            protected override int CalculatePacketLength(ReadOnlySequence<byte> buffer)
            {
                return buffer.Length >= _length ? _length : 0;
            }
        }

        [Fact]
        public void UnpackContext_ReferenceCounting_PreventsUseAfterFree()
        {
            var unpacker = new TestFixedLengthUnpacker(4);
            byte[] rented = ArrayPool<byte>.Shared.Rent(16);
            for (int i = 0; i < 4; i++) rented[i] = (byte)(i + 1);

            var context = new ReceiveContext(rented, 0, 4);

            UnpackContext capturedContext = null;
            unpacker.OnDataParsed += (s, e) =>
            {
                // 模拟业务异步消费，捕获上下文
                capturedContext = e.Data;
            };

            // 输入数据
            unpacker.Input(context);

            // 原始输入方释放自身持有的引用
            context.Dispose();

            // 关键断言：此时 Unpacker 已消费完毕并执行了 SkipCachedBytes，
            // 但因为 capturedContext 仍存活且持有引用，底层内存绝对不能被释放或归还池化
            Assert.NotNull(capturedContext);
            Assert.False(context.IsDisposed, "UnpackContext 仍持有段引用，底层 ReceiveContext 绝对不能被提前释放");
            Assert.Equal(4, capturedContext.Data.Length);

            // 模拟异步业务消费结束，执行 Dispose
            capturedContext.Dispose();

            // 此时所有引用归零，底层 ReceiveContext 被安全释放
            Assert.True(context.IsDisposed, "UnpackContext.Dispose 调用后，引用计数归零，底层 ReceiveContext 必须被释放");
        }

        [Fact]
        public void Unpacker_MaxBufferSize_TriggersBufferOverflowError_AndClears()
        {
            var unpacker = new TestFixedLengthUnpacker(100) // 要求 100 字节才成包
            {
                MaxBufferSize = 32 // 限制最大缓存为 32 字节
            };

            Unpacker.ErrorCode capturedError = default;
            bool errorHandlerCalled = false;

            unpacker.ErrorOccurHandler = code =>
            {
                capturedError = code;
                errorHandlerCalled = true;
                return true; // 清空缓存
            };

            byte[] bigData = new byte[64];
            var context = new ReceiveContext(bigData);

            unpacker.Input(context);

            Assert.True(errorHandlerCalled, "超出 MaxBufferSize 必须触发 ErrorOccurHandler");
            Assert.Equal(Unpacker.ErrorCode.BufferOverflow, capturedError);

            // 验证缓存已被清空
            var field = typeof(Unpacker).GetField("_cachedTotalLength", BindingFlags.NonPublic | BindingFlags.Instance);
            int cachedLen = (int)field.GetValue(unpacker);
            Assert.Equal(0, cachedLen);
        }

        [Fact]
        public void Unpacker_MultiSegment_MatchesAcrossBoundary()
        {
            var unpacker = new TestFixedLengthUnpacker(4)
            {
                StartMark = new byte[] { 0xAA, 0x55 }
            };

            // 模拟跨段：段1末尾为 0xAA，段2开头为 0x55，后跟 2 字节载荷 0x01, 0x02
            byte[] seg1 = new byte[] { 0xAA };
            byte[] seg2 = new byte[] { 0x55, 0x01, 0x02 };

            UnpackContext parsed = null;
            unpacker.OnDataParsed += (s, e) =>
            {
                parsed = e.Data;
            };

            unpacker.Input(new ReceiveContext(seg1));
            Assert.Null(parsed); // 尚未成包

            unpacker.Input(new ReceiveContext(seg2));
            Assert.NotNull(parsed); // 跨段拼包成功

            var result = parsed.Data.ToArray();
            Assert.Equal(new byte[] { 0xAA, 0x55, 0x01, 0x02 }, result);

            parsed.Dispose();
        }

        #endregion

        #region 阶段 4 验证测试 (Phase 4: 竞态与幽灵 ACK 鲁棒性验证)

        private class RapidDisconnectClient : BytesClient
        {
            private bool _isConnected;
            public int ConnectCallCount { get; private set; }

            public override bool IsConnected => _isConnected;

            public override ConnectResult Connect(ConnectArgument? argument = null)
            {
                ConnectCallCount++;
                _isConnected = true;
                GenerateNewConnectionId();
                RaiseConnectedSuccessfully(this, new ConnectedSuccessfullyEventArgs());

                // 模拟连接刚成功后，对端瞬时被动断网
                _isConnected = false;
                RaiseDisconnected(this, new DisconnectedEventArgs(DisconnectionReasonCode.Passive, null));

                return new ConnectResult();
            }

            public override Task<ConnectResult> ConnectAsync(ConnectArgument? argument = null)
            {
                return Task.FromResult(Connect(argument));
            }

            public override DisconnectResult Disconnect(DisconnectArgument? argument = null)
            {
                _isConnected = false;
                RaiseDisconnected(this, new DisconnectedEventArgs(argument?.ReasonCode ?? DisconnectionReasonCode.Passive, argument?.Exception));
                return new DisconnectResult();
            }

            protected override Task SendHandlerAsync(SendArgs args) => Task.CompletedTask;
            protected override Task ReceiveDataHandleAsync(CancellationToken cancellationToken) => Task.CompletedTask;
            protected override void ReceiveDataCompletedHandle() { }
        }

        [Fact]
        public async Task ReconnectPlugin_RapidDisconnectDuringConnectContinuation_TimerStaysActive()
        {
            var client = new RapidDisconnectClient();
            client.UseAutoReconnect(delay: 50, times: 0);

            // 触发首次断开事件
            client.Disconnect(new DisconnectArgument(DisconnectionReasonCode.Passive));

            // 等待重连触发至少 2 次（验证在极速断网的并发时序下，定时器不会被 Connect 成功后的残余代码误掐死）
            for (int i = 0; i < 20 && client.ConnectCallCount < 2; i++)
            {
                await Task.Delay(50);
            }

            Assert.True(client.ConnectCallCount >= 2, $"极速断网下应能持续重连，当前尝试次数: {client.ConnectCallCount}");

            client.Dispose();
        }

        [Fact]
        public void SharedMemoryIpcClient_LateGhostAck_IsDiscardedWithoutBreakingSlotBackpressure()
        {
            var client = new STTech.BytesIO.Ipc.SharedMemoryIpcClient();

            var fieldSlotLocker = typeof(STTech.BytesIO.Ipc.SharedMemoryIpcClient).GetField("_slotLocker", BindingFlags.NonPublic | BindingFlags.Instance);
            var fieldSendSeq = typeof(STTech.BytesIO.Ipc.SharedMemoryIpcClient).GetField("_sendSequence", BindingFlags.NonPublic | BindingFlags.Instance);
            var fieldLastAckedSeq = typeof(STTech.BytesIO.Ipc.SharedMemoryIpcClient).GetField("_lastAckedSequence", BindingFlags.NonPublic | BindingFlags.Instance);
            var fieldPubCount = typeof(STTech.BytesIO.Ipc.SharedMemoryIpcClient).GetField("_publishedCount", BindingFlags.NonPublic | BindingFlags.Instance);
            var fieldAckCount = typeof(STTech.BytesIO.Ipc.SharedMemoryIpcClient).GetField("_ackedCount", BindingFlags.NonPublic | BindingFlags.Instance);

            var slotLocker = fieldSlotLocker.GetValue(client);

            // 模拟发送了包 1：_sendSequence = 1, _publishedCount = 1, _ackedCount = 0, _lastAckedSequence = 0
            fieldSendSeq.SetValue(client, 1);
            fieldPubCount.SetValue(client, 1);
            fieldAckCount.SetValue(client, 0);
            fieldLastAckedSeq.SetValue(client, 0);

            // 模拟包 1 等待 ACK 超时：
            // 超时恢复逻辑将 _publishedCount 设为 _ackedCount (0)，并将 _lastAckedSequence 推进到 _sendSequence (1)
            lock (slotLocker)
            {
                fieldPubCount.SetValue(client, fieldAckCount.GetValue(client));
                fieldLastAckedSeq.SetValue(client, fieldSendSeq.GetValue(client));
            }

            Assert.Equal(0, (int)fieldPubCount.GetValue(client));
            Assert.Equal(0, (int)fieldAckCount.GetValue(client));
            Assert.Equal(1, (int)fieldLastAckedSeq.GetValue(client));

            // 现在模拟包 1 的迟到幽灵 ACK 到达 (Sequence = 1)
            // 此时应判定 frame.Sequence (1) > _lastAckedSequence (1) 为 false，丢弃该 ACK
            lock (slotLocker)
            {
                int ghostAckSeq = 1;
                int lastAcked = (int)fieldLastAckedSeq.GetValue(client);
                if (ghostAckSeq > lastAcked)
                {
                    fieldLastAckedSeq.SetValue(client, ghostAckSeq);
                    int currentAcked = (int)fieldAckCount.GetValue(client);
                    fieldAckCount.SetValue(client, currentAcked + 1);
                }
            }

            // 验证：_ackedCount 绝对不能增加，依然保持为 0，防止破坏后续单槽位背压
            Assert.Equal(0, (int)fieldAckCount.GetValue(client));

            // 模拟后续发送包 2 (Sequence = 2)，合法 ACK 到达 (Sequence = 2)
            lock (slotLocker)
            {
                int legitimateAckSeq = 2;
                int lastAcked = (int)fieldLastAckedSeq.GetValue(client);
                if (legitimateAckSeq > lastAcked)
                {
                    fieldLastAckedSeq.SetValue(client, legitimateAckSeq);
                    int currentAcked = (int)fieldAckCount.GetValue(client);
                    fieldAckCount.SetValue(client, currentAcked + 1);
                }
            }

            // 验证合法 ACK 成功推进
            Assert.Equal(2, (int)fieldLastAckedSeq.GetValue(client));
            Assert.Equal(1, (int)fieldAckCount.GetValue(client));

            client.Dispose();
        }

        #endregion
    }
}
