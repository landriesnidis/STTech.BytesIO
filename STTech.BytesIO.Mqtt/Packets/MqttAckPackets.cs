using STTech.BytesIO.Core.Component;

namespace STTech.BytesIO.Mqtt
{
    /// <summary>
    /// MQTT 报文标识符与原因码确认报文抽象基类。
    /// </summary>
    public abstract class MqttAckPacketBase : MqttPacket
    {
        /// <summary>
        /// 报文标识符。
        /// </summary>
        public ushort PacketId { get; set; }

        /// <summary>
        /// 原始原因码数值。
        /// </summary>
        public byte RawReasonCode { get; set; }

        protected MqttAckPacketBase() : base() { }
        protected MqttAckPacketBase(UnpackContext context) : base(context) { }

        protected override void EncodeVariableHeaderAndPayload(MqttBinaryWriter writer)
        {
            writer.WriteTwoByteInteger(PacketId);

            // 在 MQTT v5 中，若原因码为 Success(0x00) 且无属性，可以省略原因码和属性以压缩报文
            if (RawReasonCode == 0x00 && Properties.IsEmpty)
            {
                return;
            }

            writer.WriteByte(RawReasonCode);
            Properties.WriteTo(writer);
        }

        protected internal override void DecodeVariableHeaderAndPayload(MqttBinaryReader reader)
        {
            PacketId = reader.ReadTwoByteInteger();

            if (reader.BytesRemaining > 0)
            {
                RawReasonCode = reader.ReadByte();
            }
            else
            {
                RawReasonCode = 0x00;
            }

            if (reader.BytesRemaining > 0)
            {
                Properties = MqttProperties.ReadFrom(reader);
            }
        }
    }

    /// <summary>
    /// QoS 1 发布确认报文 (PUBACK)。
    /// </summary>
    public sealed class MqttPubAckPacket : MqttAckPacketBase
    {
        public override MqttPacketType PacketType => MqttPacketType.PubAck;

        public MqttPubAckReasonCode ReasonCode
        {
            get => (MqttPubAckReasonCode)RawReasonCode;
            set => RawReasonCode = (byte)value;
        }

        public MqttPubAckPacket() : base() { }
        public MqttPubAckPacket(UnpackContext context) : base(context) { }
    }

    /// <summary>
    /// QoS 2 发布收到报文 (PUBREC)。
    /// </summary>
    public sealed class MqttPubRecPacket : MqttAckPacketBase
    {
        public override MqttPacketType PacketType => MqttPacketType.PubRec;

        public MqttPubRecReasonCode ReasonCode
        {
            get => (MqttPubRecReasonCode)RawReasonCode;
            set => RawReasonCode = (byte)value;
        }

        public MqttPubRecPacket() : base() { }
        public MqttPubRecPacket(UnpackContext context) : base(context) { }
    }

    /// <summary>
    /// QoS 2 发布释放报文 (PUBREL)。
    /// </summary>
    public sealed class MqttPubRelPacket : MqttAckPacketBase
    {
        public override MqttPacketType PacketType => MqttPacketType.PubRel;

        public override byte Flags
        {
            get => 0x02; // 按照 MQTT 规范，PUBREL 的标志位固定为 0010 (0x02)
            set => base.Flags = 0x02;
        }

        public MqttPubRelReasonCode ReasonCode
        {
            get => (MqttPubRelReasonCode)RawReasonCode;
            set => RawReasonCode = (byte)value;
        }

        public MqttPubRelPacket() : base()
        {
            Flags = 0x02;
        }

        public MqttPubRelPacket(UnpackContext context) : base(context)
        {
            Flags = 0x02;
        }
    }

    /// <summary>
    /// QoS 2 发布完成报文 (PUBCOMP)。
    /// </summary>
    public sealed class MqttPubCompPacket : MqttAckPacketBase
    {
        public override MqttPacketType PacketType => MqttPacketType.PubComp;

        public MqttPubCompReasonCode ReasonCode
        {
            get => (MqttPubCompReasonCode)RawReasonCode;
            set => RawReasonCode = (byte)value;
        }

        public MqttPubCompPacket() : base() { }
        public MqttPubCompPacket(UnpackContext context) : base(context) { }
    }
}
