using STTech.BytesIO.Core;
using STTech.BytesIO.Core.Component;
using STTech.BytesIO.Tcp;
using System.Net;

namespace STTech.BytesIO.Modbus
{
    public partial class ModbusTcpClient : ModbusClient<TcpClient>, IModbusClient
    {

    }

    public partial class ModbusTcpClient : ITcpClient
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
