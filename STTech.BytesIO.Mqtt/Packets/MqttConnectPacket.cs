using STTech.BytesIO.Core.Component;
using System;

namespace STTech.BytesIO.Mqtt
{
    /// <summary>
    /// MQTT 连接请求报文 (CONNECT)。
    /// </summary>
    public sealed class MqttConnectPacket : MqttPacket
    {
        public override MqttPacketType PacketType => MqttPacketType.Connect;

        public string ClientId { get; set; } = string.Empty;
        public string UserName { get; set; }
        public string Password { get; set; }
        public ushort KeepAlive { get; set; } = 60;
        public bool CleanStart { get; set; } = true;

        // 遗嘱消息
        public bool HasWill { get; set; }
        public string WillTopic { get; set; }
        public byte[] WillPayload { get; set; }
        public MqttQualityOfServiceLevel WillQoS { get; set; } = MqttQualityOfServiceLevel.AtMostOnce;
        public bool WillRetain { get; set; }
        public MqttProperties WillProperties { get; set; } = new MqttProperties();

        public MqttConnectPacket() : base() { }
        public MqttConnectPacket(UnpackContext context) : base(context) { }

        protected override void EncodeVariableHeaderAndPayload(MqttBinaryWriter writer)
        {
            // 协议名
            writer.WriteString("MQTT");
            // 协议版本 (5)
            writer.WriteByte((byte)ProtocolVersion);

            // 连接标志位
            byte connectFlags = 0;
            if (!string.IsNullOrEmpty(UserName)) connectFlags |= 0x80;
            if (!string.IsNullOrEmpty(Password)) connectFlags |= 0x40;
            if (HasWill)
            {
                connectFlags |= 0x04;
                if (WillRetain) connectFlags |= 0x20;
                connectFlags |= (byte)(((byte)WillQoS & 0x03) << 3);
            }
            if (CleanStart) connectFlags |= 0x02;

            writer.WriteByte(connectFlags);
            writer.WriteTwoByteInteger(KeepAlive);

            // CONNECT 属性
            Properties.WriteTo(writer);

            // Payload: Client Identifier
            writer.WriteString(ClientId ?? string.Empty);

            // Payload: Will
            if (HasWill)
            {
                WillProperties.WriteTo(writer);
                writer.WriteString(WillTopic ?? string.Empty);
                writer.WriteBinaryData(WillPayload ?? Array.Empty<byte>());
            }

            // Payload: User Name
            if (!string.IsNullOrEmpty(UserName))
            {
                writer.WriteString(UserName);
            }

            // Payload: Password
            if (!string.IsNullOrEmpty(Password))
            {
                writer.WriteString(Password);
            }
        }

        protected internal override void DecodeVariableHeaderAndPayload(MqttBinaryReader reader)
        {
            string protocolName = reader.ReadString();
            byte version = reader.ReadByte();
            ProtocolVersion = (MqttProtocolVersion)version;

            byte connectFlags = reader.ReadByte();
            bool hasUserName = (connectFlags & 0x80) != 0;
            bool hasPassword = (connectFlags & 0x40) != 0;
            WillRetain = (connectFlags & 0x20) != 0;
            WillQoS = (MqttQualityOfServiceLevel)((connectFlags >> 3) & 0x03);
            HasWill = (connectFlags & 0x04) != 0;
            CleanStart = (connectFlags & 0x02) != 0;

            KeepAlive = reader.ReadTwoByteInteger();
            Properties = MqttProperties.ReadFrom(reader);

            ClientId = reader.ReadString();

            if (HasWill)
            {
                WillProperties = MqttProperties.ReadFrom(reader);
                WillTopic = reader.ReadString();
                WillPayload = reader.ReadBinaryData();
            }

            if (hasUserName)
            {
                UserName = reader.ReadString();
            }

            if (hasPassword)
            {
                Password = reader.ReadString();
            }
        }
    }
}
