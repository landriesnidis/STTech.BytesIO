using STTech.BytesIO.Core;
using STTech.BytesIO.Core.Component;
using STTech.BytesIO.Serial;
using System.IO;
using System.IO.Ports;

namespace STTech.BytesIO.Modbus
{
    /// <summary>
    /// Modbus RTU 客户端
    /// </summary>
    public partial class ModbusRtuClient : ModbusClient, IModbusClient
    {
        public new SerialClient InnerClient { get; }
        public ModbusRtuClient() : base(new SerialClient())
        {

        }
    }

    public partial class ModbusRtuClient : ISerialClient
    {
        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public Handshake Handshake { get => InnerClient.Handshake; set => InnerClient.Handshake = value; }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public bool DtrEnable { get => InnerClient.DiscardNull; set => InnerClient.DiscardNull = value; }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public bool CtsHolding => InnerClient.CtsHolding;

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public bool DiscardNull { get => InnerClient.DiscardNull; set => InnerClient.DiscardNull = value; }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public int DataBits { get => InnerClient.DataBits; set => InnerClient.DataBits = value; }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public bool DsrHolding => InnerClient.DsrHolding;

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public string NewLine { get => InnerClient.NewLine; set => InnerClient.NewLine = value; }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public override int ReceiveBufferSize { get => InnerClient.ReceiveBufferSize; set => InnerClient.ReceiveBufferSize = value; }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public byte ParityReplace { get => InnerClient.ParityReplace; set => InnerClient.ParityReplace = value; }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public string PortName { get => InnerClient.PortName; set => InnerClient.PortName = value; }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public bool CDHolding => InnerClient.CDHolding;

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public int ReadTimeout { get => InnerClient.ReadTimeout; set => InnerClient.ReadTimeout = value; }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public int ReceivedBytesThreshold { get; set; }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public bool RtsEnable { get => InnerClient.RtsEnable; set => InnerClient.RtsEnable = value; }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public StopBits StopBits { get => InnerClient.StopBits; set => InnerClient.StopBits = value; }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public override int SendBufferSize { get => InnerClient.SendBufferSize; set => InnerClient.SendBufferSize = value; }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public int WriteTimeout { get => InnerClient.WriteTimeout; set => InnerClient.WriteTimeout = value; }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public Parity Parity { get => InnerClient.Parity; set => InnerClient.Parity = value; }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public int BytesToWrite => InnerClient.BytesToWrite;

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public int BaudRate { get => InnerClient.BaudRate; set => InnerClient.BaudRate = value; }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public bool BreakState { get => InnerClient.BreakState; set => InnerClient.BreakState = value; }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public Stream BaseStream => InnerClient.BaseStream;

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public int BytesToRead => InnerClient.BytesToRead;

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public void DiscardInBuffer()
        {
            InnerClient.DiscardInBuffer();
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public void DiscardOutBuffer()
        {
            InnerClient.DiscardOutBuffer();
        }
    }
}
