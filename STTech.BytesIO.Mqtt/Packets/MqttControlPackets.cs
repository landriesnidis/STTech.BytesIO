using STTech.BytesIO.Core.Component;

namespace STTech.BytesIO.Mqtt
{
    /// <summary>
    /// 心跳请求报文 (PINGREQ)。
    /// </summary>
    public sealed class MqttPingReqPacket : MqttPacket
    {
        public override MqttPacketType PacketType => MqttPacketType.PingReq;

        public MqttPingReqPacket() : base() { }
        public MqttPingReqPacket(UnpackContext context) : base(context) { }

        protected override void EncodeVariableHeaderAndPayload(MqttBinaryWriter writer)
        {
            // PINGREQ 没有任何可变报头与负载
        }

        protected internal override void DecodeVariableHeaderAndPayload(MqttBinaryReader reader)
        {
        }
    }

    /// <summary>
    /// 心跳响应报文 (PINGRESP)。
    /// </summary>
    public sealed class MqttPingRespPacket : MqttPacket
    {
        public override MqttPacketType PacketType => MqttPacketType.PingResp;

        public MqttPingRespPacket() : base() { }
        public MqttPingRespPacket(UnpackContext context) : base(context) { }

        protected override void EncodeVariableHeaderAndPayload(MqttBinaryWriter writer)
        {
            // PINGRESP 没有任何可变报头与负载
        }

        protected internal override void DecodeVariableHeaderAndPayload(MqttBinaryReader reader)
        {
        }
    }

    /// <summary>
    /// 断开连接报文 (DISCONNECT)。
    /// </summary>
    public sealed class MqttDisconnectPacket : MqttPacket
    {
        public override MqttPacketType PacketType => MqttPacketType.Disconnect;

        /// <summary>
        /// 断开原因码。
        /// </summary>
        public MqttDisconnectReasonCode ReasonCode { get; set; } = MqttDisconnectReasonCode.NormalDisconnection;

        public MqttDisconnectPacket() : base() { }
        public MqttDisconnectPacket(UnpackContext context) : base(context) { }

        protected override void EncodeVariableHeaderAndPayload(MqttBinaryWriter writer)
        {
            if (ReasonCode == MqttDisconnectReasonCode.NormalDisconnection && Properties.IsEmpty)
            {
                return;
            }

            writer.WriteByte((byte)ReasonCode);
            Properties.WriteTo(writer);
        }

        protected internal override void DecodeVariableHeaderAndPayload(MqttBinaryReader reader)
        {
            if (reader.BytesRemaining > 0)
            {
                ReasonCode = (MqttDisconnectReasonCode)reader.ReadByte();
            }
            if (reader.BytesRemaining > 0)
            {
                Properties = MqttProperties.ReadFrom(reader);
            }
        }
    }

    /// <summary>
    /// 认证交互报文 (AUTH)。
    /// </summary>
    public sealed class MqttAuthPacket : MqttPacket
    {
        public override MqttPacketType PacketType => MqttPacketType.Auth;

        public byte ReasonCode { get; set; } = 0x00;

        public MqttAuthPacket() : base() { }
        public MqttAuthPacket(UnpackContext context) : base(context) { }

        protected override void EncodeVariableHeaderAndPayload(MqttBinaryWriter writer)
        {
            if (ReasonCode == 0x00 && Properties.IsEmpty)
            {
                return;
            }

            writer.WriteByte(ReasonCode);
            Properties.WriteTo(writer);
        }

        protected internal override void DecodeVariableHeaderAndPayload(MqttBinaryReader reader)
        {
            if (reader.BytesRemaining > 0)
            {
                ReasonCode = reader.ReadByte();
            }
            if (reader.BytesRemaining > 0)
            {
                Properties = MqttProperties.ReadFrom(reader);
            }
        }
    }
}
