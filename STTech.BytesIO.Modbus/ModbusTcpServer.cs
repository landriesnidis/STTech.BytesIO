using STTech.BytesIO.Core;
using STTech.BytesIO.Tcp;
using STTech.BytesIO.Core.Component;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace STTech.BytesIO.Modbus
{
    /// <summary>
    /// Modbus TCP 服务端 (从站模拟器/网关)
    /// <para>在网络中作为 Slave 角色，监听主站 (Master) 的连接并响应其请求。</para>
    /// </summary>
    public class ModbusTcpServer : ModbusServer, IDisposable
    {
        /// <summary>
        /// 内部 TCP 服务端
        /// </summary>
        public TcpServer InnerServer { get; }

        /// <inheritdoc/>
        public override bool IsRunning => InnerServer.IsRunning;

        /// <summary>
        /// 协议格式
        /// </summary>
        public ModbusProtocolFormat ProtocolFormat { get; }

        /// <summary>
        /// 监听端口
        /// </summary>
        public int Port { get => InnerServer.Port; set => InnerServer.Port = value; }

        /// <summary>
        /// 已连接的客户端列表
        /// </summary>
        public IEnumerable<TcpClient> Clients => InnerServer.Clients;

        private ConcurrentDictionary<TcpClient, ModbusRequestUnpacker> unpackers = new ConcurrentDictionary<TcpClient, ModbusRequestUnpacker>();

        /// <summary>
        /// 客户端连接事件
        /// </summary>
        public EventHandler<ClientConnectedEventArgs> ClientConnected;

        /// <summary>
        /// 客户端断开事件
        /// </summary>
        public EventHandler<ClientDisconnectedEventArgs> ClientDisconnected;

        /// <summary>
        /// 构造 Modbus TCP 服务端
        /// </summary>
        /// <param name="format">协议格式 (默认 RTU，通常 ModbusTCP 内部也使用 RTU 结构的 Payload)</param>
        public ModbusTcpServer(ModbusProtocolFormat format = ModbusProtocolFormat.RTU)
        {
            ProtocolFormat = format;
            InnerServer = new TcpServer();
            InnerServer.ClientConnected += InnerServer_ClientConnected;
            InnerServer.ClientDisconnected += InnerServer_ClientDisconnected;
        }

        private void InnerServer_ClientDisconnected(object sender, ClientDisconnectedEventArgs e)
        {
            if (unpackers.TryRemove((TcpClient)e.Client, out var unpacker))
            {
                unpacker.OnDataParsed -= Unpacker_OnDataParsed;
            }
            ClientDisconnected?.Invoke(this, e);
        }

        private void InnerServer_ClientConnected(object sender, ClientConnectedEventArgs e)
        {
            // 在绑定解包器前，先给用户一个注入点（或者用户直接在 ClientConnected 里监听 OnDataReceived）
            ClientConnected?.Invoke(this, e);

            var unpacker = new ModbusRequestUnpacker((TcpClient)e.Client, ProtocolFormat)
            {
                IsLocalSlaveId = (id) => id == this.SlaveId
            };
            ((TcpClient)e.Client).BindUnpacker(unpacker);
            unpacker.OnDataParsed += Unpacker_OnDataParsed;
            unpackers.TryAdd((TcpClient)e.Client, unpacker);
        }

        private void Unpacker_OnDataParsed(object sender, DataParsedEventArgs<ModbusRequest> e)
        {
            var unpacker = (ModbusRequestUnpacker)sender;
            HandleRequest(e.Data, unpacker.Client);
        }

        /// <inheritdoc/>
        protected override void BroadcastUpstream(byte[] data)
        {
            foreach (var client in Clients)
            {
                client.SendAsync(data);
            }
        }

        /// <summary>
        /// 启动服务端
        /// </summary>
        public void Start()
        {
            InnerServer.StartAsync();
        }

        /// <summary>
        /// 停止服务端
        /// </summary>
        public void Stop()
        {
            InnerServer.StopAsync();
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            InnerServer?.Dispose();
        }
    }
}
