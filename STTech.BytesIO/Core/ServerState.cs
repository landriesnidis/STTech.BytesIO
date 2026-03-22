namespace STTech.BytesIO.Core
{
    /// <summary>
    /// 服务器状态
    /// </summary>
    public enum ServerState
    {
        /// <summary>
        /// 服务器处于关闭状态
        /// </summary>
        Closed,
        /// <summary>
        /// 正在监听新客户端的加入
        /// </summary>
        Listening,
        /// <summary>
        /// 停止监听新客户端的连接，保留现有客户端的通信
        /// </summary>
        Paused,
    }
}
