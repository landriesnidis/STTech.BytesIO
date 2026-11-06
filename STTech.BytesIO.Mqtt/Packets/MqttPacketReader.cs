using STTech.BytesIO.Core.Component;
using System;
using System.Buffers;
using System.Runtime.InteropServices;

namespace STTech.BytesIO.Mqtt
{
    /// <summary>
    /// MQTT 报文解析读取工厂。
    /// </summary>
    public static class MqttPacketReader
    {
        /// <summary>
        /// 从 ReadOnlySequence 中解析 MQTT 报文。
        /// 针对单段连续内存执行零拷贝零分配切片，跨段碎片回退聚合。
        /// </summary>
        public static MqttPacket ReadPacket(ReadOnlySequence<byte> sequence, MqttProtocolVersion version = MqttProtocolVersion.V5, UnpackContext context = null)
        {
            if (sequence.IsSingleSegment)
            {
                ReadOnlyMemory<byte> memory = sequence.First;
                if (MemoryMarshal.TryGetArray(memory, out ArraySegment<byte> segment))
                {
                    return ReadPacket(segment.Array, segment.Offset, segment.Count, version, context);
                }
            }

            byte[] buffer = sequence.ToArray();
            return ReadPacket(buffer, 0, buffer.Length, version, context);
        }

        /// <summary>
        /// 从字节数组中解析 MQTT 报文。
        /// </summary>
        public static MqttPacket ReadPacket(byte[] buffer, MqttProtocolVersion version = MqttProtocolVersion.V5, UnpackContext context = null)
        {
            if (buffer == null) throw new ArgumentNullException(nameof(buffer));
            return ReadPacket(buffer, 0, buffer.Length, version, context);
        }

        /// <summary>
        /// 从字节数组片段中解析 MQTT 报文。
        /// </summary>
        public static MqttPacket ReadPacket(byte[] buffer, int offset, int count, MqttProtocolVersion version = MqttProtocolVersion.V5, UnpackContext context = null)
        {
            if (buffer == null) throw new ArgumentNullException(nameof(buffer));
            if (count < 2) throw new MqttProtocolViolationException("数据长度不足以构成 MQTT 报文头部。");

            var reader = new MqttBinaryReader(buffer, offset, count);
            byte firstByte = reader.ReadByte();
            var packetType = (MqttPacketType)(firstByte >> 4);
            byte flags = (byte)(firstByte & 0x0F);

            int remainingLength = reader.ReadVariableByteInteger();
            if (reader.BytesRemaining < remainingLength)
            {
                throw new MqttProtocolViolationException($"报文数据不完整：声明的剩余长度为 {remainingLength}，但实际读取只有 {reader.BytesRemaining} 字节。");
            }

            var payloadReader = reader.Slice(remainingLength);

            MqttPacket packet = CreatePacket(packetType, context);
            packet.Flags = flags;
            packet.ProtocolVersion = version;
            packet.DecodeVariableHeaderAndPayload(payloadReader);
            return packet;
        }

        /// <summary>
        /// 根据控制报文类型创建空报文实例。
        /// </summary>
        public static MqttPacket CreatePacket(MqttPacketType packetType, UnpackContext context = null)
        {
            switch (packetType)
            {
                case MqttPacketType.Connect: return new MqttConnectPacket(context);
                case MqttPacketType.ConnAck: return new MqttConnAckPacket(context);
                case MqttPacketType.Publish: return new MqttPublishPacket(context);
                case MqttPacketType.PubAck: return new MqttPubAckPacket(context);
                case MqttPacketType.PubRec: return new MqttPubRecPacket(context);
                case MqttPacketType.PubRel: return new MqttPubRelPacket(context);
                case MqttPacketType.PubComp: return new MqttPubCompPacket(context);
                case MqttPacketType.Subscribe: return new MqttSubscribePacket(context);
                case MqttPacketType.SubAck: return new MqttSubAckPacket(context);
                case MqttPacketType.Unsubscribe: return new MqttUnsubscribePacket(context);
                case MqttPacketType.UnsubAck: return new MqttUnsubAckPacket(context);
                case MqttPacketType.PingReq: return new MqttPingReqPacket(context);
                case MqttPacketType.PingResp: return new MqttPingRespPacket(context);
                case MqttPacketType.Disconnect: return new MqttDisconnectPacket(context);
                case MqttPacketType.Auth: return new MqttAuthPacket(context);
                default:
                    throw new MqttProtocolViolationException($"未知的 MQTT 报文类型: {(byte)packetType}");
            }
        }
    }
}
