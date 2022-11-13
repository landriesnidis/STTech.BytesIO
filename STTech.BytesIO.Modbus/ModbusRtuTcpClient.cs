using STTech.BytesIO.Core;
using STTech.BytesIO.Core.Component;
using STTech.BytesIO.Tcp;
using System.Net;

namespace STTech.BytesIO.Modbus
{
    /// <summary>
    /// Modbus TCP 客户端
    /// </summary>
    public partial class ModbusRtuTcpClient : ModbusClient, IModbusClient
    {
        public new TcpClient InnerClient { get; }
        public TcpClient GetInnerClient() => InnerClient;
        public ModbusRtuTcpClient() : base(new TcpClient())
        {
            InnerClient = (TcpClient)base.InnerClient;
        }
    }

    public partial class ModbusRtuTcpClient : ITcpClient
    {
        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public string Host { get => InnerClient.Host; set => InnerClient.Host = value; }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public int Port { get => InnerClient.Port; set => InnerClient.Port = value; }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public int LocalPort => InnerClient.LocalPort;

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public IPEndPoint RemoteEndPoint => InnerClient.RemoteEndPoint;
    }
}
