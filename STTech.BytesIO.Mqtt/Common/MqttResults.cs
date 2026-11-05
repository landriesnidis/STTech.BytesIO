using System;
using System.Collections.Generic;

namespace STTech.BytesIO.Mqtt
{
    /// <summary>
    /// MQTT 连接操作结果。
    /// </summary>
    public class MqttConnectResult
    {
        public bool IsSuccess => ReasonCode == MqttConnectReasonCode.Success;
        public MqttConnectReasonCode ReasonCode { get; set; } = MqttConnectReasonCode.Success;
        public bool SessionPresent { get; set; }
        public MqttProperties Properties { get; set; } = new MqttProperties();
        public Exception Exception { get; set; }
        public string ReasonString => Properties?.ReasonString;
    }

    /// <summary>
    /// MQTT 发布操作结果。
    /// </summary>
    public class MqttPublishResult
    {
        public bool IsSuccess { get; set; }
        public ushort PacketId { get; set; }
        public MqttPubAckReasonCode? ReasonCode { get; set; }
        public MqttProperties Properties { get; set; } = new MqttProperties();
        public Exception Exception { get; set; }
        public string ReasonString => Properties?.ReasonString;
    }

    /// <summary>
    /// MQTT 订阅操作结果。
    /// </summary>
    public class MqttSubscribeResult
    {
        public bool IsSuccess { get; set; }
        public ushort PacketId { get; set; }
        public List<MqttSubAckReasonCode> ReasonCodes { get; set; } = new List<MqttSubAckReasonCode>();
        public MqttProperties Properties { get; set; } = new MqttProperties();
        public Exception Exception { get; set; }
        public string ReasonString => Properties?.ReasonString;
    }

    /// <summary>
    /// MQTT 取消订阅操作结果。
    /// </summary>
    public class MqttUnsubscribeResult
    {
        public bool IsSuccess { get; set; }
        public ushort PacketId { get; set; }
        public List<MqttUnsubAckReasonCode> ReasonCodes { get; set; } = new List<MqttUnsubAckReasonCode>();
        public MqttProperties Properties { get; set; } = new MqttProperties();
        public Exception Exception { get; set; }
        public string ReasonString => Properties?.ReasonString;
    }
}
