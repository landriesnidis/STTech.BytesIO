using System;
using System.Text;

namespace STTech.BytesIO.Mqtt
{
    /// <summary>
    /// MQTT 应用业务消息。
    /// </summary>
    public class MqttApplicationMessage
    {
        /// <summary>
        /// 消息主题。
        /// </summary>
        public string Topic { get; set; } = string.Empty;

        /// <summary>
        /// 消息载荷原始字节。
        /// </summary>
        public byte[] Payload { get; set; } = Array.Empty<byte>();

        /// <summary>
        /// 服务质量等级。
        /// </summary>
        public MqttQualityOfServiceLevel QualityOfServiceLevel { get; set; } = MqttQualityOfServiceLevel.AtMostOnce;

        /// <summary>
        /// 是否保留消息 (Retain)。
        /// </summary>
        public bool Retain { get; set; }

        /// <summary>
        /// 是否重复投递 (Dup)。
        /// </summary>
        public bool Dup { get; set; }

        /// <summary>
        /// MQTT v5 属性集合。
        /// </summary>
        public MqttProperties Properties { get; set; } = new MqttProperties();

        /// <summary>
        /// 响应主题 (MQTT v5 请求-响应模型)。快捷访问 <see cref="Properties"/>.ResponseTopic。
        /// </summary>
        public string ResponseTopic
        {
            get => Properties?.ResponseTopic;
            set
            {
                if (Properties == null) Properties = new MqttProperties();
                Properties.ResponseTopic = value;
            }
        }

        public MqttApplicationMessage() { }

        public MqttApplicationMessage(string topic, byte[] payload = null, MqttQualityOfServiceLevel qos = MqttQualityOfServiceLevel.AtMostOnce, bool retain = false)
        {
            Topic = topic;
            Payload = payload ?? Array.Empty<byte>();
            QualityOfServiceLevel = qos;
            Retain = retain;
        }

        public MqttApplicationMessage(string topic, string payloadString, Encoding encoding = null, MqttQualityOfServiceLevel qos = MqttQualityOfServiceLevel.AtMostOnce, bool retain = false)
            : this(topic, (encoding ?? Encoding.UTF8).GetBytes(payloadString ?? string.Empty), qos, retain)
        {
        }

        /// <summary>
        /// 将载荷转换为字符串。
        /// </summary>
        public string ConvertPayloadToString(Encoding encoding = null)
        {
            if (Payload == null || Payload.Length == 0) return string.Empty;
            return (encoding ?? Encoding.UTF8).GetString(Payload);
        }
    }
}
