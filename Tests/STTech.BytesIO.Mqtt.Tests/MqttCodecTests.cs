using STTech.BytesIO.Mqtt;
using System.Buffers;
using System.Text;
using Xunit;

namespace STTech.BytesIO.Mqtt.Tests
{
    public class MqttCodecTests
    {
        [Theory]
        [InlineData(0, new byte[] { 0x00 })]
        [InlineData(127, new byte[] { 0x7F })]
        [InlineData(128, new byte[] { 0x80, 0x01 })]
        [InlineData(16383, new byte[] { 0xFF, 0x7F })]
        [InlineData(16384, new byte[] { 0x80, 0x80, 0x01 })]
        [InlineData(2097151, new byte[] { 0xFF, 0xFF, 0x7F })]
        [InlineData(2097152, new byte[] { 0x80, 0x80, 0x80, 0x01 })]
        [InlineData(268435455, new byte[] { 0xFF, 0xFF, 0xFF, 0x7F })]
        public void Test_VariableByteInteger_EncodeDecode(int value, byte[] expectedBytes)
        {
            using (var writer = new MqttBinaryWriter())
            {
                writer.WriteVariableByteInteger(value);
                byte[] encoded = writer.ToArray();
                Assert.Equal(expectedBytes, encoded);

                var reader = new MqttBinaryReader(encoded);
                int decoded = reader.ReadVariableByteInteger();
                Assert.Equal(value, decoded);
            }
        }

        [Fact]
        public void Test_Connect_ConnAck_Packet_RoundTrip()
        {
            var connect = new MqttConnectPacket
            {
                ClientId = "TestClient_001",
                CleanStart = true,
                KeepAlive = 60,
                UserName = "admin",
                Password = "secretPassword",
                HasWill = true,
                WillTopic = "status/offline",
                WillPayload = Encoding.UTF8.GetBytes("unexpected death"),
                WillQoS = MqttQualityOfServiceLevel.AtLeastOnce,
                WillRetain = true
            };
            connect.Properties.SessionExpiryInterval = 3600;
            connect.Properties.UserProperties.Add(new MqttUserProperty("env", "test"));
            connect.WillProperties.ContentType = "text/plain";

            byte[] connectBytes = connect.Encode();
            var parsedPacket = MqttPacketReader.ReadPacket(connectBytes);
            var parsedConnect = Assert.IsType<MqttConnectPacket>(parsedPacket);

            Assert.Equal("TestClient_001", parsedConnect.ClientId);
            Assert.True(parsedConnect.CleanStart);
            Assert.Equal(60, parsedConnect.KeepAlive);
            Assert.Equal("admin", parsedConnect.UserName);
            Assert.Equal("secretPassword", parsedConnect.Password);
            Assert.True(parsedConnect.HasWill);
            Assert.Equal("status/offline", parsedConnect.WillTopic);
            Assert.Equal("unexpected death", Encoding.UTF8.GetString(parsedConnect.WillPayload));
            Assert.Equal(MqttQualityOfServiceLevel.AtLeastOnce, parsedConnect.WillQoS);
            Assert.True(parsedConnect.WillRetain);
            Assert.Equal((uint)3600, parsedConnect.Properties.SessionExpiryInterval);
            Assert.Single(parsedConnect.Properties.UserProperties);
            Assert.Equal("env", parsedConnect.Properties.UserProperties[0].Name);
            Assert.Equal("test", parsedConnect.Properties.UserProperties[0].Value);
            Assert.Equal("text/plain", parsedConnect.WillProperties.ContentType);

            // ConnAck
            var connAck = new MqttConnAckPacket
            {
                SessionPresent = true,
                ReasonCode = MqttConnectReasonCode.Success
            };
            connAck.Properties.AssignedClientIdentifier = "ServerAssignedId";
            connAck.Properties.ServerKeepAlive = 120;

            byte[] connAckBytes = connAck.Encode();
            var parsedConnAck = Assert.IsType<MqttConnAckPacket>(MqttPacketReader.ReadPacket(connAckBytes));
            Assert.True(parsedConnAck.SessionPresent);
            Assert.Equal(MqttConnectReasonCode.Success, parsedConnAck.ReasonCode);
            Assert.Equal("ServerAssignedId", parsedConnAck.Properties.AssignedClientIdentifier);
            Assert.Equal((ushort)120, parsedConnAck.Properties.ServerKeepAlive);
        }

        [Fact]
        public void Test_Publish_Packets_RoundTrip()
        {
            var publish = new MqttPublishPacket
            {
                Topic = "device/sensor/temperature",
                QoS = MqttQualityOfServiceLevel.ExactlyOnce,
                PacketId = 12345,
                Dup = false,
                Retain = true,
                Payload = Encoding.UTF8.GetBytes("{\"temp\": 24.5}")
            };
            publish.Properties.ContentType = "application/json";
            publish.Properties.UserProperties.Add(new MqttUserProperty("trace-id", "xyz-123"));

            byte[] pubBytes = publish.Encode();
            var parsedPub = Assert.IsType<MqttPublishPacket>(MqttPacketReader.ReadPacket(pubBytes));

            Assert.Equal("device/sensor/temperature", parsedPub.Topic);
            Assert.Equal(MqttQualityOfServiceLevel.ExactlyOnce, parsedPub.QoS);
            Assert.Equal((ushort)12345, parsedPub.PacketId);
            Assert.False(parsedPub.Dup);
            Assert.True(parsedPub.Retain);
            Assert.Equal("{\"temp\": 24.5}", Encoding.UTF8.GetString(parsedPub.Payload));
            Assert.Equal("application/json", parsedPub.Properties.ContentType);
            Assert.Single(parsedPub.Properties.UserProperties);
            Assert.Equal("trace-id", parsedPub.Properties.UserProperties[0].Name);

            // PubAck
            var pubAck = new MqttPubAckPacket { PacketId = 12345, ReasonCode = MqttPubAckReasonCode.Success };
            var parsedPubAck = Assert.IsType<MqttPubAckPacket>(MqttPacketReader.ReadPacket(pubAck.Encode()));
            Assert.Equal((ushort)12345, parsedPubAck.PacketId);
            Assert.Equal(MqttPubAckReasonCode.Success, parsedPubAck.ReasonCode);

            // PubRec
            var pubRec = new MqttPubRecPacket { PacketId = 12345, ReasonCode = MqttPubRecReasonCode.Success };
            var parsedPubRec = Assert.IsType<MqttPubRecPacket>(MqttPacketReader.ReadPacket(pubRec.Encode()));
            Assert.Equal((ushort)12345, parsedPubRec.PacketId);
            Assert.Equal(MqttPubRecReasonCode.Success, parsedPubRec.ReasonCode);

            // PubRel
            var pubRel = new MqttPubRelPacket { PacketId = 12345, ReasonCode = MqttPubRelReasonCode.Success };
            var parsedPubRel = Assert.IsType<MqttPubRelPacket>(MqttPacketReader.ReadPacket(pubRel.Encode()));
            Assert.Equal((ushort)12345, parsedPubRel.PacketId);
            Assert.Equal(0x02, parsedPubRel.Flags);
            Assert.Equal(MqttPubRelReasonCode.Success, parsedPubRel.ReasonCode);

            // PubComp
            var pubComp = new MqttPubCompPacket { PacketId = 12345, ReasonCode = MqttPubCompReasonCode.Success };
            var parsedPubComp = Assert.IsType<MqttPubCompPacket>(MqttPacketReader.ReadPacket(pubComp.Encode()));
            Assert.Equal((ushort)12345, parsedPubComp.PacketId);
            Assert.Equal(MqttPubCompReasonCode.Success, parsedPubComp.ReasonCode);
        }

        [Fact]
        public void Test_Subscribe_SubAck_Unsubscribe_UnsubAck_RoundTrip()
        {
            var sub = new MqttSubscribePacket
            {
                PacketId = 8888,
                Subscriptions =
                {
                    new MqttTopicSubscription("sensors/+/data", MqttQualityOfServiceLevel.AtLeastOnce) { RetainHandling = 1 },
                    new MqttTopicSubscription("alerts/#", MqttQualityOfServiceLevel.ExactlyOnce) { NoLocal = true }
                }
            };
            byte[] subBytes = sub.Encode();
            var parsedSub = Assert.IsType<MqttSubscribePacket>(MqttPacketReader.ReadPacket(subBytes));

            Assert.Equal((ushort)8888, parsedSub.PacketId);
            Assert.Equal(0x02, parsedSub.Flags);
            Assert.Equal(2, parsedSub.Subscriptions.Count);
            Assert.Equal("sensors/+/data", parsedSub.Subscriptions[0].TopicFilter);
            Assert.Equal(MqttQualityOfServiceLevel.AtLeastOnce, parsedSub.Subscriptions[0].QualityOfServiceLevel);
            Assert.Equal(1, parsedSub.Subscriptions[0].RetainHandling);
            Assert.Equal("alerts/#", parsedSub.Subscriptions[1].TopicFilter);
            Assert.Equal(MqttQualityOfServiceLevel.ExactlyOnce, parsedSub.Subscriptions[1].QualityOfServiceLevel);
            Assert.True(parsedSub.Subscriptions[1].NoLocal);

            // SubAck
            var subAck = new MqttSubAckPacket
            {
                PacketId = 8888,
                ReasonCodes = { MqttSubAckReasonCode.GrantedQoS1, MqttSubAckReasonCode.GrantedQoS2 }
            };
            var parsedSubAck = Assert.IsType<MqttSubAckPacket>(MqttPacketReader.ReadPacket(subAck.Encode()));
            Assert.Equal((ushort)8888, parsedSubAck.PacketId);
            Assert.Equal(2, parsedSubAck.ReasonCodes.Count);
            Assert.Equal(MqttSubAckReasonCode.GrantedQoS1, parsedSubAck.ReasonCodes[0]);
            Assert.Equal(MqttSubAckReasonCode.GrantedQoS2, parsedSubAck.ReasonCodes[1]);

            // Unsubscribe
            var unsub = new MqttUnsubscribePacket
            {
                PacketId = 9999,
                TopicFilters = { "sensors/+/data", "alerts/#" }
            };
            var parsedUnsub = Assert.IsType<MqttUnsubscribePacket>(MqttPacketReader.ReadPacket(unsub.Encode()));
            Assert.Equal((ushort)9999, parsedUnsub.PacketId);
            Assert.Equal(0x02, parsedUnsub.Flags);
            Assert.Equal(2, parsedUnsub.TopicFilters.Count);
            Assert.Equal("sensors/+/data", parsedUnsub.TopicFilters[0]);

            // UnsubAck
            var unsubAck = new MqttUnsubAckPacket
            {
                PacketId = 9999,
                ReasonCodes = { MqttUnsubAckReasonCode.Success, MqttUnsubAckReasonCode.NoSubscriptionExisted }
            };
            var parsedUnsubAck = Assert.IsType<MqttUnsubAckPacket>(MqttPacketReader.ReadPacket(unsubAck.Encode()));
            Assert.Equal((ushort)9999, parsedUnsubAck.PacketId);
            Assert.Equal(2, parsedUnsubAck.ReasonCodes.Count);
            Assert.Equal(MqttUnsubAckReasonCode.Success, parsedUnsubAck.ReasonCodes[0]);
            Assert.Equal(MqttUnsubAckReasonCode.NoSubscriptionExisted, parsedUnsubAck.ReasonCodes[1]);
        }

        [Fact]
        public void Test_Ping_Disconnect_Auth_RoundTrip()
        {
            // PingReq
            var pingReq = new MqttPingReqPacket();
            var parsedPingReq = Assert.IsType<MqttPingReqPacket>(MqttPacketReader.ReadPacket(pingReq.Encode()));
            Assert.Equal(MqttPacketType.PingReq, parsedPingReq.PacketType);

            // PingResp
            var pingResp = new MqttPingRespPacket();
            var parsedPingResp = Assert.IsType<MqttPingRespPacket>(MqttPacketReader.ReadPacket(pingResp.Encode()));
            Assert.Equal(MqttPacketType.PingResp, parsedPingResp.PacketType);

            // Disconnect
            var disconnect = new MqttDisconnectPacket
            {
                ReasonCode = MqttDisconnectReasonCode.AdministrativeAction
            };
            disconnect.Properties.ReasonString = "Server maintenance";
            var parsedDisconnect = Assert.IsType<MqttDisconnectPacket>(MqttPacketReader.ReadPacket(disconnect.Encode()));
            Assert.Equal(MqttDisconnectReasonCode.AdministrativeAction, parsedDisconnect.ReasonCode);
            Assert.Equal("Server maintenance", parsedDisconnect.Properties.ReasonString);

            // Auth
            var auth = new MqttAuthPacket { ReasonCode = 0x18 };
            auth.Properties.AuthenticationMethod = "SCRAM-SHA-256";
            var parsedAuth = Assert.IsType<MqttAuthPacket>(MqttPacketReader.ReadPacket(auth.Encode()));
            Assert.Equal((byte)0x18, parsedAuth.ReasonCode);
            Assert.Equal("SCRAM-SHA-256", parsedAuth.Properties.AuthenticationMethod);
        }

        [Fact]
        public void Test_MqttUnpacker_CalculatePacketLength()
        {
            var unpacker = new MqttUnpacker();
            var testUnpackerType = typeof(MqttUnpacker);
            var calcMethod = testUnpackerType.GetMethod("CalculatePacketLength", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Assert.NotNull(calcMethod);

            // Partial packet: only 1 byte
            var seq1 = new ReadOnlySequence<byte>(new byte[] { 0x10 });
            int len1 = (int)(calcMethod.Invoke(unpacker, new object[] { seq1 }) ?? 0);
            Assert.Equal(0, len1);

            // Valid Connect Packet
            var connect = new MqttConnectPacket { ClientId = "Device1" };
            byte[] connectBytes = connect.Encode();

            var fullSeq = new ReadOnlySequence<byte>(connectBytes);
            int fullLen = (int)(calcMethod.Invoke(unpacker, new object[] { fullSeq }) ?? 0);
            Assert.Equal(connectBytes.Length, fullLen);

            // Concatenated packets (Packet 1 + Packet 2)
            var ping = new MqttPingReqPacket();
            byte[] pingBytes = ping.Encode();

            byte[] multiPackets = new byte[connectBytes.Length + pingBytes.Length];
            connectBytes.CopyTo(multiPackets, 0);
            pingBytes.CopyTo(multiPackets, connectBytes.Length);

            var multiSeq = new ReadOnlySequence<byte>(multiPackets);
            int firstPacketLen = (int)(calcMethod.Invoke(unpacker, new object[] { multiSeq }) ?? 0);
            Assert.Equal(connectBytes.Length, firstPacketLen);
        }
    }
}
