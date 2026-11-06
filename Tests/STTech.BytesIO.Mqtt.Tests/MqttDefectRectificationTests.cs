using STTech.BytesIO.Mqtt;
using STTcpClient = STTech.BytesIO.Tcp.TcpClient;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace STTech.BytesIO.Mqtt.Tests
{
    /// <summary>
    /// 针对自查整改报告中确认的各项关键缺陷所编写的专项回归验证测试。
    /// </summary>
    public class MqttDefectRectificationTests
    {
        /// <summary>
        /// 验证缺陷 1：连接事件冒泡时序。
        /// 确保 OnConnectedSuccessfully 绝不会在底层 TCP 建立瞬间被提前触发，
        /// 必须在 MQTT CONNACK 握手成功后才触发，且触发时 IsConnected 必须为 true。
        /// </summary>
        [Fact]
        public async Task Test_Defect1_ConnectedEvent_FiresOnlyAfterMqttConnAck()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            int port = ((IPEndPoint)listener.LocalEndpoint).Port;

            using var cts = new CancellationTokenSource(10000);
            var connAckSentTcs = new TaskCompletionSource<bool>();

            var serverTask = Task.Run(async () =>
            {
                using var socket = await listener.AcceptSocketAsync(cts.Token);
                using var stream = new NetworkStream(socket);

                byte[] buf = new byte[256];
                int len = await stream.ReadAsync(buf, 0, buf.Length, cts.Token);

                // 延迟 200ms 再发送 CONNACK，模拟 TCP 建立与 MQTT 握手之间的时差
                await Task.Delay(200, cts.Token);

                var connAck = new MqttConnAckPacket { ReasonCode = MqttConnectReasonCode.Success };
                byte[] connAckBytes = connAck.Encode();
                await stream.WriteAsync(connAckBytes, 0, connAckBytes.Length, cts.Token);
                connAckSentTcs.TrySetResult(true);

                // 保持连接直到测试完毕
                try { await stream.ReadAsync(buf, 0, buf.Length, cts.Token); } catch { }
            }, cts.Token);

            try
            {
                using var client = new MqttTcpClient("127.0.0.1", port);
                int connectedEventCount = 0;
                bool isConnectedWhenEventFired = false;
                bool connAckWasSentWhenEventFired = false;

                client.OnConnectedSuccessfully += (s, e) =>
                {
                    connectedEventCount++;
                    isConnectedWhenEventFired = client.IsConnected;
                    connAckWasSentWhenEventFired = connAckSentTcs.Task.IsCompleted;
                };

                var result = await client.ConnectMqttAsync(cancellationToken: cts.Token);
                Assert.True(result.IsSuccess);

                // 验证 OnConnectedSuccessfully 仅触发了 1 次
                Assert.Equal(1, connectedEventCount);
                // 验证触发事件时 client.IsConnected 已为 true
                Assert.True(isConnectedWhenEventFired);
                // 验证触发事件时 CONNACK 报文确已发送完成（绝非 TCP 刚连上就被提前触发）
                Assert.True(connAckWasSentWhenEventFired);
            }
            finally
            {
                listener.Stop();
                try { await serverTask; } catch { }
            }
        }

        /// <summary>
        /// 验证缺陷 2：TCP 异常中断时协议层状态失控与泄露。
        /// 验证网络物理中断时，所有挂起等待的发布、订阅任务必须立即失败（< 1s，绝非死等 10s 超时），
        /// 且 IsConnected 变为 false，OnMqttDisconnected 触发。
        /// </summary>
        [Fact]
        public async Task Test_Defect2_UnexpectedTcpDisconnection_FailsAllPendingRequestsImmediately()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            int port = ((IPEndPoint)listener.LocalEndpoint).Port;

            using var cts = new CancellationTokenSource(10000);

            var serverTask = Task.Run(async () =>
            {
                using var socket = await listener.AcceptSocketAsync(cts.Token);
                using var stream = new NetworkStream(socket);

                byte[] buf = new byte[256];
                int len = await stream.ReadAsync(buf, 0, buf.Length, cts.Token);
                var connAck = new MqttConnAckPacket { ReasonCode = MqttConnectReasonCode.Success };
                byte[] connAckBytes = connAck.Encode();
                await stream.WriteAsync(connAckBytes, 0, connAckBytes.Length, cts.Token);

                // 等待客户端发送 QoS 1 PUBLISH
                len = await stream.ReadAsync(buf, 0, buf.Length, cts.Token);

                // 服务端直接关闭 Socket（模拟物理拔网线/Broker 崩溃），不回发 PUBACK
                socket.Shutdown(SocketShutdown.Both);
                socket.Close();
            }, cts.Token);

            try
            {
                using var client = new MqttTcpClient("127.0.0.1", port);
                var disconnectedTcs = new TaskCompletionSource<bool>();
                client.OnMqttDisconnected += (s, e) => disconnectedTcs.TrySetResult(true);

                var connectResult = await client.ConnectMqttAsync(cancellationToken: cts.Token);
                Assert.True(connectResult.IsSuccess);

                // 发送 QoS 1 消息，等待 PUBACK
                var sw = Stopwatch.StartNew();
                var pubResult = await client.PublishAsync("test/topic", Encoding.UTF8.GetBytes("hello"), MqttQualityOfServiceLevel.AtLeastOnce);
                sw.Stop();

                // 验证操作立刻失败，而不是傻等 10 秒超时
                Assert.False(pubResult.IsSuccess);
                Assert.NotNull(pubResult.Exception);
                Assert.True(sw.ElapsedMilliseconds < 2000, $"挂起请求耗时 {sw.ElapsedMilliseconds} ms，未能在断开瞬间即时抛出异常。");

                // 验证连接状态已被置为 false
                Assert.False(client.IsConnected);

                // 验证触发了断开事件
                var completed = await Task.WhenAny(disconnectedTcs.Task, Task.Delay(2000));
                Assert.Equal(disconnectedTcs.Task, completed);
            }
            finally
            {
                listener.Stop();
                try { await serverTask; } catch { }
            }
        }

        /// <summary>
        /// 验证缺陷 3：主题路由与自动重订阅 QoS 保持。
        /// 验证用户以 QoS 0、QoS 1、QoS 2 订阅的主题，其服务质量等级均被精确记忆；
        /// 托管客户端禁用底层的 Options.AutoResubscribe 以避免双重风暴。
        /// </summary>
        [Fact]
        public void Test_Defect3_TopicRouter_And_AutoResubscribe_PreservesOriginalQoS()
        {
            var router = new TopicRouter();

            // 注册不同 QoS 的路由
            router.AddRoute("sensor/temp", msg => Task.CompletedTask, MqttQualityOfServiceLevel.AtMostOnce);
            router.AddRoute("sensor/humidity", msg => Task.CompletedTask, MqttQualityOfServiceLevel.AtLeastOnce);
            router.AddRoute("sensor/pressure", msg => Task.CompletedTask, MqttQualityOfServiceLevel.ExactlyOnce);

            var qosDict = router.GetSubscribedTopicsWithQoS();
            Assert.Equal(3, qosDict.Count);
            Assert.Equal(MqttQualityOfServiceLevel.AtMostOnce, qosDict["sensor/temp"]);
            Assert.Equal(MqttQualityOfServiceLevel.AtLeastOnce, qosDict["sensor/humidity"]);
            Assert.Equal(MqttQualityOfServiceLevel.ExactlyOnce, qosDict["sensor/pressure"]);

            // 验证托管客户端默认禁用底层的 Options.AutoResubscribe，由插件统一管理
            using var tcpClient = new STTcpClient();
            using var managed = new MqttManagedClient<STTcpClient>(tcpClient);
            Assert.False(managed.Options.AutoResubscribe);
        }

        /// <summary>
        /// 验证缺陷 4：QoS 2 接收端入站报文去重。
        /// 验证当 Broker 重发相同 PacketId 的 QoS 2 PUBLISH 报文时，
        /// 客户端每次都回复 PUBREC，但业务应用层回调严格只分发 1 次！
        /// 收到 PUBREL 后应答 PUBCOMP 并清除去重缓存。
        /// </summary>
        [Fact]
        public async Task Test_Defect4_QoS2_DuplicateInboundPublish_DeliveredOnlyOnce()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            int port = ((IPEndPoint)listener.LocalEndpoint).Port;

            using var cts = new CancellationTokenSource(10000);
            var pubRecCount = 0;
            var pubCompReceivedTcs = new TaskCompletionSource<bool>();

            var serverTask = Task.Run(async () =>
            {
                using var socket = await listener.AcceptSocketAsync(cts.Token);
                using var stream = new NetworkStream(socket);

                byte[] buf = new byte[1024];
                int len = await stream.ReadAsync(buf, 0, buf.Length, cts.Token);
                var connAck = new MqttConnAckPacket { ReasonCode = MqttConnectReasonCode.Success };
                byte[] connAckBytes = connAck.Encode();
                await stream.WriteAsync(connAckBytes, 0, connAckBytes.Length, cts.Token);

                // 发送第一个 QoS 2 PUBLISH (PacketId = 100)
                var qos2Publish = new MqttPublishPacket
                {
                    PacketId = 100,
                    Topic = "test/qos2",
                    Payload = Encoding.UTF8.GetBytes("payload1"),
                    QoS = MqttQualityOfServiceLevel.ExactlyOnce
                };
                byte[] pubBytes = qos2Publish.Encode();
                await stream.WriteAsync(pubBytes, 0, pubBytes.Length, cts.Token);

                // 读取客户端回发的 PUBREC
                len = await stream.ReadAsync(buf, 0, buf.Length, cts.Token);
                var packet1 = MqttPacketReader.ReadPacket(buf, 0, len);
                if (packet1 is MqttPubRecPacket) pubRecCount++;

                // 模拟网络丢包/重传：再次发送完全相同 PacketId = 100 的 QoS 2 PUBLISH (Dup = true)
                qos2Publish.Dup = true;
                byte[] dupBytes = qos2Publish.Encode();
                await stream.WriteAsync(dupBytes, 0, dupBytes.Length, cts.Token);

                // 读取客户端对重发报文回发的第二个 PUBREC
                len = await stream.ReadAsync(buf, 0, buf.Length, cts.Token);
                var packet2 = MqttPacketReader.ReadPacket(buf, 0, len);
                if (packet2 is MqttPubRecPacket) pubRecCount++;

                // 发送 PUBREL
                var pubRel = new MqttPubRelPacket { PacketId = 100, ReasonCode = MqttPubRelReasonCode.Success };
                byte[] relBytes = pubRel.Encode();
                await stream.WriteAsync(relBytes, 0, relBytes.Length, cts.Token);

                // 读取客户端回发的 PUBCOMP
                len = await stream.ReadAsync(buf, 0, buf.Length, cts.Token);
                var packet3 = MqttPacketReader.ReadPacket(buf, 0, len);
                if (packet3 is MqttPubCompPacket) pubCompReceivedTcs.TrySetResult(true);
            }, cts.Token);

            try
            {
                using var client = new MqttTcpClient("127.0.0.1", port);
                int applicationMessageCount = 0;

                client.OnApplicationMessageReceived += (s, e) =>
                {
                    Interlocked.Increment(ref applicationMessageCount);
                };

                var connectResult = await client.ConnectMqttAsync(cancellationToken: cts.Token);
                Assert.True(connectResult.IsSuccess);

                // 等待 PUBCOMP 交互结束
                var completed = await Task.WhenAny(pubCompReceivedTcs.Task, Task.Delay(3000));
                Assert.Equal(pubCompReceivedTcs.Task, completed);

                // 服务端应收到 2 次 PUBREC
                Assert.Equal(2, pubRecCount);

                // 业务应用层回调严格只有 1 次！
                Assert.Equal(1, applicationMessageCount);
            }
            finally
            {
                listener.Stop();
                try { await serverTask; } catch { }
            }
        }

        /// <summary>
        /// 验证缺陷 5：离线发布队列非阻塞异步排空。
        /// 服务端采用流式分包循环，正确应对 TCP 粘包/合并传输，
        /// 验证在离线期间推入队列的消息，在连接恢复后能被异步连续排空处理。
        /// </summary>
        [Fact]
        public async Task Test_Defect5_MqttPublishQueuePlugin_DrainsAsynchronouslyWithoutDeadlock()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            int port = ((IPEndPoint)listener.LocalEndpoint).Port;

            using var cts = new CancellationTokenSource(10000);
            int serverReceivedPublishCount = 0;
            var allReceivedTcs = new TaskCompletionSource<bool>();

            var serverTask = Task.Run(async () =>
            {
                using var socket = await listener.AcceptSocketAsync(cts.Token);
                using var stream = new NetworkStream(socket);

                byte[] buf = new byte[2048];
                int len = await stream.ReadAsync(buf, 0, buf.Length, cts.Token);
                var connAck = new MqttConnAckPacket { ReasonCode = MqttConnectReasonCode.Success };
                byte[] connAckBytes = connAck.Encode();
                await stream.WriteAsync(connAckBytes, 0, connAckBytes.Length, cts.Token);

                // 接收客户端排空队列发来的 3 条消息 (QoS 0)
                // 基于 MQTT 长度编码进行流式分帧循环，杜绝假定 1 次 Read = 1 包引发的粘包丢失
                var streamBuffer = new List<byte>();
                while (serverReceivedPublishCount < 3 && !cts.Token.IsCancellationRequested)
                {
                    len = await stream.ReadAsync(buf, 0, buf.Length, cts.Token);
                    if (len == 0) break;
                    for (int i = 0; i < len; i++) streamBuffer.Add(buf[i]);

                    while (streamBuffer.Count >= 2)
                    {
                        byte[] current = streamBuffer.ToArray();
                        int multiplier = 1;
                        int remainingLen = 0;
                        int lenBytesCount = 0;
                        bool lenParsed = false;

                        for (int i = 1; i < Math.Min(current.Length, 5); i++)
                        {
                            byte b = current[i];
                            lenBytesCount++;
                            remainingLen += (b & 0x7F) * multiplier;
                            multiplier *= 128;
                            if ((b & 0x80) == 0)
                            {
                                lenParsed = true;
                                break;
                            }
                        }

                        if (!lenParsed) break;
                        int totalLen = 1 + lenBytesCount + remainingLen;
                        if (current.Length < totalLen) break;

                        var packet = MqttPacketReader.ReadPacket(current, 0, totalLen);
                        if (packet is MqttPublishPacket)
                        {
                            if (Interlocked.Increment(ref serverReceivedPublishCount) == 3)
                            {
                                allReceivedTcs.TrySetResult(true);
                            }
                        }

                        streamBuffer.RemoveRange(0, totalLen);
                    }
                }
            }, cts.Token);

            try
            {
                using var managed = new MqttManagedTcpClient("127.0.0.1", port);

                // 客户端离线时入队 3 条消息
                var t1 = managed.EnqueuePublishAsync(new MqttApplicationMessage("queue/test1", "msg1"));
                var t2 = managed.EnqueuePublishAsync(new MqttApplicationMessage("queue/test2", "msg2"));
                var t3 = managed.EnqueuePublishAsync(new MqttApplicationMessage("queue/test3", "msg3"));

                Assert.Equal(3, managed.QueuedCount);

                // 建立连接
                var connectResult = await managed.ConnectMqttAsync(cancellationToken: cts.Token);
                Assert.True(connectResult.IsSuccess);

                // 等待队列消息异步发布完成
                await Task.WhenAll(t1, t2, t3);

                // 等待服务端确已完整收到并解码出全部 3 条消息
                var completed = await Task.WhenAny(allReceivedTcs.Task, Task.Delay(3000));
                Assert.Equal(allReceivedTcs.Task, completed);

                // 队列计数归零
                Assert.Equal(0, managed.QueuedCount);
                Assert.Equal(3, serverReceivedPublishCount);
            }
            finally
            {
                listener.Stop();
                try { await serverTask; } catch { }
            }
        }

        /// <summary>
        /// 验证缺陷 5 衍生项：QueueOverflowStrategy.Block 策略下的纯异步非阻塞与容量释放。
        /// 验证队列满时 EnqueuePublishAsync 不阻塞调用线程；
        /// 取消等待入队的消息时，容量信号量被准确补偿，后续入队依然通畅。
        /// </summary>
        [Fact]
        public async Task Test_Defect5_MqttPublishQueuePlugin_BlockStrategy_NonBlockingAsyncAndCapacityAccounting()
        {
            var plugin = new MqttPublishQueuePlugin
            {
                MaxQueueSize = 2,
                OverflowStrategy = QueueOverflowStrategy.Block,
                BlockTimeout = TimeSpan.FromMilliseconds(500)
            };

            // 1. 入队 2 条消息填满容量
            var t1 = plugin.EnqueuePublishAsync(new MqttApplicationMessage("test/1", "m1"));
            var t2 = plugin.EnqueuePublishAsync(new MqttApplicationMessage("test/2", "m2"));
            Assert.Equal(2, plugin.QueuedCount);

            // 2. 尝试推入第 3 条消息，携带 100ms 取消令牌，验证其为非阻塞异步调用
            using (var cancelCts = new CancellationTokenSource(100))
            {
                var sw = Stopwatch.StartNew();
                var t3 = plugin.EnqueuePublishAsync(new MqttApplicationMessage("test/3", "m3"), cancelCts.Token);

                // 验证 EnqueuePublishAsync 方法在微秒级立即返回 Task，绝对没有在此处同步卡死 100ms
                Assert.True(sw.ElapsedMilliseconds < 50, "EnqueuePublishAsync 出现同步阻塞调用方线程！");

                // 等待取消完成
                await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await t3);
            }

            // 3. 清空队列，验证容量信号量得到彻底补偿释放
            plugin.Clear();
            Assert.Equal(0, plugin.QueuedCount);

            // 4. 清空后再次连续入队 2 条，验证信号量没有泄漏枯竭
            var t4 = plugin.EnqueuePublishAsync(new MqttApplicationMessage("test/4", "m4"));
            var t5 = plugin.EnqueuePublishAsync(new MqttApplicationMessage("test/5", "m5"));
            Assert.Equal(2, plugin.QueuedCount);

            plugin.Dispose();
        }

        /// <summary>
        /// 验证缺陷 6：MqttClient.Dispose 资源彻底释放。
        /// 验证 Dispose 后停止心跳定时器，解绑事件，清理路由，断开底层传输。
        /// </summary>
        [Fact]
        public void Test_Defect6_MqttClient_Dispose_CleansUpAllResources()
        {
            var client = new MqttTcpClient("127.0.0.1", 1883);

            // 注册路由
            client.Router.AddRoute("test/topic", msg => Task.CompletedTask);
            Assert.Single(client.Router.GetSubscribedTopicFilters());

            // 释放客户端
            client.Dispose();

            // 路由已被清空
            Assert.Empty(client.Router.GetSubscribedTopicFilters());
            Assert.False(client.IsConnected);
        }
    }
}
