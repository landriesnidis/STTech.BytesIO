using STTech.BytesIO.Core;
using STTech.BytesIO.Core.Component;
using STTech.BytesIO.Tcp;
using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Net;
using System.Net.Security;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace STTech.BytesIO.Core.Tests
{
    public class DeepAuditRemediationTests
    {
        private class TestClient : BytesClient
        {
            public bool ConnectedState = true;
            public override bool IsConnected => ConnectedState;
            public override ConnectResult Connect(ConnectArgument? argument = null) => new ConnectResult();
            public override Task<ConnectResult> ConnectAsync(ConnectArgument? argument = null) => Task.FromResult(new ConnectResult());
            public override DisconnectResult Disconnect(DisconnectArgument? argument = null)
            {
                ConnectedState = false;
                RaiseDisconnected(this, new DisconnectedEventArgs(DisconnectionReasonCode.Active));
                return new DisconnectResult();
            }
            protected override Task SendHandlerAsync(SendArgs args) => Task.CompletedTask;
            protected override Task ReceiveDataHandleAsync(CancellationToken cancellationToken) => Task.CompletedTask;
            protected override void ReceiveDataCompletedHandle() { }

            public void SimulateReceive(byte[] data)
            {
                var ctx = CreateReceiveContext(data, 0, data.Length);
                InvokeDataReceivedEventCallback(ctx);
            }

            public void SimulateConnected()
            {
                ConnectedState = true;
                RaiseConnectedSuccessfully(this, new ConnectedSuccessfullyEventArgs());
            }

            public void SimulateDisconnected()
            {
                ConnectedState = false;
                RaiseDisconnected(this, new DisconnectedEventArgs(DisconnectionReasonCode.Passive));
            }
        }

        private class FailingConnectClient : BytesClient
        {
            public override bool IsConnected => false;
            public override ConnectResult Connect(ConnectArgument? argument = null) => new ConnectResult(ConnectErrorCode.Error);
            public override Task<ConnectResult> ConnectAsync(ConnectArgument? argument = null) => Task.FromResult(new ConnectResult(ConnectErrorCode.Error));
            public override DisconnectResult Disconnect(DisconnectArgument? argument = null) => new DisconnectResult();
            protected override Task SendHandlerAsync(SendArgs args) => Task.CompletedTask;
            protected override Task ReceiveDataHandleAsync(CancellationToken cancellationToken) => Task.CompletedTask;
            protected override void ReceiveDataCompletedHandle() { }
        }

        private class SequenceSegment : ReadOnlySequenceSegment<byte>
        {
            public SequenceSegment(ReadOnlyMemory<byte> memory) => Memory = memory;
            public SequenceSegment Append(ReadOnlyMemory<byte> memory)
            {
                var next = new SequenceSegment(memory) { RunningIndex = RunningIndex + Memory.Length };
                Next = next;
                return next;
            }
        }

        private static ReadOnlySequence<byte> BuildMultiSegmentSequence(params byte[][] chunks)
        {
            if (chunks.Length == 0) return ReadOnlySequence<byte>.Empty;
            var first = new SequenceSegment(chunks[0]);
            var last = first;
            for (int i = 1; i < chunks.Length; i++)
            {
                last = last.Append(chunks[i]);
            }
            return new ReadOnlySequence<byte>(first, 0, last, last.Memory.Length);
        }

        #region 1. UnpackContext 显式释放与 ArrayPool 防穿透验证

        [Fact]
        public void UnpackerT_ExplicitlyDisposesUnpackContext_AfterParsing()
        {
            UnpackContext? capturedContext = null;
            var unpacker = new Unpacker<string>(
                lengthCalculator: seq => (int)seq.Length,
                dataParser: ctx =>
                {
                    capturedContext = ctx;
                    return "ParsedString";
                }
            );

            bool parsedFired = false;
            unpacker.OnDataParsed += (s, e) =>
            {
                parsedFired = true;
                Assert.Equal("ParsedString", e.Data);
            };

            var testData = new byte[] { 0xAA, 0xBB, 0xCC };
            var rc = new ReceiveContext(testData, 0, testData.Length, () => { });

            // 模拟解包器接收
            unpacker.Input(rc);

            Assert.True(parsedFired);
            Assert.NotNull(capturedContext);
            // 验证解包后立即调用了 Dispose，避免了堆积到 GC 终结器队列
            Assert.True(capturedContext!.IsDisposed, "UnpackContext 必须在强类型解析完成后被显式 Dispose，杜绝依赖 Finalizer 堆积");
        }

        #endregion

        #region 2. ReceiveContext.Retain 与引用计数生命周期验证 (防 UAF)

        [Fact]
        public void ReceiveContext_RetainAndReferenceCount_PreventsPrematureRelease()
        {
            bool memoryReleased = false;
            var rawData = new byte[] { 1, 2, 3, 4, 5 };
            var ctx = new ReceiveContext(rawData, 0, rawData.Length, () => { memoryReleased = true; });

            Assert.Equal(1, ctx.ReferenceCount);

            // 模拟用户在异步回调开始时调用 Retain()
            ctx.Retain();
            Assert.Equal(2, ctx.ReferenceCount);

            // 框架在事件回调结束后调用 Dispose()
            ctx.Dispose();
            Assert.Equal(1, ctx.ReferenceCount);
            Assert.False(ctx.IsDisposed, "调用 Retain 后，框架的第一轮 Dispose 不应释放底层内存");
            Assert.False(memoryReleased);

            // 用户的异步任务处理完成后调用配对的 Dispose()
            ctx.Dispose();
            Assert.Equal(0, ctx.ReferenceCount);
            Assert.True(ctx.IsDisposed);
            Assert.True(memoryReleased, "引用计数归零时底层内存必须被正确释放");
        }

        #endregion

        #region 3. ReceiveContext 基于 BinaryPrimitives 的零分配数值解析验证

        [Fact]
        public void ReceiveContext_BinaryPrimitives_EndianParsingAccuracy()
        {
            var buffer = new byte[32];

            // 写入大端
            BinaryPrimitives.WriteInt16BigEndian(buffer.AsSpan(0, 2), 0x1234);
            BinaryPrimitives.WriteInt32BigEndian(buffer.AsSpan(2, 4), 0x12345678);
            BinaryPrimitives.WriteInt64BigEndian(buffer.AsSpan(6, 8), 0x1122334455667788);

            // 写入小端
            BinaryPrimitives.WriteInt16LittleEndian(buffer.AsSpan(14, 2), 0x1234);
            BinaryPrimitives.WriteInt32LittleEndian(buffer.AsSpan(16, 4), 0x12345678);
            BinaryPrimitives.WriteInt64LittleEndian(buffer.AsSpan(20, 8), 0x1122334455667788);

            var ctx = new ReceiveContext(buffer, 0, buffer.Length, () => { });

            // 大端读取
            Assert.Equal(0x1234, ctx.ToInt16(0, bigEndian: true));
            Assert.Equal(0x12345678, ctx.ToInt32(2, bigEndian: true));
            Assert.Equal(0x1122334455667788, ctx.ToInt64(6, bigEndian: true));

            // 小端读取
            Assert.Equal(0x1234, ctx.ToInt16(14, bigEndian: false));
            Assert.Equal(0x12345678, ctx.ToInt32(16, bigEndian: false));
            Assert.Equal(0x1122334455667788, ctx.ToInt64(20, bigEndian: false));

            // 浮点数解析
            var floatBuf = new byte[8];
            float originalFloat = 3.14159f;
            int floatBits = BitConverter.ToInt32(BitConverter.GetBytes(originalFloat), 0);
            BinaryPrimitives.WriteInt32BigEndian(floatBuf.AsSpan(0, 4), floatBits);
            var floatCtx = new ReceiveContext(floatBuf, 0, 4, () => { });
            Assert.Equal(originalFloat, floatCtx.ToSingle(0, bigEndian: true), 5);
        }

        #endregion

        #region 4. ReconnectPlugin 与 TryConnectPlugin 异常崩溃防御验证

        [Fact]
        public void ReconnectPlugin_UserCallbackThrowsException_DoesNotCrashProcess()
        {
            var client = new FailingConnectClient();
            var plugin = new ReconnectPlugin();
            client.AddPlugin(plugin);

            var exceptionSignal = new ManualResetEventSlim(false);
            bool exceptionCaught = false;

            client.OnExceptionOccurs += (s, e) =>
            {
                if (e.Exception.Message.Contains("Simulated user callback exception"))
                {
                    exceptionCaught = true;
                    exceptionSignal.Set();
                }
            };

            // 配置失败回调，并在回调中抛出未捕获异常
            plugin.Configure(
                delay: 20,
                times: 1,
                reconnectFailedHandler: c => throw new InvalidOperationException("Simulated user callback exception")
            );

            // 模拟触发断开以唤醒重连定时器
            plugin.OnDisconnected(new DisconnectedEventArgs(DisconnectionReasonCode.Passive));

            // 响应式等待最多 3000ms
            bool signaled = exceptionSignal.Wait(3000);

            Assert.True(signaled && exceptionCaught, "async void 内部抛出的用户回调异常必须被捕获并通过 OnExceptionOccurs 抛出，绝不能崩死宿主进程");
            plugin.Dispose();
        }

        [Fact]
        public void TryConnectPlugin_UserCallbackThrowsException_DoesNotCrashProcess()
        {
            var client = new FailingConnectClient();
            var plugin = new TryConnectPlugin();
            client.AddPlugin(plugin);

            var exceptionSignal = new ManualResetEventSlim(false);
            bool exceptionCaught = false;

            client.OnExceptionOccurs += (s, e) =>
            {
                if (e.Exception.Message.Contains("Simulated try-connect callback exception"))
                {
                    exceptionCaught = true;
                    exceptionSignal.Set();
                }
            };

            // 启动定时连接并在失败回调中抛出异常
            plugin.Configure(
                delay: 20,
                times: 1,
                connectFailedHandler: c => throw new InvalidOperationException("Simulated try-connect callback exception")
            );

            // 响应式等待最多 3000ms
            bool signaled = exceptionSignal.Wait(3000);

            Assert.True(signaled && exceptionCaught, "TryConnectPlugin 中的用户回调异常必须被安全防护");
            plugin.Dispose();
        }

        #endregion

        #region 5. HeartbeatPlugin 优化验证 (断开暂停与时间戳防振荡)

        [Fact]
        public void HeartbeatPlugin_Disconnected_PausesTimerAndRecoversOnReconnect()
        {
            var client = new TestClient();
            var plugin = new HeartbeatPlugin();
            client.AddPlugin(plugin);

            var beatSignal = new AutoResetEvent(false);
            int sentCount = 0;

            plugin.ConfigureHeartbeat(c =>
            {
                Interlocked.Increment(ref sentCount);
                beatSignal.Set();
            }, interval: 25);
            plugin.ConfigureTimeout(timeout: 500);

            client.SimulateConnected();

            // 响应式等待至少收到一次心跳
            bool receivedInitialBeat = beatSignal.WaitOne(2000);
            Assert.True(receivedInitialBeat, "连接状态下心跳必须按时发送");

            // 模拟断开连接
            client.SimulateDisconnected();
            Thread.Sleep(100);
            int countAfterDisconnect = Volatile.Read(ref sentCount);

            // 再等待一段时间确认在断连期间没有再产生心跳
            bool ghostBeat = beatSignal.WaitOne(150);
            Assert.False(ghostBeat, "断开连接后定时器必须被暂停，绝不产生幽灵心跳");
            Assert.Equal(countAfterDisconnect, Volatile.Read(ref sentCount));

            // 重新连接后应恢复发送
            client.SimulateConnected();
            bool recoveredBeat = beatSignal.WaitOne(2000);
            Assert.True(recoveredBeat, "重连后心跳定时器必须自动恢复发送");

            plugin.Dispose();
        }

        #endregion

        #region 6. Unpacker.IndexOf 多段跨段检索算法验证

        [Fact]
        public void Unpacker_IndexOf_MultiSegmentSearch_AccurateAndFast()
        {
            var targetHeader = new byte[] { 0x5A, 0xA5, 0x12, 0x34 };

            // 场景 1：目标头恰好跨在两个段的边界上
            var chunk1 = new byte[] { 0x01, 0x02, 0x5A, 0xA5 };
            var chunk2 = new byte[] { 0x12, 0x34, 0x03, 0x04 };
            var seqCross = BuildMultiSegmentSequence(chunk1, chunk2);

            var indexOfMethod = typeof(Unpacker).GetMethod("IndexOf", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(indexOfMethod);

            int crossIndex = (int)indexOfMethod!.Invoke(null, new object[] { seqCross, targetHeader })!;
            Assert.Equal(2, crossIndex);

            // 场景 2：目标头跨在第 3 个段上
            var chunkA = new byte[] { 0x00, 0x00 };
            var chunkB = new byte[] { 0x01, 0x02, 0x03 };
            var chunkC = new byte[] { 0x5A, 0xA5, 0x12, 0x34, 0x99 };
            var seqMulti = BuildMultiSegmentSequence(chunkA, chunkB, chunkC);

            int multiIndex = (int)indexOfMethod.Invoke(null, new object[] { seqMulti, targetHeader })!;
            Assert.Equal(5, multiIndex);

            // 场景 3：未命中
            var noMatch = new byte[] { 0xFF, 0xEE, 0xDD, 0xCC };
            int notFound = (int)indexOfMethod.Invoke(null, new object[] { seqMulti, noMatch })!;
            Assert.Equal(-1, notFound);
        }

        #endregion

        #region 7. TcpClient.RemoteEndPoint 状态安全与 SSL 默认策略验证

        [Fact]
        public void TcpClient_RemoteEndPoint_DoesNotThrowWhenDisconnected()
        {
            using var client = new TcpClient();
            var remoteEp = client.RemoteEndPoint;
            Assert.Null(remoteEp);
        }

        [Fact]
        public void TcpClient_SslCertificateValidation_DefaultPolicyIsStrict()
        {
            using var client = new TcpClient();
            Assert.False(client.DangerousAcceptAnyServerCertificate, "默认必须禁用危险的证书全放行");

            var callbackMethod = typeof(TcpClient).GetMethod("RemoteCertificateValidateCallback", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(callbackMethod);

            // 默认情况下有 SSL 策略错误时必须返回 false
            bool passWithErrors = (bool)callbackMethod!.Invoke(client, new object?[] { null, null, null, SslPolicyErrors.RemoteCertificateChainErrors })!;
            Assert.False(passWithErrors, "默认严谨策略下，存在证书链错误时必须拒绝验证");

            // 无错误时通过
            bool passWithoutErrors = (bool)callbackMethod.Invoke(client, new object?[] { null, null, null, SslPolicyErrors.None })!;
            Assert.True(passWithoutErrors);

            // 显式开启 DangerousAcceptAnyServerCertificate 时放行
            client.DangerousAcceptAnyServerCertificate = true;
            bool passWhenDangerous = (bool)callbackMethod.Invoke(client, new object?[] { null, null, null, SslPolicyErrors.RemoteCertificateChainErrors })!;
            Assert.True(passWhenDangerous, "显式开启 DangerousAcceptAnyServerCertificate 时允许放行自签名证书");
        }

        #endregion
    }
}
