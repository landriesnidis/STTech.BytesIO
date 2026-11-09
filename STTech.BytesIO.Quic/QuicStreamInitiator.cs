namespace STTech.BytesIO.Quic
{
    /// <summary>
    /// QUIC 初始通信流的发起方式
    /// </summary>
    public enum QuicStreamInitiator
    {
        /// <summary>
        /// 客户端主动向服务端开辟出站流（默认标准模式，适用于客户端先发报文或标准请求响应模型）
        /// </summary>
        Client = 0,

        /// <summary>
        /// 客户端等待服务端主动开辟流（适用于“服务端先发言/下发问候语或挑战指令”的工业协议模式）
        /// </summary>
        Server = 1
    }
}
