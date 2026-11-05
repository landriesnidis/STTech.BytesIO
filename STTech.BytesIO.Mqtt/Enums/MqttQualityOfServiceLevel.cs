namespace STTech.BytesIO.Mqtt
{
    /// <summary>
    /// MQTT 服务质量等级 (QoS)。
    /// </summary>
    public enum MqttQualityOfServiceLevel : byte
    {
        /// <summary>
        /// QoS 0：最多交付一次（尽力交付，可能丢失）。
        /// </summary>
        AtMostOnce = 0,

        /// <summary>
        /// QoS 1：至少交付一次（保证到达，可能重复）。
        /// </summary>
        AtLeastOnce = 1,

        /// <summary>
        /// QoS 2：精确交付一次（保证仅交付一次，无丢失无重复）。
        /// </summary>
        ExactlyOnce = 2,
    }

    /// <summary>
    /// MQTT 有效载荷格式指示符。
    /// </summary>
    public enum MqttPayloadFormatIndicator : byte
    {
        /// <summary>
        /// 未指定的字节流（原始字节）。
        /// </summary>
        Unspecified = 0,

        /// <summary>
        /// UTF-8 编码的字符数据。
        /// </summary>
        Utf8Encoded = 1,
    }

    /// <summary>
    /// MQTT v5 报文属性标识符。
    /// </summary>
    public enum MqttPropertyId : byte
    {
        /// <summary>
        /// 无属性。
        /// </summary>
        None = 0,

        /// <summary>
        /// 载荷格式指示符（Byte）。
        /// </summary>
        PayloadFormatIndicator = 1,

        /// <summary>
        /// 消息过期时间间隔（Four Byte Integer，秒）。
        /// </summary>
        MessageExpiryInterval = 2,

        /// <summary>
        /// 内容类型（UTF-8 String）。
        /// </summary>
        ContentType = 3,

        /// <summary>
        /// 响应主题（UTF-8 String）。
        /// </summary>
        ResponseTopic = 8,

        /// <summary>
        /// 相关性数据（Binary Data）。
        /// </summary>
        CorrelationData = 9,

        /// <summary>
        /// 订阅标识符（Variable Byte Integer）。
        /// </summary>
        SubscriptionIdentifier = 11,

        /// <summary>
        /// 会话过期时间间隔（Four Byte Integer，秒）。
        /// </summary>
        SessionExpiryInterval = 17,

        /// <summary>
        /// 分配的客户端标识符（UTF-8 String）。
        /// </summary>
        AssignedClientIdentifier = 18,

        /// <summary>
        /// 服务端保持连接时长（Two Byte Integer，秒）。
        /// </summary>
        ServerKeepAlive = 19,

        /// <summary>
        /// 认证方法（UTF-8 String）。
        /// </summary>
        AuthenticationMethod = 21,

        /// <summary>
        /// 认证数据（Binary Data）。
        /// </summary>
        AuthenticationData = 22,

        /// <summary>
        /// 请求问题信息（Byte）。
        /// </summary>
        RequestProblemInformation = 23,

        /// <summary>
        /// 遗嘱延迟时间间隔（Four Byte Integer，秒）。
        /// </summary>
        WillDelayInterval = 24,

        /// <summary>
        /// 请求响应信息（Byte）。
        /// </summary>
        RequestResponseInformation = 25,

        /// <summary>
        /// 响应信息（UTF-8 String）。
        /// </summary>
        ResponseInformation = 26,

        /// <summary>
        /// 服务端引用（UTF-8 String）。
        /// </summary>
        ServerReference = 28,

        /// <summary>
        /// 原因字符串（UTF-8 String）。
        /// </summary>
        ReasonString = 31,

        /// <summary>
        /// 接收最大值（Two Byte Integer）。
        /// </summary>
        ReceiveMaximum = 33,

        /// <summary>
        /// 主题别名最大值（Two Byte Integer）。
        /// </summary>
        TopicAliasMaximum = 34,

        /// <summary>
        /// 主题别名（Two Byte Integer）。
        /// </summary>
        TopicAlias = 35,

        /// <summary>
        /// 最大 QoS 等级（Byte）。
        /// </summary>
        MaximumQoS = 36,

        /// <summary>
        /// 保留消息可用性（Byte）。
        /// </summary>
        RetainAvailable = 37,

        /// <summary>
        /// 用户属性（UTF-8 String Pair）。
        /// </summary>
        UserProperty = 38,

        /// <summary>
        /// 最大数据包大小（Four Byte Integer）。
        /// </summary>
        MaximumPacketSize = 39,

        /// <summary>
        /// 通配符订阅可用性（Byte）。
        /// </summary>
        WildcardSubscriptionAvailable = 40,

        /// <summary>
        /// 订阅标识符可用性（Byte）。
        /// </summary>
        SubscriptionIdentifiersAvailable = 41,

        /// <summary>
        /// 共享订阅可用性（Byte）。
        /// </summary>
        SharedSubscriptionAvailable = 42,
    }
}
