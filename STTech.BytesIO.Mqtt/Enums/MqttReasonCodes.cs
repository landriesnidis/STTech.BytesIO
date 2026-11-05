namespace STTech.BytesIO.Mqtt
{
    /// <summary>
    /// MQTT 连接确认原因码 (CONNACK Reason Code)。
    /// </summary>
    public enum MqttConnectReasonCode : byte
    {
        Success = 0x00,
        UnspecifiedError = 0x80,
        MalformedPacket = 0x81,
        ProtocolError = 0x82,
        ImplementationSpecificError = 0x83,
        UnsupportedProtocolVersion = 0x84,
        ClientIdentifierNotValid = 0x85,
        BadUserNameOrPassword = 0x86,
        NotAuthorized = 0x87,
        ServerUnavailable = 0x88,
        ServerBusy = 0x89,
        Banned = 0x8A,
        BadAuthenticationMethod = 0x8C,
        TopicNameInvalid = 0x90,
        PacketTooLarge = 0x95,
        QuotaExceeded = 0x97,
        PayloadFormatInvalid = 0x99,
        RetainNotSupported = 0x9A,
        QoSNotSupported = 0x9B,
        UseAnotherServer = 0x9C,
        ServerMoved = 0x9D,
        SharedSubscriptionsNotSupported = 0x9E,
        ConnectionRateExceeded = 0x9F,
        MaximumConnectTime = 0xA0,
        SubscriptionIdentifiersNotSupported = 0xA1,
        WildcardSubscriptionsNotSupported = 0xA2,
    }

    /// <summary>
    /// MQTT 订阅确认原因码 (SUBACK Reason Code)。
    /// </summary>
    public enum MqttSubAckReasonCode : byte
    {
        GrantedQoS0 = 0x00,
        GrantedQoS1 = 0x01,
        GrantedQoS2 = 0x02,
        UnspecifiedError = 0x80,
        ImplementationSpecificError = 0x83,
        NotAuthorized = 0x87,
        TopicFilterInvalid = 0x8F,
        PacketIdentifierInUse = 0x91,
        QuotaExceeded = 0x97,
        SharedSubscriptionsNotSupported = 0x9E,
        SubscriptionIdentifiersNotSupported = 0xA1,
        WildcardSubscriptionsNotSupported = 0xA2,
    }

    /// <summary>
    /// MQTT 取消订阅确认原因码 (UNSUBACK Reason Code)。
    /// </summary>
    public enum MqttUnsubAckReasonCode : byte
    {
        Success = 0x00,
        NoSubscriptionExisted = 0x11,
        UnspecifiedError = 0x80,
        ImplementationSpecificError = 0x83,
        NotAuthorized = 0x87,
        TopicFilterInvalid = 0x8F,
        PacketIdentifierInUse = 0x91,
    }

    /// <summary>
    /// MQTT 发布确认原因码 (PUBACK Reason Code)。
    /// </summary>
    public enum MqttPubAckReasonCode : byte
    {
        Success = 0x00,
        NoMatchingSubscribers = 0x10,
        UnspecifiedError = 0x80,
        ImplementationSpecificError = 0x83,
        NotAuthorized = 0x87,
        TopicNameInvalid = 0x90,
        PacketIdentifierInUse = 0x91,
        QuotaExceeded = 0x97,
        PayloadFormatInvalid = 0x99,
    }

    /// <summary>
    /// MQTT QoS 2 收到原因码 (PUBREC Reason Code)。
    /// </summary>
    public enum MqttPubRecReasonCode : byte
    {
        Success = 0x00,
        NoMatchingSubscribers = 0x10,
        UnspecifiedError = 0x80,
        ImplementationSpecificError = 0x83,
        NotAuthorized = 0x87,
        TopicNameInvalid = 0x90,
        PacketIdentifierInUse = 0x91,
        QuotaExceeded = 0x97,
        PayloadFormatInvalid = 0x99,
    }

    /// <summary>
    /// MQTT QoS 2 释放原因码 (PUBREL Reason Code)。
    /// </summary>
    public enum MqttPubRelReasonCode : byte
    {
        Success = 0x00,
        PacketIdentifierNotFound = 0x92,
    }

    /// <summary>
    /// MQTT QoS 2 完成原因码 (PUBCOMP Reason Code)。
    /// </summary>
    public enum MqttPubCompReasonCode : byte
    {
        Success = 0x00,
        PacketIdentifierNotFound = 0x92,
    }

    /// <summary>
    /// MQTT 断开连接原因码 (DISCONNECT Reason Code)。
    /// </summary>
    public enum MqttDisconnectReasonCode : byte
    {
        NormalDisconnection = 0x00,
        DisconnectWithWillMessage = 0x04,
        UnspecifiedError = 0x80,
        MalformedPacket = 0x81,
        ProtocolError = 0x82,
        ImplementationSpecificError = 0x83,
        NotAuthorized = 0x87,
        ServerBusy = 0x89,
        ServerShuttingDown = 0x8B,
        KeepAliveTimeout = 0x8D,
        SessionTakenOver = 0x8E,
        TopicFilterInvalid = 0x8F,
        TopicNameInvalid = 0x90,
        ReceiveMaximumExceeded = 0x93,
        TopicAliasInvalid = 0x94,
        PacketTooLarge = 0x95,
        MessageRateTooHigh = 0x96,
        QuotaExceeded = 0x97,
        AdministrativeAction = 0x98,
        PayloadFormatInvalid = 0x99,
        RetainNotSupported = 0x9A,
        QoSNotSupported = 0x9B,
        UseAnotherServer = 0x9C,
        ServerMoved = 0x9D,
        SharedSubscriptionsNotSupported = 0x9E,
        ConnectionRateExceeded = 0x9F,
        MaximumConnectTime = 0xA0,
        SubscriptionIdentifiersNotSupported = 0xA1,
        WildcardSubscriptionsNotSupported = 0xA2,
    }
}
