using STTech.BytesIO.Core.Component;

namespace STTech.BytesIO.Mqtt
{
    /// <summary>
    /// MQTT 连接确认报文 (CONNACK)。
    /// </summary>
    public sealed class MqttConnAckPacket : MqttPacket
    {
        public override MqttPacketType PacketType => MqttPacketType.ConnAck;

        /// <summary>
        /// 服务端是否存在既有会话 (Session Present)。
        /// </summary>
        public bool SessionPresent { get; set; }

        /// <summary>
        /// 连接原因码。
        /// </summary>
        public MqttConnectReasonCode ReasonCode { get; set; } = MqttConnectReasonCode.Success;

        public MqttConnAckPacket() : base() { }
        public MqttConnAckPacket(UnpackContext context) : base(context) { }

        protected override void EncodeVariableHeaderAndPayload(MqttBinaryWriter writer)
        {
            byte ackFlags = (byte)(SessionPresent ? 0x01 : 0x00);
            writer.WriteByte(ackFlags);
            writer.WriteByte((byte)ReasonCode);
            Properties.WriteTo(writer);
        }

        protected internal override void DecodeVariableHeaderAndPayload(MqttBinaryReader reader)
        {
            byte ackFlags = reader.ReadByte();
            SessionPresent = (ackFlags & 0x01) != 0;
            ReasonCode = (MqttConnectReasonCode)reader.ReadByte();
            Properties = MqttProperties.ReadFrom(reader);
        }
    }
}
