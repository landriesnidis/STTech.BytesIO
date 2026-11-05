using STTech.BytesIO.Mqtt;
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace STTech.BytesIO.Mqtt.Tests
{
    public class MqttClientEndToEndTests
    {
        [Fact]
        public async Task EndToEnd_MqttClient_Connect_Publish_Subscribe_Ping_Disconnect()
        {
            // 启动轻量级 TCP 模拟服务端
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            int port = ((IPEndPoint)listener.LocalEndpoint).Port;

            var serverCts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            var serverTask = Task.Run(async () =>
            {
                using var socket = await listener.AcceptSocketAsync(serverCts.Token);
                using var stream = new NetworkStream(socket);

                // 1. 读取 CONNECT 报文并回应 CONNACK
                var connectPacket = await ReadPacketFromStreamAsync(stream, serverCts.Token);
                Assert.IsType<MqttConnectPacket>(connectPacket);

                var connAck = new MqttConnAckPacket
                {
                    ReasonCode = MqttConnectReasonCode.Success,
                    SessionPresent = false
                };
                byte[] connAckBytes = connAck.Encode();
                await stream.WriteAsync(connAckBytes, 0, connAckBytes.Length, serverCts.Token);

                // 2. 持续循环处理后续客户端报文
                while (!serverCts.IsCancellationRequested)
                {
                    MqttPacket packet;
                    try
                    {
                        packet = await ReadPacketFromStreamAsync(stream, serverCts.Token);
                    }
                    catch
                    {
                        break;
                    }

                    if (packet is MqttPublishPacket pub)
                    {
                        if (pub.QoS == MqttQualityOfServiceLevel.AtLeastOnce)
                        {
                            var pubAck = new MqttPubAckPacket { PacketId = pub.PacketId, ReasonCode = MqttPubAckReasonCode.Success };
                            byte[] bytes = pubAck.Encode();
                            await stream.WriteAsync(bytes, 0, bytes.Length, serverCts.Token);
                        }
                        else if (pub.QoS == MqttQualityOfServiceLevel.ExactlyOnce)
                        {
                            var pubRec = new MqttPubRecPacket { PacketId = pub.PacketId, ReasonCode = MqttPubRecReasonCode.Success };
                            byte[] bytes = pubRec.Encode();
                            await stream.WriteAsync(bytes, 0, bytes.Length, serverCts.Token);
                        }
                    }
                    else if (packet is MqttPubRelPacket pubRel)
                    {
                        var pubComp = new MqttPubCompPacket { PacketId = pubRel.PacketId, ReasonCode = MqttPubCompReasonCode.Success };
                        byte[] bytes = pubComp.Encode();
                        await stream.WriteAsync(bytes, 0, bytes.Length, serverCts.Token);
                    }
                    else if (packet is MqttSubscribePacket sub)
                    {
                        var subAck = new MqttSubAckPacket { PacketId = sub.PacketId };
                        foreach (var s in sub.Subscriptions)
                        {
                            subAck.ReasonCodes.Add(MqttSubAckReasonCode.GrantedQoS1);
                        }
                        byte[] bytes = subAck.Encode();
                        await stream.WriteAsync(bytes, 0, bytes.Length, serverCts.Token);

                        // 收到订阅后，服务端主动向客户端下发一条消息
                        var serverPush = new MqttPublishPacket
                        {
                            Topic = "sensors/temp/room1",
                            QoS = MqttQualityOfServiceLevel.AtLeastOnce,
                            PacketId = 5001,
                            Payload = Encoding.UTF8.GetBytes("26.8C")
                        };
                        byte[] pushBytes = serverPush.Encode();
                        await stream.WriteAsync(pushBytes, 0, pushBytes.Length, serverCts.Token);
                    }
                    else if (packet is MqttPingReqPacket)
                    {
                        var pingResp = new MqttPingRespPacket();
                        byte[] bytes = pingResp.Encode();
                        await stream.WriteAsync(bytes, 0, bytes.Length, serverCts.Token);
                    }
                    else if (packet is MqttDisconnectPacket)
                    {
                        break;
                    }
                }
            }, serverCts.Token);

            try
            {
                // 创建 MqttTcpClient 客户端
                using var client = new MqttTcpClient("127.0.0.1", port);
                client.Options.KeepAlive = 60;
                client.Options.ClientId = "IntegrationTestClient";

                // 1. ConnectMqttAsync
                var connectResult = await client.ConnectMqttAsync();
                Assert.True(connectResult.IsSuccess);
                Assert.True(client.IsConnected);

                // 2. Publish QoS 0
                var pub0Result = await client.PublishAsync("test/qos0", "Hello QoS 0");
                Assert.True(pub0Result.IsSuccess);

                // 3. Publish QoS 1
                var pub1Result = await client.PublishAsync("test/qos1", "Hello QoS 1", qos: MqttQualityOfServiceLevel.AtLeastOnce);
                Assert.True(pub1Result.IsSuccess);

                // 4. Publish QoS 2
                var pub2Result = await client.PublishAsync("test/qos2", "Hello QoS 2", qos: MqttQualityOfServiceLevel.ExactlyOnce);
                Assert.True(pub2Result.IsSuccess);

                // 5. Subscribe & Receive Inbound Message
                var messageReceivedTcs = new TaskCompletionSource<MqttApplicationMessage>(TaskCreationOptions.RunContinuationsAsynchronously);

                var subResult = await client.SubscribeAsync("sensors/#", MqttQualityOfServiceLevel.AtLeastOnce, msg =>
                {
                    messageReceivedTcs.TrySetResult(msg);
                    return Task.CompletedTask;
                });
                Assert.True(subResult.IsSuccess);

                // 等待接收服务端推送的消息
                var receivedMsg = await Task.WhenAny(messageReceivedTcs.Task, Task.Delay(3000));
                Assert.Equal(messageReceivedTcs.Task, receivedMsg);
                var actualMsg = await messageReceivedTcs.Task;
                Assert.Equal("sensors/temp/room1", actualMsg.Topic);
                Assert.Equal("26.8C", actualMsg.ConvertPayloadToString());

                // 6. PingAsync
                await client.PingAsync();

                // 7. DisconnectMqttAsync
                await client.DisconnectMqttAsync();
                Assert.False(client.IsConnected);
            }
            finally
            {
                serverCts.Cancel();
                listener.Stop();
                try { await serverTask; } catch { }
            }
        }

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
            // 写入可变字节整数
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
    }
}
