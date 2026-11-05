using STTech.BytesIO.Core;
using STTech.BytesIO.Core.Component;
using System;

namespace STTech.BytesIO.Mqtt
{
    /// <summary>
    /// MQTT 报文抽象基类。
    /// 同时继承 Response 并实现 IRequest，完美兼容 BytesIO 发送与解包体系。
    /// </summary>
    public abstract class MqttPacket : Response, IRequest
    {
        /// <summary>
        /// MQTT 报文类型。
        /// </summary>
        public abstract MqttPacketType PacketType { get; }

        /// <summary>
        /// 报文标志位（固定报头的低 4 位）。
        /// </summary>
        public virtual byte Flags { get; set; }

        /// <summary>
        /// MQTT 协议版本。
        /// </summary>
        public MqttProtocolVersion ProtocolVersion { get; set; } = MqttProtocolVersion.V5;

        /// <summary>
        /// MQTT v5 属性集合。
        /// </summary>
        public MqttProperties Properties { get; set; } = new MqttProperties();

        protected MqttPacket() : base()
        {
        }

        protected MqttPacket(UnpackContext context) : base(context)
        {
        }

        /// <summary>
        /// 获取报文序列化字节数组 (IRequest 实现)。
        /// </summary>
        public byte[] GetBytes()
        {
            return Encode();
        }

        /// <summary>
        /// 序列化完整报文（固定报头 + 剩余长度 + 可变报头与负载）。
        /// </summary>
        public byte[] Encode()
        {
            byte[] variablePart;
            using (var varWriter = new MqttBinaryWriter())
            {
                EncodeVariableHeaderAndPayload(varWriter);
                variablePart = varWriter.ToArray();
            }

            using (var writer = new MqttBinaryWriter(1 + 4 + variablePart.Length))
            {
                byte firstByte = (byte)(((byte)PacketType << 4) | (Flags & 0x0F));
                writer.WriteByte(firstByte);
                writer.WriteVariableByteInteger(variablePart.Length);
                writer.WriteRawBytes(variablePart);
                return writer.ToArray();
            }
        }

        /// <summary>
        /// 序列化可变报头与有效载荷。
        /// </summary>
        protected abstract void EncodeVariableHeaderAndPayload(MqttBinaryWriter writer);

        /// <summary>
        /// 反序列化可变报头与有效载荷。
        /// </summary>
        protected internal abstract void DecodeVariableHeaderAndPayload(MqttBinaryReader reader);
    }
}
