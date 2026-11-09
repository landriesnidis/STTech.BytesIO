using System;
using System.Net;
using System.Net.Quic;

namespace STTech.BytesIO.Quic
{
    /// <summary>
    /// QUIC 物理连接建立事件参数
    /// </summary>
    public class QuicConnectionAcceptedEventArgs : EventArgs
    {
        /// <summary>
        /// 建立的物理 QUIC 连接
        /// </summary>
        public QuicConnection Connection { get; }

        /// <summary>
        /// 对端网络端点
        /// </summary>
        public EndPoint? RemoteEndPoint => Connection.RemoteEndPoint;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="connection">已完成握手的底层 QUIC 连接</param>
        public QuicConnectionAcceptedEventArgs(QuicConnection connection)
        {
            Connection = connection ?? throw new ArgumentNullException(nameof(connection));
        }
    }
}
