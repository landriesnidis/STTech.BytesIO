namespace STTech.BytesIO.Udp
{
    public interface IUdpClient
    {
        /// <summary>
        /// 本地端口号
        /// </summary>
        int LocalPort { get; set; }

        /// <summary>
        /// 远程主机网络地址
        /// </summary>
        string Host { get; set; }

        /// <summary>
        /// 远程主机端口号
        /// </summary>
        int Port { get; set; }

        /// <summary>
        /// 允许匿名消息
        /// 非Host来源的消息
        /// </summary>
        bool AllowReceivingDataFromAnyIP { get; set; }
    }
}
