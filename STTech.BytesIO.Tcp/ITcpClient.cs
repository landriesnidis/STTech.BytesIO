using STTech.BytesIO.Tcp.Entity;
using System;
using System.Net;

namespace STTech.BytesIO.Tcp
{
    public interface ITcpClient
    {


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

        /// <summary>
        /// 本地终端信息
        /// </summary>
        IPEndPoint LocalEndPoint { get; set; }

        /// <summary>
        /// 本地端口号
        /// </summary>
        [Obsolete("该属性已过时。请通过 LocalEndPoint 属性获取本地端口号。")]
        int LocalPort { get; }
    }


}
