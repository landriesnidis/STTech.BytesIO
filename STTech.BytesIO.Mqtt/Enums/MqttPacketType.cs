namespace STTech.BytesIO.Mqtt
{
    /// <summary>
    /// MQTT 控制报文类型。
    /// </summary>
    public enum MqttPacketType : byte
    {
        /// <summary>
        /// 保留。
        /// </summary>
        Reserved = 0,

        /// <summary>
        /// 客户端请求连接服务端。
        /// </summary>
        Connect = 1,

        /// <summary>
        /// 服务端确认连接请求。
        /// </summary>
        ConnAck = 2,

        /// <summary>
        /// 发布消息。
        /// </summary>
        Publish = 3,

        /// <summary>
        /// QoS 1 消息发布确认。
        /// </summary>
        PubAck = 4,

        /// <summary>
        /// QoS 2 消息发布收到（保证交付第一步）。
        /// </summary>
        PubRec = 5,

        /// <summary>
        /// QoS 2 消息发布释放（保证交付第二步）。
        /// </summary>
        PubRel = 6,

        /// <summary>
        /// QoS 2 消息发布完成（保证交付第三步）。
        /// </summary>
        PubComp = 7,

        /// <summary>
        /// 订阅主题请求。
        /// </summary>
        Subscribe = 8,

        /// <summary>
        /// 订阅确认。
        /// </summary>
        SubAck = 9,

        /// <summary>
        /// 取消订阅请求。
        /// </summary>
        Unsubscribe = 10,

        /// <summary>
        /// 取消订阅确认。
        /// </summary>
        UnsubAck = 11,

        /// <summary>
        /// 心跳请求 (PING)。
        /// </summary>
        PingReq = 12,

        /// <summary>
        /// 心跳响应 (PONG)。
        /// </summary>
        PingResp = 13,

        /// <summary>
        /// 断开连接通知。
        /// </summary>
        Disconnect = 14,

        /// <summary>
        /// 认证信息交换。
        /// </summary>
        Auth = 15,
    }
}
