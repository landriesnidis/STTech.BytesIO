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
    public class MqttManagedClientTests
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
        public async Task MqttManagedTcpClient_OutOfTheBox_OfflineEnqueue_AutoTrackSubscription()
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

                    if (packet is MqttPublishPacket pub && pub.QoS == MqttQualityOfServiceLevel.AtLeastOnce)
                    {
                        var pubAck = new MqttPubAckPacket { PacketId = pub.PacketId, ReasonCode = MqttPubAckReasonCode.Success };
                        byte[] b = pubAck.Encode();
                        await stream.WriteAsync(b, 0, b.Length, serverCts.Token);
                    }
                    else if (packet is MqttSubscribePacket sub)
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
                }
            });

            // 开箱即用构造 MqttManagedTcpClient
            var client = new MqttManagedTcpClient("127.0.0.1", port);
            Assert.NotNull(client.PublishQueue);
            Assert.NotNull(client.AutoResubscribe);
            Assert.NotNull(client.Reconnect);

            // 1. 订阅主题（自动被 AutoResubscribe 追踪）
            _ = client.SubscribeAsync("devices/telemetry/#", MqttQualityOfServiceLevel.AtLeastOnce);
            Assert.Equal(1, client.AutoResubscribe.ManagedSubscriptionCount);

            // 2. 离线入队
            var msg = new MqttApplicationMessage("devices/telemetry/1", "offline data", Encoding.UTF8, MqttQualityOfServiceLevel.AtLeastOnce);
            var publishTask = client.EnqueuePublishAsync(msg);
            Assert.Equal(1, client.QueuedCount);

            // 3. 连接
            var connResult = await client.ConnectMqttAsync();
            Assert.True(connResult.IsSuccess);

            // 4. 验证发布队列已自动排空
            var pubResult = await publishTask;
            Assert.True(pubResult.IsSuccess);
            Assert.Equal(0, client.QueuedCount);

            // 5. 验证自动重订阅已生效
            var subPacket = await Task.WhenAny(subscribeReceivedTcs.Task, Task.Delay(5000));
            Assert.Same(subscribeReceivedTcs.Task, subPacket);

            // 6. 取消订阅验证已移除追踪
            _ = client.UnsubscribeAsync("devices/telemetry/#");
            Assert.Equal(0, client.AutoResubscribe.ManagedSubscriptionCount);

            listener.Stop();
            client.Dispose();
        }
    }
}
