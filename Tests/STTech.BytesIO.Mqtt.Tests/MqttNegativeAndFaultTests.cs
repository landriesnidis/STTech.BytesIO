using STTech.BytesIO.Mqtt;
using System;
using System.Buffers;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace STTech.BytesIO.Mqtt.Tests
{
    public class MqttNegativeAndFaultTests
    {
        [Fact]
        public void Test_VariableByteInteger_ExceedsFourBytes_ThrowsProtocolViolationException()
        {
            // 5 字节且最高位均置 1，超出规范 4 字节上限
            byte[] invalidData = new byte[] { 0x80, 0x80, 0x80, 0x80, 0x01 };
            var reader = new MqttBinaryReader(invalidData);
            Assert.Throws<MqttProtocolViolationException>(() => reader.ReadVariableByteInteger());
        }

        [Fact]
        public void Test_Unpacker_CalculatePacketLength_ExceedsFourBytes_ThrowsProtocolViolationException()
        {
            var unpacker = new MqttUnpacker();
            var calcMethod = typeof(MqttUnpacker).GetMethod("CalculatePacketLength", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(calcMethod);

            // 首字节 0x10 (CONNECT)，后续 4 个带有 0x80 的字节，无终止符
            byte[] invalidBuffer = new byte[] { 0x10, 0x80, 0x80, 0x80, 0x80, 0x01 };
            var seq = new ReadOnlySequence<byte>(invalidBuffer);

            var ex = Assert.Throws<TargetInvocationException>(() => calcMethod.Invoke(unpacker, new object[] { seq }));
            Assert.IsType<MqttProtocolViolationException>(ex.InnerException);
        }

        [Fact]
        public void Test_PacketReader_BufferTooShort_ThrowsProtocolViolationException()
        {
            byte[] tooShort = new byte[] { 0x10 };
            Assert.Throws<MqttProtocolViolationException>(() => MqttPacketReader.ReadPacket(tooShort));
        }

        [Fact]
        public void Test_PacketReader_IncompleteRemainingLength_ThrowsProtocolViolationException()
        {
            // 声明剩余长度 50 字节，但后续只有 2 字节
            byte[] truncated = new byte[] { 0x10, 50, 0x00, 0x04 };
            Assert.Throws<MqttProtocolViolationException>(() => MqttPacketReader.ReadPacket(truncated));
        }

        [Fact]
        public async Task Test_Broker_Refuses_Connect_ReturnsFailure_And_CleansUp()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            int port = ((IPEndPoint)listener.LocalEndpoint).Port;

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            var serverTask = Task.Run(async () =>
            {
                using var socket = await listener.AcceptSocketAsync(cts.Token);
                using var stream = new NetworkStream(socket);

                // 读取 CONNECT
                byte[] buf = new byte[256];
                await stream.ReadAsync(buf, 0, buf.Length, cts.Token);

                // 回复拒绝原因码：BadUserNameOrPassword (0x86)
                var connAck = new MqttConnAckPacket
                {
                    ReasonCode = MqttConnectReasonCode.BadUserNameOrPassword
                };
                byte[] connAckBytes = connAck.Encode();
                await stream.WriteAsync(connAckBytes, 0, connAckBytes.Length, cts.Token);
            }, cts.Token);

            try
            {
                using var client = new MqttTcpClient("127.0.0.1", port);
                var result = await client.ConnectMqttAsync(cancellationToken: cts.Token);

                Assert.False(result.IsSuccess);
                Assert.Equal(MqttConnectReasonCode.BadUserNameOrPassword, result.ReasonCode);
                Assert.NotNull(result.Exception);
                Assert.False(client.IsConnected);
            }
            finally
            {
                listener.Stop();
                try { await serverTask; } catch { }
            }
        }

        [Fact]
        public async Task Test_Broker_Refuses_Subscribe_RollsBack_TopicRouter()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            int port = ((IPEndPoint)listener.LocalEndpoint).Port;

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            var serverTask = Task.Run(async () =>
            {
                using var socket = await listener.AcceptSocketAsync(cts.Token);
                using var stream = new NetworkStream(socket);

                // 1. 握手成功
                byte[] buf = new byte[256];
                await stream.ReadAsync(buf, 0, buf.Length, cts.Token);
                var connAck = new MqttConnAckPacket { ReasonCode = MqttConnectReasonCode.Success };
                await stream.WriteAsync(connAck.Encode(), 0, connAck.Encode().Length, cts.Token);

                // 2. 收到 SUBSCRIBE，回复拒绝原因码 TopicFilterInvalid (0x8F)
                int read = await stream.ReadAsync(buf, 0, buf.Length, cts.Token);
                var subPacket = (MqttSubscribePacket)MqttPacketReader.ReadPacket(buf, 0, read);

                var subAck = new MqttSubAckPacket { PacketId = subPacket.PacketId };
                subAck.ReasonCodes.Add(MqttSubAckReasonCode.TopicFilterInvalid);
                await stream.WriteAsync(subAck.Encode(), 0, subAck.Encode().Length, cts.Token);
            }, cts.Token);

            try
            {
                using var client = new MqttTcpClient("127.0.0.1", port);
                var connectResult = await client.ConnectMqttAsync(cancellationToken: cts.Token);
                Assert.True(connectResult.IsSuccess);

                // 订阅该主题
                var subResult = await client.SubscribeAsync("invalid/+/topic", MqttQualityOfServiceLevel.AtLeastOnce, msg => Task.CompletedTask, cancellationToken: cts.Token);

                Assert.False(subResult.IsSuccess);
                Assert.Equal(MqttSubAckReasonCode.TopicFilterInvalid, subResult.ReasonCodes[0]);

                // 验证路由已被自动卸载回滚，不残留无效路由
                Assert.Empty(client.Router.GetSubscribedTopicFilters());
            }
            finally
            {
                listener.Stop();
                try { await serverTask; } catch { }
            }
        }

        [Fact]
        public async Task Test_Broker_Initiates_Disconnect_Triggers_OnMqttDisconnected()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            int port = ((IPEndPoint)listener.LocalEndpoint).Port;

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            var disconnectedTcs = new TaskCompletionSource<MqttDisconnectedEventArgs>(TaskCreationOptions.RunContinuationsAsynchronously);

            var serverTask = Task.Run(async () =>
            {
                using var socket = await listener.AcceptSocketAsync(cts.Token);
                using var stream = new NetworkStream(socket);

                // 1. 正常握手
                byte[] buf = new byte[256];
                await stream.ReadAsync(buf, 0, buf.Length, cts.Token);
                var connAck = new MqttConnAckPacket { ReasonCode = MqttConnectReasonCode.Success };
                await stream.WriteAsync(connAck.Encode(), 0, connAck.Encode().Length, cts.Token);

                await Task.Delay(50, cts.Token);

                // 2. 服务端主动发送 DISCONNECT 踢下线 (AdministrativeAction = 0x98)
                var disconnect = new MqttDisconnectPacket
                {
                    ReasonCode = MqttDisconnectReasonCode.AdministrativeAction
                };
                disconnect.Properties.ReasonString = "Kicked by Administrator";
                byte[] disBytes = disconnect.Encode();
                await stream.WriteAsync(disBytes, 0, disBytes.Length, cts.Token);
            }, cts.Token);

            try
            {
                using var client = new MqttTcpClient("127.0.0.1", port);
                client.OnMqttDisconnected += (sender, e) =>
                {
                    disconnectedTcs.TrySetResult(e);
                };

                var connectResult = await client.ConnectMqttAsync(cancellationToken: cts.Token);
                Assert.True(connectResult.IsSuccess);

                // 等待客户端接收并响应服务端的 DISCONNECT
                var completedTask = await Task.WhenAny(disconnectedTcs.Task, Task.Delay(3000));
                Assert.Equal(disconnectedTcs.Task, completedTask);

                var eventArgs = await disconnectedTcs.Task;
                Assert.Equal(MqttDisconnectReasonCode.AdministrativeAction, eventArgs.ReasonCode);
                Assert.Equal("Kicked by Administrator", eventArgs.ReasonString);
                Assert.False(client.IsConnected);
            }
            finally
            {
                listener.Stop();
                try { await serverTask; } catch { }
            }
        }
    }
}
