using STTech.BytesIO.Core;
using STTech.BytesIO.Core.Component;
using System;
using System.Buffers;

namespace STTech.BytesIO.Mqtt
{
    /// <summary>
    /// MQTT 协议解包器。
    /// 负责计算分包/粘包边界并将数据帧反序列化为强类型的 MqttPacket。
    /// </summary>
    public class MqttUnpacker : Unpacker<MqttPacket>
    {
        /// <summary>
        /// 协议版本（默认为 V5）。
        /// </summary>
        public MqttProtocolVersion ProtocolVersion { get; set; } = MqttProtocolVersion.V5;

        /// <summary>
        /// 构造 MQTT 协议解包器。
        /// </summary>
        /// <param name="client">底层的通信客户端</param>
        /// <param name="protocolVersion">协议版本</param>
        public MqttUnpacker(BytesClient client = null, MqttProtocolVersion protocolVersion = MqttProtocolVersion.V5) : base(client)
        {
            ProtocolVersion = protocolVersion;
        }

        /// <inheritdoc/>
        protected override int CalculatePacketLength(ReadOnlySequence<byte> buffer)
        {
            // MQTT 固定报头最少需要 2 个字节：首字节（PacketType + Flags）+ 至少 1 字节的剩余长度
            if (buffer.Length < 2)
            {
                return 0;
            }

            int multiplier = 1;
            int remainingLength = 0;
            int lengthBytesCount = 0;

            // 取前 5 个字节解析（1 字节固定报头 + 最多 4 字节剩余长度）
            int peekLength = Math.Min(5, (int)buffer.Length);
            Span<byte> headBytes = stackalloc byte[peekLength];
            buffer.Slice(0, peekLength).CopyTo(headBytes);

            for (int i = 1; i < headBytes.Length; i++)
            {
                byte b = headBytes[i];
                lengthBytesCount++;
                remainingLength += (b & 0x7F) * multiplier;
                multiplier *= 128;

                if ((b & 0x80) == 0)
                {
                    // 报文总长度 = 1(首字节) + 长度字节数 + 剩余长度
                    return 1 + lengthBytesCount + remainingLength;
                }
            }

            if (lengthBytesCount >= 4)
            {
                throw new MqttProtocolViolationException("MQTT 可变字节整数超出规范规定的 4 字节上限。");
            }

            return 0;
        }

        /// <inheritdoc/>
        protected override MqttPacket ResponseSerializeHandler(UnpackContext context)
        {
            return MqttPacketReader.ReadPacket(context.Data, ProtocolVersion, context);
        }
    }
}
