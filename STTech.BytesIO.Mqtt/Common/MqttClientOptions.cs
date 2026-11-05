using System;

namespace STTech.BytesIO.Mqtt
{
    /// <summary>
    /// MQTT 客户端连接配置选项。
    /// </summary>
    public class MqttClientOptions
    {
        /// <summary>
        /// 客户端标识符 (Client ID)。若为空则自动生成 GUID 标识。
        /// </summary>
        public string ClientId { get; set; } = Guid.NewGuid().ToString("N");

        /// <summary>
        /// 用户名。
        /// </summary>
        public string UserName { get; set; }

        /// <summary>
        /// 密码。
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        /// 心跳保活时间（秒），默认为 60 秒。
        /// </summary>
        public ushort KeepAlive { get; set; } = 60;

        /// <summary>
        /// 是否全新启动会话 (Clean Start)，默认为 true。
        /// </summary>
        public bool CleanStart { get; set; } = true;

        /// <summary>
        /// 会话过期时间（秒），MQTT v5 特性。
        /// </summary>
        public uint? SessionExpiryInterval { get; set; }

        /// <summary>
        /// CONNECT 报文附加属性。
        /// </summary>
        public MqttProperties Properties { get; set; } = new MqttProperties();

        /// <summary>
        /// 协议通信默认超时时间。
        /// </summary>
        public TimeSpan CommunicationTimeout { get; set; } = TimeSpan.FromSeconds(10);

        /// <summary>
        /// 断线重连后是否自动补发订阅历史主题，默认为 true。
        /// </summary>
        public bool AutoResubscribe { get; set; } = true;

        #region Will Message

        /// <summary>
        /// 是否配置遗嘱消息。
        /// </summary>
        public bool HasWill { get; set; }

        /// <summary>
        /// 遗嘱主题。
        /// </summary>
        public string WillTopic { get; set; }

        /// <summary>
        /// 遗嘱载荷。
        /// </summary>
        public byte[] WillPayload { get; set; }

        /// <summary>
        /// 遗嘱 QoS 等级。
        /// </summary>
        public MqttQualityOfServiceLevel WillQoS { get; set; } = MqttQualityOfServiceLevel.AtMostOnce;

        /// <summary>
        /// 遗嘱保留标志。
        /// </summary>
        public bool WillRetain { get; set; }

        /// <summary>
        /// 遗嘱属性集合。
        /// </summary>
        public MqttProperties WillProperties { get; set; } = new MqttProperties();

        #endregion
    }
}
