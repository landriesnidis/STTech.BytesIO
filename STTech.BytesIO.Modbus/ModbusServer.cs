using STTech.BytesIO.Core;
using System;

namespace STTech.BytesIO.Modbus
{
    /// <summary>
    /// Modbus 服务端基类 (从站/网关模拟器)
    /// <para>作为 Modbus 网络中的 Slave 角色，负责响应 Master 的请求，或者作为网关进行透明代理转发。</para>
    /// </summary>
    public abstract class ModbusServer
    {
        /// <summary>
        /// 是否正在运行
        /// </summary>
        public abstract bool IsRunning { get; }

        /// <summary>
        /// 从站站号 (默认: 1)。
        /// 仅当请求报文的站号与本配置匹配时，才触发本地处理逻辑；非本机站号的报文将通过透明代理原样转发至 DownstreamClient。
        /// </summary>
        public byte SlaveId { get; set; } = 1;

        /// <summary>
        /// 下游客户端 (用于非本机站号报文的透明透明代理)。
        /// 可挂接 SerialClient 或 TcpClient，配合 UseAutoReconnect 可实现下游网络自动重连。
        /// </summary>
        public BytesClient DownstreamClient
        {
            get => downstreamClient;
            set
            {
                if (downstreamClient != null)
                {
                    UnregisterDownstreamEvents(downstreamClient);
                }
                downstreamClient = value;
                if (downstreamClient != null)
                {
                    RegisterDownstreamEvents(downstreamClient);
                }
            }
        }
        private BytesClient downstreamClient;
        
        /// <summary>
        /// 下游节点列表 (站号 -> 客户端)
        /// 若配置了此列表，则优先根据站号寻找转发目标；若未找到，则退而求其次寻找默认的 DownstreamClient。
        /// </summary>
        public System.Collections.Generic.Dictionary<byte, BytesClient> DownstreamNodes { get; } = new System.Collections.Generic.Dictionary<byte, BytesClient>();

        /// <summary>
        /// 添加下游节点
        /// </summary>
        /// <param name="slaveId">从站站号</param>
        /// <param name="client">通信客户端</param>
        public void AddDownstreamNode(byte slaveId, BytesClient client)
        {
            if (client == null) return;
            if (DownstreamNodes.TryGetValue(slaveId, out var oldClient))
            {
                UnregisterDownstreamEvents(oldClient);
            }
            DownstreamNodes[slaveId] = client;
            RegisterDownstreamEvents(client);
        }

        /// <summary>
        /// 移除下游节点
        /// </summary>
        /// <param name="slaveId">从站站号</param>
        public void RemoveDownstreamNode(byte slaveId)
        {
            if (DownstreamNodes.TryGetValue(slaveId, out var client))
            {
                UnregisterDownstreamEvents(client);
                DownstreamNodes.Remove(slaveId);
            }
        }

        /// <summary>
        /// 是否已连接至下游节点
        /// </summary>
        public bool IsDownstreamConnected => DownstreamClient?.IsConnected ?? false;

        private void DownstreamClient_OnDataReceived(object sender, DataReceivedEventArgs e)
        {
            // 下游返回的数据直接原样抛回给上游
            BroadcastUpstream(e.Data.ToArray());
        }

        private void RegisterDownstreamEvents(BytesClient client)
        {
            if (client == null) return;
            client.OnDataReceived += DownstreamClient_OnDataReceived;
        }

        private void UnregisterDownstreamEvents(BytesClient client)
        {
            if (client == null) return;
            client.OnDataReceived -= DownstreamClient_OnDataReceived;
        }

        /// <summary>
        /// 向上游客户端广播数据
        /// </summary>
        /// <param name="data">需要回传的原始字节数组</param>
        protected abstract void BroadcastUpstream(byte[] data);
        /// <summary>
        /// 读取保持寄存器请求事件
        /// </summary>
        public EventHandler<ModbusReadRequestedEventArgs<byte[]>> ReadHoldingRegisterRequested;

        /// <summary>
        /// 读取输入寄存器请求事件
        /// </summary>
        public EventHandler<ModbusReadRequestedEventArgs<byte[]>> ReadInputRegisterRequested;

        /// <summary>
        /// 读取线圈寄存器请求事件
        /// </summary>
        public EventHandler<ModbusReadRequestedEventArgs<bool[]>> ReadCoilRegisterRequested;

        /// <summary>
        /// 读取离散输入寄存器请求事件
        /// </summary>
        public EventHandler<ModbusReadRequestedEventArgs<bool[]>> ReadDiscreteInputRegisterRequested;

        /// <summary>
        /// 写入单个保持寄存器请求事件
        /// </summary>
        public EventHandler<ModbusWriteRequestedEventArgs<WriteSingleHoldingRegisterRequest>> WriteSingleHoldingRegisterRequested;

        /// <summary>
        /// 写入多个保持寄存器请求事件
        /// </summary>
        public EventHandler<ModbusWriteRequestedEventArgs<WriteMultipleHoldingRegistersRequest>> WriteMultipleHoldingRegistersRequested;

        /// <summary>
        /// 写入单个线圈寄存器请求事件
        /// </summary>
        public EventHandler<ModbusWriteRequestedEventArgs<WriteSingleCoilRegisterRequest>> WriteSingleCoilRegisterRequested;

        /// <summary>
        /// 写入多个线圈寄存器请求事件
        /// </summary>
        public EventHandler<ModbusWriteRequestedEventArgs<WriteMultipleCoilRegistersRequest>> WriteMultipleCoilRegistersRequested;

        protected void HandleRequest(ModbusRequest request, BytesClient sender)
        {
            // 如果是非本机的转发请求，直接透明传递
            if (request is STTech.BytesIO.Modbus.Entity.ModbusForwardRequest forwardReq)
            {
                // 路由逻辑：
                // 1. 优先查找专属于该 SlaveId 的下游节点
                // 2. 若未找到，则尝试使用默认的全局下游节点
                BytesClient target = null;
                if (DownstreamNodes.TryGetValue(forwardReq.SlaveId, out var node))
                {
                    target = node;
                }
                else
                {
                    target = DownstreamClient;
                }

                if (target != null && target.IsConnected)
                {
                    target.SendAsync(forwardReq.RawBytes);
                }
                return;
            }

            try
            {
                ModbusResponse response = null;

                switch (request.FunctionCode)
                {
                    case FunctionCode.ReadHoldingRegister:
                        {
                            var req = (ReadRegisterRequest)request;
                            var args = new ModbusReadRequestedEventArgs<byte[]>(req, sender);
                            ReadHoldingRegisterRequested?.Invoke(this, args);
                            if (args.IsError) response = new ModbusResponse(req.SlaveId, req.FunctionCode, req.ProtocolFormat, args.ErrorCode);
                            else response = new ReadHoldingRegisterResponse(req.SlaveId, req.ProtocolFormat, args.ResponseData);
                            break;
                        }
                    case FunctionCode.ReadInputRegister:
                        {
                            var req = (ReadRegisterRequest)request;
                            var args = new ModbusReadRequestedEventArgs<byte[]>(req, sender);
                            ReadInputRegisterRequested?.Invoke(this, args);
                            if (args.IsError) response = new ModbusResponse(req.SlaveId, req.FunctionCode, req.ProtocolFormat, args.ErrorCode);
                            else response = new ReadInputRegisterResponse(req.SlaveId, req.ProtocolFormat, args.ResponseData);
                            break;
                        }
                    case FunctionCode.ReadCoilRegister:
                        {
                            var req = (ReadRegisterRequest)request;
                            var args = new ModbusReadRequestedEventArgs<bool[]>(req, sender);
                            ReadCoilRegisterRequested?.Invoke(this, args);
                            if (args.IsError) response = new ModbusResponse(req.SlaveId, req.FunctionCode, req.ProtocolFormat, args.ErrorCode);
                            else response = new ReadCoilRegisterResponse(req.SlaveId, req.ProtocolFormat, args.ResponseData);
                            break;
                        }
                    case FunctionCode.ReadDiscreteInputRegister:
                        {
                            var req = (ReadRegisterRequest)request;
                            var args = new ModbusReadRequestedEventArgs<bool[]>(req, sender);
                            ReadDiscreteInputRegisterRequested?.Invoke(this, args);
                            if (args.IsError) response = new ModbusResponse(req.SlaveId, req.FunctionCode, req.ProtocolFormat, args.ErrorCode);
                            else response = new ReadDiscreteInputRegisterResponse(req.SlaveId, req.ProtocolFormat, args.ResponseData);
                            break;
                        }
                    case FunctionCode.WriteSingleHoldingRegister:
                        {
                            var req = (WriteSingleHoldingRegisterRequest)request;
                            var args = new ModbusWriteRequestedEventArgs<WriteSingleHoldingRegisterRequest>(req, sender);
                            WriteSingleHoldingRegisterRequested?.Invoke(this, args);
                            if (args.IsError) response = new ModbusResponse(req.SlaveId, req.FunctionCode, req.ProtocolFormat, args.ErrorCode);
                            else response = new WriteRegisterResponse(req.SlaveId, req.FunctionCode, req.ProtocolFormat, req.WriteAddress, req.Data);
                            break;
                        }
                    case FunctionCode.WriteMultipleHoldingRegisters:
                        {
                            var req = (WriteMultipleHoldingRegistersRequest)request;
                            var args = new ModbusWriteRequestedEventArgs<WriteMultipleHoldingRegistersRequest>(req, sender);
                            WriteMultipleHoldingRegistersRequested?.Invoke(this, args);
                            if (args.IsError) response = new ModbusResponse(req.SlaveId, req.FunctionCode, req.ProtocolFormat, args.ErrorCode);
                            else
                            {
                                ushort quantity = (ushort)(req.Data.Length / 2);
                                byte[] values = new byte[] { (byte)(quantity >> 8), (byte)(quantity & 0xFF) };
                                response = new WriteRegisterResponse(req.SlaveId, req.FunctionCode, req.ProtocolFormat, req.WriteAddress, values);
                            }
                            break;
                        }
                    case FunctionCode.WriteSingleCoilRegister:
                        {
                            var req = (WriteSingleCoilRegisterRequest)request;
                            var args = new ModbusWriteRequestedEventArgs<WriteSingleCoilRegisterRequest>(req, sender);
                            WriteSingleCoilRegisterRequested?.Invoke(this, args);
                            if (args.IsError) response = new ModbusResponse(req.SlaveId, req.FunctionCode, req.ProtocolFormat, args.ErrorCode);
                            else response = new WriteRegisterResponse(req.SlaveId, req.FunctionCode, req.ProtocolFormat, req.WriteAddress, req.Data ? new byte[] { 0xFF, 0x00 } : new byte[] { 0x00, 0x00 });
                            break;
                        }
                    case FunctionCode.WriteMultipleCoilRegisters:
                        {
                            var req = (WriteMultipleCoilRegistersRequest)request;
                            var args = new ModbusWriteRequestedEventArgs<WriteMultipleCoilRegistersRequest>(req, sender);
                            WriteMultipleCoilRegistersRequested?.Invoke(this, args);
                            if (args.IsError) response = new ModbusResponse(req.SlaveId, req.FunctionCode, req.ProtocolFormat, args.ErrorCode);
                            else
                            {
                                ushort quantity = (ushort)req.Data.Length;
                                byte[] values = new byte[] { (byte)(quantity >> 8), (byte)(quantity & 0xFF) };
                                response = new WriteRegisterResponse(req.SlaveId, req.FunctionCode, req.ProtocolFormat, req.WriteAddress, values);
                            }
                            break;
                        }
                }

                if (response == null) 
                {
                    response = new ModbusResponse(request.SlaveId, request.FunctionCode, request.ProtocolFormat, ModbusErrorCode.IllegalFunction);
                }

                sender.SendAsync(response.GetBytes());
            }
            catch (Exception)
            {
                var errorResponse = new ModbusResponse(request.SlaveId, request.FunctionCode, request.ProtocolFormat, ModbusErrorCode.SlaveDeviceFailure);
                sender.SendAsync(errorResponse.GetBytes());
            }
        }
    }

    /// <summary>
    /// Modbus 读取请求事件参数
    /// </summary>
    /// <typeparam name="TResponseData">响应数据类型 (byte[] 或 bool[])</typeparam>
    public class ModbusReadRequestedEventArgs<TResponseData> : EventArgs
    {
        /// <summary>
        /// 原始请求信息
        /// </summary>
        public ReadRegisterRequest Request { get; }

        /// <summary>
        /// 发送方客户端
        /// </summary>
        public BytesClient Client { get; }

        /// <summary>
        /// 准备返回的响应数据
        /// </summary>
        public TResponseData ResponseData { get; set; }

        /// <summary>
        /// 是否发生错误
        /// </summary>
        public bool IsError { get; set; }

        /// <summary>
        /// 错误码 (默认为非法数据地址)
        /// </summary>
        public ModbusErrorCode ErrorCode { get; set; } = ModbusErrorCode.IllegalDataAddress;

        /// <summary>
        /// 构造读取请求事件参数
        /// </summary>
        /// <param name="request">请求实体</param>
        /// <param name="client">发送方</param>
        public ModbusReadRequestedEventArgs(ReadRegisterRequest request, BytesClient client)
        {
            Request = request;
            Client = client;
        }
    }

    /// <summary>
    /// Modbus 写入请求事件参数
    /// </summary>
    /// <typeparam name="TRequest">请求实体类型</typeparam>
    public class ModbusWriteRequestedEventArgs<TRequest> : EventArgs where TRequest : ModbusRequest
    {
        /// <summary>
        /// 原始请求信息
        /// </summary>
        public TRequest Request { get; }

        /// <summary>
        /// 发送方客户端
        /// </summary>
        public BytesClient Client { get; }

        /// <summary>
        /// 是否发生错误
        /// </summary>
        public bool IsError { get; set; }

        /// <summary>
        /// 错误码 (默认为非法数据值)
        /// </summary>
        public ModbusErrorCode ErrorCode { get; set; } = ModbusErrorCode.IllegalDataValue;

        /// <summary>
        /// 构造写入请求事件参数
        /// </summary>
        /// <param name="request">请求实体</param>
        /// <param name="client">发送方</param>
        public ModbusWriteRequestedEventArgs(TRequest request, BytesClient client)
        {
            Request = request;
            Client = client;
        }
    }
}
