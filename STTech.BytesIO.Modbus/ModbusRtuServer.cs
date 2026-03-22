using STTech.BytesIO.Core;
using STTech.BytesIO.Core.Component;
using STTech.BytesIO.Serial;
using System;

namespace STTech.BytesIO.Modbus
{
    /// <summary>
    /// Modbus RTU/ASCII 服务端 (串口从站模拟器/网关)
    /// <para>在串口网络中作为 Slave 角色，响应主站 (Master) 的请求。</para>
    /// </summary>
    public class ModbusRtuServer : ModbusServer, IDisposable
    {
        /// <summary>
        /// 内部串口客户端
        /// </summary>
        public SerialClient InnerClient { get; }

        /// <inheritdoc/>
        public override bool IsRunning => InnerClient.IsConnected;

        /// <summary>
        /// 协议格式
        /// </summary>
        public ModbusProtocolFormat ProtocolFormat { get; }

        /// <summary>
        /// 串口号
        /// </summary>
        public string PortName { get => InnerClient.PortName; set => InnerClient.PortName = value; }
        
        /// <summary>
        /// 波特率
        /// </summary>
        public int BaudRate { get => InnerClient.BaudRate; set => InnerClient.BaudRate = value; }

        private ModbusRequestUnpacker unpacker;

        /// <summary>
        /// 构造 Modbus RTU/ASCII 服务端
        /// </summary>
        /// <param name="format">协议格式 (默认为 RTU)</param>
        public ModbusRtuServer(ModbusProtocolFormat format = ModbusProtocolFormat.RTU)
        {
            ProtocolFormat = format;
            InnerClient = new SerialClient();
            InnerClient.OnConnectedSuccessfully += InnerClient_OnConnectedSuccessfully;
            InnerClient.OnDisconnected += InnerClient_OnDisconnected;
        }

        private void InnerClient_OnConnectedSuccessfully(object sender, ConnectedSuccessfullyEventArgs e)
        {
            unpacker = new ModbusRequestUnpacker(InnerClient, ProtocolFormat)
            {
                IsLocalSlaveId = (id) => id == this.SlaveId
            };
            InnerClient.BindUnpacker(unpacker);
            unpacker.OnDataParsed += Unpacker_OnDataParsed;
        }

        private void InnerClient_OnDisconnected(object sender, DisconnectedEventArgs e)
        {
            if (unpacker != null)
            {
                unpacker.OnDataParsed -= Unpacker_OnDataParsed;
                unpacker = null;
            }
        }

        private void Unpacker_OnDataParsed(object sender, DataParsedEventArgs<ModbusRequest> e)
        {
            HandleRequest(e.Data, InnerClient);
        }

        /// <inheritdoc/>
        protected override void BroadcastUpstream(byte[] data)
        {
            if (InnerClient.IsConnected)
            {
                InnerClient.SendAsync(data);
            }
        }

        /// <summary>
        /// 启动服务端 (打开串口)
        /// </summary>
        public void Start()
        {
            InnerClient.Connect();
        }

        /// <summary>
        /// 停止服务端 (关闭串口)
        /// </summary>
        public void Stop()
        {
            InnerClient.Disconnect();
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            InnerClient?.Dispose();
        }
    }
}
