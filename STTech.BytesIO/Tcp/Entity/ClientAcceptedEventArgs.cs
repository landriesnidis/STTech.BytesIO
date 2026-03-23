using STTech.BytesIO.Core;
using System;

namespace STTech.BytesIO.Tcp
{
    /// <summary>
    /// 客户端连接已被接收事件参数
    /// </summary>
    public class ClientAcceptedEventArgs : EventArgs
    {
        /// <summary>
        /// 客户端 Socket 对象
        /// </summary>
        public System.Net.Sockets.Socket ClientSocket { get;  }

        /// <summary>
        /// 构造客户端连接已被接收事件参数
        /// </summary>
        /// <param name="clientSocket">客户端 Socket 对象</param>
        public ClientAcceptedEventArgs(System.Net.Sockets.Socket clientSocket)
        {
            ClientSocket = clientSocket;
        }
    }

    /// <summary>
    /// 客户端已连接事件参数
    /// </summary>
    public class ClientConnectedEventArgs : EventArgs
    {
        /// <summary>
        /// 构造客户端已连接事件参数
        /// </summary>
        /// <param name="clientSocket">客户端 Socket 对象</param>
        /// <param name="client">TCP 客户端对象</param>
        public ClientConnectedEventArgs(System.Net.Sockets.Socket clientSocket, TcpClient client)
        {
            Socket = clientSocket;
            Client = client;
        }

        /// <summary>
        /// 客户端 Socket 对象
        /// </summary>
        public System.Net.Sockets.Socket Socket { get;  }

        /// <summary>
        /// TCP 客户端对象
        /// </summary>
        public TcpClient Client { get;  }
    }

    /// <summary>
    /// 客户端已断开连接事件参数
    /// </summary>
    public class ClientDisconnectedEventArgs : DisconnectedEventArgs
    {
        /// <summary>
        /// 构造客户端已断开连接事件参数
        /// </summary>
        /// <param name="client">TCP 客户端对象</param>
        public ClientDisconnectedEventArgs(TcpClient client)
        {
            Client = client;
        }

        public ClientDisconnectedEventArgs(TcpClient client, DisconnectionReasonCode reasonCode, Exception exception = null) : base(reasonCode, exception)
        {
            Client = client;
        }

        /// <summary>
        /// TCP 客户端对象
        /// </summary>
        public TcpClient Client { get;  }
    }
}
