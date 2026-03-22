using STTech.BytesIO.Core;
using System;
using System.IO.Pipes;

namespace STTech.BytesIO.Ipc
{
    /// <summary>
    /// IPC客户端已接受连接的处理参数
    /// </summary>
    public class IpcClientAcceptedEventArgs : EventArgs
    {
        /// <summary>
        /// 管道服务端流对象
        /// </summary>
        public NamedPipeServerStream PipeStream { get; }

        /// <summary>
        /// 构造IPC客户端已接受连接的处理参数
        /// </summary>
        /// <param name="pipeStream"></param>
        public IpcClientAcceptedEventArgs(NamedPipeServerStream pipeStream)
        {
            PipeStream = pipeStream;
        }
    }

    /// <summary>
    /// IPC客户端已建立连接的处理参数
    /// </summary>
    public class IpcClientConnectedEventArgs : EventArgs
    {
        /// <summary>
        /// 构造IPC客户端已建立连接的处理参数
        /// </summary>
        /// <param name="pipeStream"></param>
        /// <param name="client"></param>
        public IpcClientConnectedEventArgs(NamedPipeServerStream pipeStream, IpcClient client)
        {
            PipeStream = pipeStream;
            Client = client;
        }

        /// <summary>
        /// 管道服务端流对象
        /// </summary>
        public NamedPipeServerStream PipeStream { get; }

        /// <summary>
        /// IPC客户端对象
        /// </summary>
        public IpcClient Client { get; }
    }

    /// <summary>
    /// IPC客户端已断开连接的处理参数
    /// </summary>
    public class IpcClientDisconnectedEventArgs : DisconnectedEventArgs
    {
        /// <summary>
        /// 构造IPC客户端已断开连接的处理参数
        /// </summary>
        /// <param name="client"></param>
        /// <param name="e"></param>
        public IpcClientDisconnectedEventArgs(IpcClient client, DisconnectedEventArgs e) : base(e.ReasonCode, e.Exception)
        {
            Client = client;
            ConnectionId = e.ConnectionId;
            Duration = e.Duration;
        }

        /// <summary>
        /// IPC客户端对象
        /// </summary>
        public IpcClient Client { get; }
    }
}
