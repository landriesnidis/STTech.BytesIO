using STTech.BytesIO.Tcp.Entity;

namespace STTech.BytesIO.Tcp
{
    public interface ITcpClient : ITcpSSL
    {
        /// <summary>
        /// 是否已连接
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// 本地端口号
        /// </summary>
        int LocalPort { get; }

        /// <summary>
        ///远程主机网络地址
        /// </summary>
        string Host { get; set; }

        /// <summary>
        /// 远程主机端口号
        /// </summary>
        int Port { get; set; }

        /// <summary>
        /// 接受缓存区大小（默认64kb）
        /// </summary>
        int ReceiveBufferSize { get; set; }

        /// <summary>
        /// 发送缓存区大小（默认32kb）
        /// </summary>
        int SendBufferSize { get; set; }
    }


}
