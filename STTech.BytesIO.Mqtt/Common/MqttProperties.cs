using System;
using System.Collections.Generic;

namespace STTech.BytesIO.Mqtt
{
    /// <summary>
    /// MQTT v5 报文属性集合。
    /// </summary>
    public sealed class MqttProperties
    {
        public byte? PayloadFormatIndicator { get; set; }
        public uint? MessageExpiryInterval { get; set; }
        public string ContentType { get; set; }
        public string ResponseTopic { get; set; }
        public byte[] CorrelationData { get; set; }
        public int? SubscriptionIdentifier { get; set; }
        public uint? SessionExpiryInterval { get; set; }
        public string AssignedClientIdentifier { get; set; }
        public ushort? ServerKeepAlive { get; set; }
        public string AuthenticationMethod { get; set; }
        public byte[] AuthenticationData { get; set; }
        public byte? RequestProblemInformation { get; set; }
        public uint? WillDelayInterval { get; set; }
        public byte? RequestResponseInformation { get; set; }
        public string ResponseInformation { get; set; }
        public string ServerReference { get; set; }
        public string ReasonString { get; set; }
        public ushort? ReceiveMaximum { get; set; }
        public ushort? TopicAliasMaximum { get; set; }
        public ushort? TopicAlias { get; set; }
        public byte? MaximumQoS { get; set; }
        public byte? RetainAvailable { get; set; }
        public uint? MaximumPacketSize { get; set; }
        public byte? WildcardSubscriptionAvailable { get; set; }
        public byte? SubscriptionIdentifiersAvailable { get; set; }
        public byte? SharedSubscriptionAvailable { get; set; }

        private List<MqttUserProperty> _userProperties;

        /// <summary>
        /// 用户属性列表。
        /// </summary>
        public List<MqttUserProperty> UserProperties
        {
            get => _userProperties ?? (_userProperties = new List<MqttUserProperty>());
            set => _userProperties = value;
        }

        public bool HasUserProperties => _userProperties != null && _userProperties.Count > 0;

        /// <summary>
        /// 检查是否有任何属性设置。
        /// </summary>
        public bool IsEmpty =>
            !PayloadFormatIndicator.HasValue &&
            !MessageExpiryInterval.HasValue &&
            ContentType == null &&
            ResponseTopic == null &&
            CorrelationData == null &&
            !SubscriptionIdentifier.HasValue &&
            !SessionExpiryInterval.HasValue &&
            AssignedClientIdentifier == null &&
            !ServerKeepAlive.HasValue &&
            AuthenticationMethod == null &&
            AuthenticationData == null &&
            !RequestProblemInformation.HasValue &&
            !WillDelayInterval.HasValue &&
            !RequestResponseInformation.HasValue &&
            ResponseInformation == null &&
            ServerReference == null &&
            ReasonString == null &&
            !ReceiveMaximum.HasValue &&
            !TopicAliasMaximum.HasValue &&
            !TopicAlias.HasValue &&
            !MaximumQoS.HasValue &&
            !RetainAvailable.HasValue &&
            !MaximumPacketSize.HasValue &&
            !WildcardSubscriptionAvailable.HasValue &&
            !SubscriptionIdentifiersAvailable.HasValue &&
            !SharedSubscriptionAvailable.HasValue &&
            !HasUserProperties;

        public void WriteTo(MqttBinaryWriter writer)
        {
            if (IsEmpty)
            {
                writer.WriteVariableByteInteger(0);
                return;
            }

            using (var propWriter = new MqttBinaryWriter())
            {
                if (PayloadFormatIndicator.HasValue)
                {
                    propWriter.WriteByte((byte)MqttPropertyId.PayloadFormatIndicator);
                    propWriter.WriteByte(PayloadFormatIndicator.Value);
                }
                if (MessageExpiryInterval.HasValue)
                {
                    propWriter.WriteByte((byte)MqttPropertyId.MessageExpiryInterval);
                    propWriter.WriteFourByteInteger(MessageExpiryInterval.Value);
                }
                if (ContentType != null)
                {
                    propWriter.WriteByte((byte)MqttPropertyId.ContentType);
                    propWriter.WriteString(ContentType);
                }
                if (ResponseTopic != null)
                {
                    propWriter.WriteByte((byte)MqttPropertyId.ResponseTopic);
                    propWriter.WriteString(ResponseTopic);
                }
                if (CorrelationData != null)
                {
                    propWriter.WriteByte((byte)MqttPropertyId.CorrelationData);
                    propWriter.WriteBinaryData(CorrelationData);
                }
                if (SubscriptionIdentifier.HasValue)
                {
                    propWriter.WriteByte((byte)MqttPropertyId.SubscriptionIdentifier);
                    propWriter.WriteVariableByteInteger(SubscriptionIdentifier.Value);
                }
                if (SessionExpiryInterval.HasValue)
                {
                    propWriter.WriteByte((byte)MqttPropertyId.SessionExpiryInterval);
                    propWriter.WriteFourByteInteger(SessionExpiryInterval.Value);
                }
                if (AssignedClientIdentifier != null)
                {
                    propWriter.WriteByte((byte)MqttPropertyId.AssignedClientIdentifier);
                    propWriter.WriteString(AssignedClientIdentifier);
                }
                if (ServerKeepAlive.HasValue)
                {
                    propWriter.WriteByte((byte)MqttPropertyId.ServerKeepAlive);
                    propWriter.WriteTwoByteInteger(ServerKeepAlive.Value);
                }
                if (AuthenticationMethod != null)
                {
                    propWriter.WriteByte((byte)MqttPropertyId.AuthenticationMethod);
                    propWriter.WriteString(AuthenticationMethod);
                }
                if (AuthenticationData != null)
                {
                    propWriter.WriteByte((byte)MqttPropertyId.AuthenticationData);
                    propWriter.WriteBinaryData(AuthenticationData);
                }
                if (RequestProblemInformation.HasValue)
                {
                    propWriter.WriteByte((byte)MqttPropertyId.RequestProblemInformation);
                    propWriter.WriteByte(RequestProblemInformation.Value);
                }
                if (WillDelayInterval.HasValue)
                {
                    propWriter.WriteByte((byte)MqttPropertyId.WillDelayInterval);
                    propWriter.WriteFourByteInteger(WillDelayInterval.Value);
                }
                if (RequestResponseInformation.HasValue)
                {
                    propWriter.WriteByte((byte)MqttPropertyId.RequestResponseInformation);
                    propWriter.WriteByte(RequestResponseInformation.Value);
                }
                if (ResponseInformation != null)
                {
                    propWriter.WriteByte((byte)MqttPropertyId.ResponseInformation);
                    propWriter.WriteString(ResponseInformation);
                }
                if (ServerReference != null)
                {
                    propWriter.WriteByte((byte)MqttPropertyId.ServerReference);
                    propWriter.WriteString(ServerReference);
                }
                if (ReasonString != null)
                {
                    propWriter.WriteByte((byte)MqttPropertyId.ReasonString);
                    propWriter.WriteString(ReasonString);
                }
                if (ReceiveMaximum.HasValue)
                {
                    propWriter.WriteByte((byte)MqttPropertyId.ReceiveMaximum);
                    propWriter.WriteTwoByteInteger(ReceiveMaximum.Value);
                }
                if (TopicAliasMaximum.HasValue)
                {
                    propWriter.WriteByte((byte)MqttPropertyId.TopicAliasMaximum);
                    propWriter.WriteTwoByteInteger(TopicAliasMaximum.Value);
                }
                if (TopicAlias.HasValue)
                {
                    propWriter.WriteByte((byte)MqttPropertyId.TopicAlias);
                    propWriter.WriteTwoByteInteger(TopicAlias.Value);
                }
                if (MaximumQoS.HasValue)
                {
                    propWriter.WriteByte((byte)MqttPropertyId.MaximumQoS);
                    propWriter.WriteByte(MaximumQoS.Value);
                }
                if (RetainAvailable.HasValue)
                {
                    propWriter.WriteByte((byte)MqttPropertyId.RetainAvailable);
                    propWriter.WriteByte(RetainAvailable.Value);
                }
                if (HasUserProperties)
                {
                    foreach (var up in _userProperties)
                    {
                        propWriter.WriteByte((byte)MqttPropertyId.UserProperty);
                        propWriter.WriteString(up.Name);
                        propWriter.WriteString(up.Value);
                    }
                }
                if (MaximumPacketSize.HasValue)
                {
                    propWriter.WriteByte((byte)MqttPropertyId.MaximumPacketSize);
                    propWriter.WriteFourByteInteger(MaximumPacketSize.Value);
                }
                if (WildcardSubscriptionAvailable.HasValue)
                {
                    propWriter.WriteByte((byte)MqttPropertyId.WildcardSubscriptionAvailable);
                    propWriter.WriteByte(WildcardSubscriptionAvailable.Value);
                }
                if (SubscriptionIdentifiersAvailable.HasValue)
                {
                    propWriter.WriteByte((byte)MqttPropertyId.SubscriptionIdentifiersAvailable);
                    propWriter.WriteByte(SubscriptionIdentifiersAvailable.Value);
                }
                if (SharedSubscriptionAvailable.HasValue)
                {
                    propWriter.WriteByte((byte)MqttPropertyId.SharedSubscriptionAvailable);
                    propWriter.WriteByte(SharedSubscriptionAvailable.Value);
                }

                byte[] propBytes = propWriter.ToArray();
                writer.WriteVariableByteInteger(propBytes.Length);
                writer.WriteRawBytes(propBytes);
            }
        }

        public static MqttProperties ReadFrom(MqttBinaryReader reader)
        {
            var properties = new MqttProperties();
            int propertyLength = reader.ReadVariableByteInteger();
            if (propertyLength == 0)
            {
                return properties;
            }

            var propReader = reader.Slice(propertyLength);
            while (propReader.BytesRemaining > 0)
            {
                byte propIdByte = propReader.ReadByte();
                var propId = (MqttPropertyId)propIdByte;

                switch (propId)
                {
                    case MqttPropertyId.PayloadFormatIndicator:
                        properties.PayloadFormatIndicator = propReader.ReadByte();
                        break;
                    case MqttPropertyId.MessageExpiryInterval:
                        properties.MessageExpiryInterval = propReader.ReadFourByteInteger();
                        break;
                    case MqttPropertyId.ContentType:
                        properties.ContentType = propReader.ReadString();
                        break;
                    case MqttPropertyId.ResponseTopic:
                        properties.ResponseTopic = propReader.ReadString();
                        break;
                    case MqttPropertyId.CorrelationData:
                        properties.CorrelationData = propReader.ReadBinaryData();
                        break;
                    case MqttPropertyId.SubscriptionIdentifier:
                        properties.SubscriptionIdentifier = propReader.ReadVariableByteInteger();
                        break;
                    case MqttPropertyId.SessionExpiryInterval:
                        properties.SessionExpiryInterval = propReader.ReadFourByteInteger();
                        break;
                    case MqttPropertyId.AssignedClientIdentifier:
                        properties.AssignedClientIdentifier = propReader.ReadString();
                        break;
                    case MqttPropertyId.ServerKeepAlive:
                        properties.ServerKeepAlive = propReader.ReadTwoByteInteger();
                        break;
                    case MqttPropertyId.AuthenticationMethod:
                        properties.AuthenticationMethod = propReader.ReadString();
                        break;
                    case MqttPropertyId.AuthenticationData:
                        properties.AuthenticationData = propReader.ReadBinaryData();
                        break;
                    case MqttPropertyId.RequestProblemInformation:
                        properties.RequestProblemInformation = propReader.ReadByte();
                        break;
                    case MqttPropertyId.WillDelayInterval:
                        properties.WillDelayInterval = propReader.ReadFourByteInteger();
                        break;
                    case MqttPropertyId.RequestResponseInformation:
                        properties.RequestResponseInformation = propReader.ReadByte();
                        break;
                    case MqttPropertyId.ResponseInformation:
                        properties.ResponseInformation = propReader.ReadString();
                        break;
                    case MqttPropertyId.ServerReference:
                        properties.ServerReference = propReader.ReadString();
                        break;
                    case MqttPropertyId.ReasonString:
                        properties.ReasonString = propReader.ReadString();
                        break;
                    case MqttPropertyId.ReceiveMaximum:
                        properties.ReceiveMaximum = propReader.ReadTwoByteInteger();
                        break;
                    case MqttPropertyId.TopicAliasMaximum:
                        properties.TopicAliasMaximum = propReader.ReadTwoByteInteger();
                        break;
                    case MqttPropertyId.TopicAlias:
                        properties.TopicAlias = propReader.ReadTwoByteInteger();
                        break;
                    case MqttPropertyId.MaximumQoS:
                        properties.MaximumQoS = propReader.ReadByte();
                        break;
                    case MqttPropertyId.RetainAvailable:
                        properties.RetainAvailable = propReader.ReadByte();
                        break;
                    case MqttPropertyId.UserProperty:
                        string key = propReader.ReadString();
                        string val = propReader.ReadString();
                        properties.UserProperties.Add(new MqttUserProperty(key, val));
                        break;
                    case MqttPropertyId.MaximumPacketSize:
                        properties.MaximumPacketSize = propReader.ReadFourByteInteger();
                        break;
                    case MqttPropertyId.WildcardSubscriptionAvailable:
                        properties.WildcardSubscriptionAvailable = propReader.ReadByte();
                        break;
                    case MqttPropertyId.SubscriptionIdentifiersAvailable:
                        properties.SubscriptionIdentifiersAvailable = propReader.ReadByte();
                        break;
                    case MqttPropertyId.SharedSubscriptionAvailable:
                        properties.SharedSubscriptionAvailable = propReader.ReadByte();
                        break;
                    default:
                        // 遇到未识别的属性，按规范抛出或忽略
                        throw new MqttProtocolViolationException($"未知或不支持的 MQTT v5 属性标识符: 0x{propIdByte:X2}");
                }
            }

            return properties;
        }
    }
}
