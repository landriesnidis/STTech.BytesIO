using System;

namespace STTech.BytesIO.Core
{
    /// <summary>
    /// 服务器客户端连接事件参数
    /// </summary>
    /// <typeparam name="TClient"></typeparam>
    public class ServerClientConnectedEventArgs<TClient> : EventArgs where TClient : BytesClient
    {
        /// <summary>
        /// 客户端对象
        /// </summary>
        public TClient Client { get; }

        /// <summary>
        /// 初始化 <see cref="ServerClientConnectedEventArgs{TClient}"/> 类的新实例。
        /// </summary>
        /// <param name="client">客户端对象</param>
        public ServerClientConnectedEventArgs(TClient client)
        {
            Client = client;
        }
    }

    /// <summary>
    /// 服务器客户端断开连接事件参数
    /// </summary>
    /// <typeparam name="TClient"></typeparam>
    public class ServerClientDisconnectedEventArgs<TClient> : DisconnectedEventArgs where TClient : BytesClient
    {
        /// <summary>
        /// 客户端对象
        /// </summary>
        public TClient Client { get; }

        /// <summary>
        /// 初始化 <see cref="ServerClientDisconnectedEventArgs{TClient}"/> 类的新实例。
        /// </summary>
        /// <param name="client">客户端对象</param>
        /// <param name="e">断开连接事件参数</param>
        public ServerClientDisconnectedEventArgs(TClient client, DisconnectedEventArgs e) : base(e.ReasonCode, e.Exception)
        {
            Client = client;
            this.ConnectionId = e.ConnectionId;
            this.Duration = e.Duration;
        }
    }
}
