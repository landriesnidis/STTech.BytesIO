using System;
using STTech.BytesIO.Core;
using STTech.BytesIO.Core.Component;
using STTech.BytesIO.Tcp;
using System.Net;

namespace STTech.BytesIO.Modbus
{
    /// <summary>
    /// Modbus TCP 客户端 (主站)
    /// <para>在网络中作为 Master 角色，发起对从站 (Slave) 的读写请求。</para>
    /// </summary>
    public partial class ModbusTcpClient : ModbusClient<TcpClient>
    {
        /// <summary>
        /// 构造 Modbus TCP 客户端
        /// </summary>
        /// <param name="format">协议格式，默认为标准 Modbus TCP (MBAP 报文头格式)，亦可切换为 RTU 格式 (Modbus RTU over TCP)</param>
        public ModbusTcpClient(ModbusProtocolFormat format = ModbusProtocolFormat.MBAP) : base(new TcpClient(), format) { }
    }

    public partial class ModbusTcpClient : ITcpClient
    {
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
