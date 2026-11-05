using STTech.BytesIO.Core.Component;
using System;

namespace STTech.BytesIO.Mqtt
{
    /// <summary>
    /// MQTT 发布消息报文 (PUBLISH)。
    /// </summary>
    public sealed class MqttPublishPacket : MqttPacket
    {
        public override MqttPacketType PacketType => MqttPacketType.Publish;

        public bool Dup { get; set; }
        public MqttQualityOfServiceLevel QoS { get; set; } = MqttQualityOfServiceLevel.AtMostOnce;
        public bool Retain { get; set; }

        public string Topic { get; set; } = string.Empty;
        public ushort PacketId { get; set; }
        public byte[] Payload { get; set; } = Array.Empty<byte>();

        public override byte Flags
        {
            get
            {
                byte flags = 0;
                if (Dup) flags |= 0x08;
                flags |= (byte)(((byte)QoS & 0x03) << 1);
                if (Retain) flags |= 0x01;
                return flags;
            }
            set
            {
                Dup = (value & 0x08) != 0;
                QoS = (MqttQualityOfServiceLevel)((value >> 1) & 0x03);
                Retain = (value & 0x01) != 0;
            }
        }

        public MqttPublishPacket() : base() { }
        public MqttPublishPacket(UnpackContext context) : base(context) { }

        protected override void EncodeVariableHeaderAndPayload(MqttBinaryWriter writer)
        {
            writer.WriteString(Topic ?? string.Empty);
            if (QoS > MqttQualityOfServiceLevel.AtMostOnce)
            {
                writer.WriteTwoByteInteger(PacketId);
            }
            Properties.WriteTo(writer);
            if (Payload != null && Payload.Length > 0)
            {
                writer.WriteRawBytes(Payload);
            }
        }

        protected internal override void DecodeVariableHeaderAndPayload(MqttBinaryReader reader)
        {
            Topic = reader.ReadString();
            if (QoS > MqttQualityOfServiceLevel.AtMostOnce)
            {
                PacketId = reader.ReadTwoByteInteger();
            }
            Properties = MqttProperties.ReadFrom(reader);
            Payload = reader.ReadRemainingBytes();
        }
    }
}
