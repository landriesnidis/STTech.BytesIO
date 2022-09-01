using STTech.BytesIO.Tcp.Entity;
using System.Net;

namespace STTech.BytesIO.Tcp
{
    public interface ITcpClient
    {
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
        /// 接受缓存区大小
        /// </summary>
        int ReceiveBufferSize { get; set; }

        /// <summary>
        /// 发送缓存区大小
        /// </summary>
        int SendBufferSize { get; set; }

        /// <summary>
        /// 远程终端信息
        /// </summary>
        IPEndPoint RemoteEndPoint { get; }
    }


}
