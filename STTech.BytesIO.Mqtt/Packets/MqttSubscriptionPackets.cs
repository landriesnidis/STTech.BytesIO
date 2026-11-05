using STTech.BytesIO.Core.Component;
using System;
using System.Collections.Generic;

namespace STTech.BytesIO.Mqtt
{
    /// <summary>
    /// 主题订阅项及其选项。
    /// </summary>
    public sealed class MqttTopicSubscription
    {
        public string TopicFilter { get; set; }
        public MqttQualityOfServiceLevel QualityOfServiceLevel { get; set; } = MqttQualityOfServiceLevel.AtMostOnce;
        public bool NoLocal { get; set; }
        public bool RetainAsPublished { get; set; }
        public byte RetainHandling { get; set; }

        public MqttTopicSubscription() { }

        public MqttTopicSubscription(string topicFilter, MqttQualityOfServiceLevel qos = MqttQualityOfServiceLevel.AtMostOnce)
        {
            TopicFilter = topicFilter;
            QualityOfServiceLevel = qos;
        }

        public byte EncodeOptions()
        {
            byte opt = (byte)((byte)QualityOfServiceLevel & 0x03);
            if (NoLocal) opt |= 0x04;
            if (RetainAsPublished) opt |= 0x08;
            opt |= (byte)((RetainHandling & 0x03) << 4);
            return opt;
        }

        public static MqttTopicSubscription DecodeOptions(string topicFilter, byte options)
        {
            return new MqttTopicSubscription
            {
                TopicFilter = topicFilter,
                QualityOfServiceLevel = (MqttQualityOfServiceLevel)(options & 0x03),
                NoLocal = (options & 0x04) != 0,
                RetainAsPublished = (options & 0x08) != 0,
                RetainHandling = (byte)((options >> 4) & 0x03)
            };
        }
    }

    /// <summary>
    /// MQTT 订阅主题请求报文 (SUBSCRIBE)。
    /// </summary>
    public sealed class MqttSubscribePacket : MqttPacket
    {
        public override MqttPacketType PacketType => MqttPacketType.Subscribe;

        public override byte Flags
        {
            get => 0x02; // 按照规范，SUBSCRIBE 标志位固定为 0010 (0x02)
            set => base.Flags = 0x02;
        }

        public ushort PacketId { get; set; }
        public List<MqttTopicSubscription> Subscriptions { get; set; } = new List<MqttTopicSubscription>();

        public MqttSubscribePacket() : base()
        {
            Flags = 0x02;
        }

        public MqttSubscribePacket(UnpackContext context) : base(context)
        {
            Flags = 0x02;
        }

        protected override void EncodeVariableHeaderAndPayload(MqttBinaryWriter writer)
        {
            writer.WriteTwoByteInteger(PacketId);
            Properties.WriteTo(writer);

            if (Subscriptions != null)
            {
                foreach (var sub in Subscriptions)
                {
                    writer.WriteString(sub.TopicFilter ?? string.Empty);
                    writer.WriteByte(sub.EncodeOptions());
                }
            }
        }

        protected internal override void DecodeVariableHeaderAndPayload(MqttBinaryReader reader)
        {
            PacketId = reader.ReadTwoByteInteger();
            Properties = MqttProperties.ReadFrom(reader);

            Subscriptions = new List<MqttTopicSubscription>();
            while (reader.BytesRemaining > 0)
            {
                string topic = reader.ReadString();
                byte opt = reader.ReadByte();
                Subscriptions.Add(MqttTopicSubscription.DecodeOptions(topic, opt));
            }
        }
    }

    /// <summary>
    /// MQTT 订阅确认报文 (SUBACK)。
    /// </summary>
    public sealed class MqttSubAckPacket : MqttPacket
    {
        public override MqttPacketType PacketType => MqttPacketType.SubAck;

        public ushort PacketId { get; set; }
        public List<MqttSubAckReasonCode> ReasonCodes { get; set; } = new List<MqttSubAckReasonCode>();

        public MqttSubAckPacket() : base() { }
        public MqttSubAckPacket(UnpackContext context) : base(context) { }

        protected override void EncodeVariableHeaderAndPayload(MqttBinaryWriter writer)
        {
            writer.WriteTwoByteInteger(PacketId);
            Properties.WriteTo(writer);

            if (ReasonCodes != null)
            {
                foreach (var code in ReasonCodes)
                {
                    writer.WriteByte((byte)code);
                }
            }
        }

        protected internal override void DecodeVariableHeaderAndPayload(MqttBinaryReader reader)
        {
            PacketId = reader.ReadTwoByteInteger();
            Properties = MqttProperties.ReadFrom(reader);

            ReasonCodes = new List<MqttSubAckReasonCode>();
            while (reader.BytesRemaining > 0)
            {
                ReasonCodes.Add((MqttSubAckReasonCode)reader.ReadByte());
            }
        }
    }

    /// <summary>
    /// MQTT 取消订阅请求报文 (UNSUBSCRIBE)。
    /// </summary>
    public sealed class MqttUnsubscribePacket : MqttPacket
    {
        public override MqttPacketType PacketType => MqttPacketType.Unsubscribe;

        public override byte Flags
        {
            get => 0x02; // 按照规范，UNSUBSCRIBE 标志位固定为 0010 (0x02)
            set => base.Flags = 0x02;
        }

        public ushort PacketId { get; set; }
        public List<string> TopicFilters { get; set; } = new List<string>();

        public MqttUnsubscribePacket() : base()
        {
            Flags = 0x02;
        }

        public MqttUnsubscribePacket(UnpackContext context) : base(context)
        {
            Flags = 0x02;
        }

        protected override void EncodeVariableHeaderAndPayload(MqttBinaryWriter writer)
        {
            writer.WriteTwoByteInteger(PacketId);
            Properties.WriteTo(writer);

            if (TopicFilters != null)
            {
                foreach (var filter in TopicFilters)
                {
                    writer.WriteString(filter ?? string.Empty);
                }
            }
        }

        protected internal override void DecodeVariableHeaderAndPayload(MqttBinaryReader reader)
        {
            PacketId = reader.ReadTwoByteInteger();
            Properties = MqttProperties.ReadFrom(reader);

            TopicFilters = new List<string>();
            while (reader.BytesRemaining > 0)
            {
                TopicFilters.Add(reader.ReadString());
            }
        }
    }

    /// <summary>
    /// MQTT 取消订阅确认报文 (UNSUBACK)。
    /// </summary>
    public sealed class MqttUnsubAckPacket : MqttPacket
    {
        public override MqttPacketType PacketType => MqttPacketType.UnsubAck;

        public ushort PacketId { get; set; }
        public List<MqttUnsubAckReasonCode> ReasonCodes { get; set; } = new List<MqttUnsubAckReasonCode>();

        public MqttUnsubAckPacket() : base() { }
        public MqttUnsubAckPacket(UnpackContext context) : base(context) { }

        protected override void EncodeVariableHeaderAndPayload(MqttBinaryWriter writer)
        {
            writer.WriteTwoByteInteger(PacketId);
            Properties.WriteTo(writer);

            if (ReasonCodes != null)
            {
                foreach (var code in ReasonCodes)
                {
                    writer.WriteByte((byte)code);
                }
            }
        }

        protected internal override void DecodeVariableHeaderAndPayload(MqttBinaryReader reader)
        {
            PacketId = reader.ReadTwoByteInteger();
            Properties = MqttProperties.ReadFrom(reader);

            ReasonCodes = new List<MqttUnsubAckReasonCode>();
            while (reader.BytesRemaining > 0)
            {
                ReasonCodes.Add((MqttUnsubAckReasonCode)reader.ReadByte());
            }
        }
    }
}
