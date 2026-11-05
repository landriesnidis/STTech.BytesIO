using STTech.BytesIO.Mqtt;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace STTech.BytesIO.Mqtt.Tests
{
    public class MqttPluginTests
    {
        private static async Task<MqttPacket> ReadPacketFromStreamAsync(Stream stream, CancellationToken cancellationToken)
        {
            byte[] header = new byte[2];
            int read = await stream.ReadAsync(header, 0, 2, cancellationToken);
            if (read < 2) throw new EndOfStreamException();

            byte firstByte = header[0];
            int multiplier = 1;
            int remainingLength = 0;
            int lengthBytesCount = 0;
            byte b = header[1];
            lengthBytesCount++;
            remainingLength += (b & 0x7F) * multiplier;
            multiplier *= 128;

            while ((b & 0x80) != 0)
            {
                byte[] nextByte = new byte[1];
                await stream.ReadAsync(nextByte, 0, 1, cancellationToken);
                b = nextByte[0];
                lengthBytesCount++;
                remainingLength += (b & 0x7F) * multiplier;
                multiplier *= 128;
            }

            byte[] body = new byte[remainingLength];
            int totalBodyRead = 0;
            while (totalBodyRead < remainingLength)
            {
                int r = await stream.ReadAsync(body, totalBodyRead, remainingLength - totalBodyRead, cancellationToken);
                if (r <= 0) throw new EndOfStreamException();
                totalBodyRead += r;
            }

            byte[] fullPacket = new byte[1 + lengthBytesCount + remainingLength];
            fullPacket[0] = firstByte;
            int tempLen = remainingLength;
            int offset = 1;
            do
            {
                byte eb = (byte)(tempLen % 128);
                tempLen /= 128;
                if (tempLen > 0) eb |= 0x80;
                fullPacket[offset++] = eb;
            } while (tempLen > 0);

            Buffer.BlockCopy(body, 0, fullPacket, offset, remainingLength);
            return MqttPacketReader.ReadPacket(fullPacket);
        }

        [Fact]
        public async Task PublishQueuePlugin_OfflineEnqueueAndDrainOnConnect_Success()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            int port = ((IPEndPoint)listener.LocalEndpoint).Port;

            var receivedPackets = new List<MqttPacket>();
            var serverCts = new CancellationTokenSource(TimeSpan.FromSeconds(15));

            var serverTask = Task.Run(async () =>
            {
                using var socket = await listener.AcceptSocketAsync(serverCts.Token);
                using var stream = new NetworkStream(socket);

                var connectPacket = await ReadPacketFromStreamAsync(stream, serverCts.Token);
                Assert.IsType<MqttConnectPacket>(connectPacket);

                var connAck = new MqttConnAckPacket
                {
                    ReasonCode = MqttConnectReasonCode.Success,
                    SessionPresent = false
                };
                byte[] connAckBytes = connAck.Encode();
                await stream.WriteAsync(connAckBytes, 0, connAckBytes.Length, serverCts.Token);

                while (!serverCts.IsCancellationRequested)
                {
                    MqttPacket packet = null;
                    try
                    {
                        packet = await ReadPacketFromStreamAsync(stream, serverCts.Token);
                    }
                    catch { break; }

                    if (packet == null) break;
                    lock (receivedPackets)
                    {
                        receivedPackets.Add(packet);
                    }

                    if (packet is MqttPublishPacket pub && pub.QoS == MqttQualityOfServiceLevel.AtLeastOnce)
                    {
                        var pubAck = new MqttPubAckPacket { PacketId = pub.PacketId, ReasonCode = MqttPubAckReasonCode.Success };
                        byte[] b = pubAck.Encode();
                        await stream.WriteAsync(b, 0, b.Length, serverCts.Token);
                    }

                    if (receivedPackets.Count >= 2)
                    {
                        break;
                    }
                }
            });

            var client = new MqttTcpClient("127.0.0.1", port);
            var queuePlugin = new MqttPublishQueuePlugin();
            client.AddPlugin(queuePlugin);

            // 1. 在断线状态下推入2条消息
            var msg1 = new MqttApplicationMessage("test/offline/1", Encoding.UTF8.GetBytes("payload1"), MqttQualityOfServiceLevel.AtMostOnce);
            var msg2 = new MqttApplicationMessage("test/offline/2", Encoding.UTF8.GetBytes("payload2"), MqttQualityOfServiceLevel.AtLeastOnce);

            var task1 = client.EnqueuePublishAsync(msg1);
            var task2 = client.EnqueuePublishAsync(msg2);

            Assert.Equal(2, queuePlugin.QueuedCount);

            // 2. 触发连接
            var connectResult = await client.ConnectMqttAsync();
            Assert.True(connectResult.IsSuccess);

            // 3. 等待队列排空
            var res1 = await task1;
            var res2 = await task2;

            Assert.True(res1.IsSuccess);
            Assert.True(res2.IsSuccess);
            Assert.Equal(0, queuePlugin.QueuedCount);

            await serverTask;
            listener.Stop();
            client.Dispose();
        }

        [Fact]
        public void PublishQueuePlugin_OverflowStrategies_WorkAsExpected()
        {
            var client = new MqttTcpClient("127.0.0.1", 1883);
            var queuePlugin = new MqttPublishQueuePlugin
            {
                MaxQueueSize = 2,
                OverflowStrategy = QueueOverflowStrategy.DropOldest
            };
            client.AddPlugin(queuePlugin);

            var msg1 = new MqttApplicationMessage("topic1", new byte[] { 1 });
            var msg2 = new MqttApplicationMessage("topic2", new byte[] { 2 });
            var msg3 = new MqttApplicationMessage("topic3", new byte[] { 3 });

            var t1 = queuePlugin.EnqueuePublishAsync(msg1);
            var t2 = queuePlugin.EnqueuePublishAsync(msg2);

            Assert.Equal(2, queuePlugin.QueuedCount);

            // DropOldest: msg1 should be dropped and exception set
            var t3 = queuePlugin.EnqueuePublishAsync(msg3);
            Assert.Equal(2, queuePlugin.QueuedCount);
            Assert.True(t1.IsFaulted);

            // Test DropNewest
            queuePlugin.OverflowStrategy = QueueOverflowStrategy.DropNewest;
            var msg4 = new MqttApplicationMessage("topic4", new byte[] { 4 });
            var t4 = queuePlugin.EnqueuePublishAsync(msg4);
            Assert.True(t4.IsFaulted);
            Assert.Equal(2, queuePlugin.QueuedCount);

            // Test ThrowException
            queuePlugin.OverflowStrategy = QueueOverflowStrategy.ThrowException;
            var msg5 = new MqttApplicationMessage("topic5", new byte[] { 5 });
            Assert.Throws<InvalidOperationException>(() =>
            {
                _ = queuePlugin.EnqueuePublishAsync(msg5);
            });

            client.Dispose();
        }

        [Fact]
        public async Task AutoResubscribePlugin_ResubscribesWhenSessionNotPresent()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            int port = ((IPEndPoint)listener.LocalEndpoint).Port;

            var serverCts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            var subscribeReceivedTcs = new TaskCompletionSource<MqttSubscribePacket>(TaskCreationOptions.RunContinuationsAsynchronously);

            var serverTask = Task.Run(async () =>
            {
                using var socket = await listener.AcceptSocketAsync(serverCts.Token);
                using var stream = new NetworkStream(socket);

                var connectPacket = await ReadPacketFromStreamAsync(stream, serverCts.Token);
                Assert.IsType<MqttConnectPacket>(connectPacket);

                var connAck = new MqttConnAckPacket
                {
                    ReasonCode = MqttConnectReasonCode.Success,
                    SessionPresent = false // 没有已存在的会话，应该触发重订阅
                };
                byte[] connAckBytes = connAck.Encode();
                await stream.WriteAsync(connAckBytes, 0, connAckBytes.Length, serverCts.Token);

                var subPacket = await ReadPacketFromStreamAsync(stream, serverCts.Token);
                if (subPacket is MqttSubscribePacket sub)
                {
                    subscribeReceivedTcs.TrySetResult(sub);

                    var subAck = new MqttSubAckPacket
                    {
                        PacketId = sub.PacketId,
                        ReasonCodes = new List<MqttSubAckReasonCode> { MqttSubAckReasonCode.GrantedQoS1 }
                    };
                    byte[] subAckBytes = subAck.Encode();
                    await stream.WriteAsync(subAckBytes, 0, subAckBytes.Length, serverCts.Token);
                }
            });

            var client = new MqttTcpClient("127.0.0.1", port);
            var resubPlugin = client.EnableAutoResubscribe();
            resubPlugin.TrackSubscription("sensors/temperature/#", MqttQualityOfServiceLevel.AtLeastOnce);

            var connectResult = await client.ConnectMqttAsync();
            Assert.True(connectResult.IsSuccess);

            // 等待服务端收到自动补发的订阅包
            var receivedSub = await Task.WhenAny(subscribeReceivedTcs.Task, Task.Delay(5000));
            Assert.Same(subscribeReceivedTcs.Task, receivedSub);

            var subResult = await subscribeReceivedTcs.Task;
            Assert.Contains(subResult.Subscriptions, s => s.TopicFilter == "sensors/temperature/#");

            await serverTask;
            listener.Stop();
            client.Dispose();
        }
    }
}
