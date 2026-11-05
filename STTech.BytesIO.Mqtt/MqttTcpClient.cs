using STTech.BytesIO.Tcp;
using System;
using System.Net;

namespace STTech.BytesIO.Mqtt
{
    /// <summary>
    /// MQTT TCP 通信客户端。
    /// 基于 BytesIO 核心库的 TcpClient 实现，支持 MQTT v5.0 标准。
    /// </summary>
    public class MqttTcpClient : MqttClient<TcpClient>, ITcpClient
    {
        /// <summary>
        /// 构造 MQTT TCP 客户端。
        /// </summary>
        /// <param name="version">协议版本，默认为 V5</param>
        public MqttTcpClient(MqttProtocolVersion version = MqttProtocolVersion.V5) : base(new TcpClient { Port = 1883 }, version)
        {
        }

        /// <summary>
        /// 构造 MQTT TCP 客户端并指定服务器地址与端口。
        /// </summary>
        /// <param name="host">MQTT 服务器地址</param>
        /// <param name="port">MQTT 服务器端口，默认 1883</param>
        /// <param name="version">协议版本，默认为 V5</param>
        public MqttTcpClient(string host, int port = 1883, MqttProtocolVersion version = MqttProtocolVersion.V5) : base(new TcpClient { Host = host, Port = port }, version)
        {
        }

        /// <inheritdoc/>
        public string Host { get => InnerClient.Host; set => InnerClient.Host = value; }

        /// <inheritdoc/>
        public int Port { get => InnerClient.Port; set => InnerClient.Port = value; }

        /// <inheritdoc/>
        [Obsolete("该属性已过时。请通过 LocalEndPoint 属性获取本地端口号。")]
        public int LocalPort => InnerClient.LocalPort;

        /// <inheritdoc/>
        public IPEndPoint RemoteEndPoint => InnerClient.RemoteEndPoint;

        /// <inheritdoc/>
        public IPEndPoint LocalEndPoint { get => InnerClient.LocalEndPoint; set => InnerClient.LocalEndPoint = value; }
    }
}
