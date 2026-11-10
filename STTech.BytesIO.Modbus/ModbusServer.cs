using STTech.BytesIO.Core;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace STTech.BytesIO.Modbus
{
    /// <summary>
    /// Modbus 服务端 (从站模拟器/网关)
    /// </summary>
    public abstract class ModbusServer
    {
        /// <summary>
        /// 服务端运行状态
        /// </summary>
        public abstract bool IsRunning { get; }

        /// <summary>
        /// 本机绑定的 Modbus 从站站号 (Slave ID)
        /// 默认为 1。若接收到的请求站号与此不符，且配置了下游节点，则会自动转发。
        /// </summary>
        public byte SlaveId { get; set; } = 1;

        /// <summary>
        /// 默认下游客户端 (串口或远端从站连接)
        /// 若配置了此项，所有非本机站号的请求都将转发至此客户端
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
        public ConcurrentDictionary<byte, BytesClient> DownstreamNodes { get; } = new ConcurrentDictionary<byte, BytesClient>();

        /// <summary>
        /// 添加下游节点
        /// </summary>
        /// <param name="slaveId">从站站号</param>
        /// <param name="client">通信客户端</param>
        public void AddDownstreamNode(byte slaveId, BytesClient client)
        {
            if (client == null) return;
            DownstreamNodes.AddOrUpdate(slaveId, 
                addValueFactory: id =>
                {
                    RegisterDownstreamEvents(client);
                    return client;
                },
                updateValueFactory: (id, oldClient) =>
                {
                    if (oldClient != client)
                    {
                        UnregisterDownstreamEvents(oldClient);
                        RegisterDownstreamEvents(client);
                    }
                    return client;
                });
        }

        /// <summary>
        /// 移除下游节点
        /// </summary>
        /// <param name="slaveId">从站站号</param>
        public void RemoveDownstreamNode(byte slaveId)
        {
            if (DownstreamNodes.TryRemove(slaveId, out var client))
            {
                UnregisterDownstreamEvents(client);
            }
        }

        /// <summary>
        /// 是否已连接至下游节点
        /// </summary>
        public bool IsDownstreamConnected => DownstreamClient?.IsConnected ?? false;

        private class ForwardSession
        {
            public BytesClient SourceClient { get; set; }
            public ushort TransactionId { get; set; }
            public ushort ProtocolId { get; set; }
            public ModbusProtocolFormat SourceFormat { get; set; }
            public DateTime Timestamp { get; set; } = DateTime.Now;
        }

        private readonly ConcurrentDictionary<byte, ForwardSession> _forwardSessions = new ConcurrentDictionary<byte, ForwardSession>();

        private void DownstreamClient_OnDataReceived(object sender, DataReceivedEventArgs e)
        {
            byte[] raw = e.Data.ToArray();
            if (raw == null || raw.Length == 0) return;

            // 优先检查从站站号路由 (针对 RTU 报文首字节为 SlaveId)
            byte slaveId = raw[0];
            if (_forwardSessions.TryRemove(slaveId, out var session) && session.SourceClient != null && session.SourceClient.IsConnected)
            {
                if (session.SourceFormat == ModbusProtocolFormat.MBAP && raw.Length >= 3)
                {
                    // 若下游返回的是 RTU 帧，将其转译为上游客户端期望的 MBAP 帧 (剥除末尾 2 字节 CRC，头部拼接 MBAP 帧头)
                    int pduAndUnitLen = raw.Length >= 4 ? raw.Length - 2 : raw.Length;
                    byte[] mbapBytes = new byte[6 + pduAndUnitLen];
                    System.Buffers.Binary.BinaryPrimitives.WriteUInt16BigEndian(mbapBytes.AsSpan(0, 2), session.TransactionId);
                    System.Buffers.Binary.BinaryPrimitives.WriteUInt16BigEndian(mbapBytes.AsSpan(2, 2), session.ProtocolId);
                    System.Buffers.Binary.BinaryPrimitives.WriteUInt16BigEndian(mbapBytes.AsSpan(4, 2), (ushort)pduAndUnitLen);
                    Buffer.BlockCopy(raw, 0, mbapBytes, 6, pduAndUnitLen);

                    session.SourceClient.SendAsync(mbapBytes);
                    return;
                }
                else
                {
                    // 同构协议或上游非 MBAP，直接定向返回给来源客户端
                    session.SourceClient.SendAsync(raw);
                    return;
                }
            }

            // 无会话匹配时，退化为全局广播兜底
            BroadcastUpstream(raw);
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
        /// 将下游返回的数据广播回所有上游客户端
        /// </summary>
        /// <param name="data">返回的原始报文</param>
        protected abstract void BroadcastUpstream(byte[] data);

        #region Events
        /// <summary>
        /// 请求读取保持寄存器时触发的事件
        /// </summary>
        public event EventHandler<ModbusReadRequestedEventArgs<byte[]>> ReadHoldingRegisterRequested;
        /// <summary>
        /// 请求读取输入寄存器时触发的事件
        /// </summary>
        public event EventHandler<ModbusReadRequestedEventArgs<byte[]>> ReadInputRegisterRequested;
        /// <summary>
        /// 请求读取线圈寄存器时触发的事件
        /// </summary>
        public event EventHandler<ModbusReadRequestedEventArgs<bool[]>> ReadCoilRegisterRequested;
        /// <summary>
        /// 请求读取离散输入寄存器时触发的事件
        /// </summary>
        public event EventHandler<ModbusReadRequestedEventArgs<bool[]>> ReadDiscreteInputRegisterRequested;

        /// <summary>
        /// 请求写单个保持寄存器时触发的事件
        /// </summary>
        public event EventHandler<ModbusWriteRequestedEventArgs<WriteSingleHoldingRegisterRequest>> WriteSingleHoldingRegisterRequested;
        /// <summary>
        /// 请求写多个保持寄存器时触发的事件
        /// </summary>
        public event EventHandler<ModbusWriteRequestedEventArgs<WriteMultipleHoldingRegistersRequest>> WriteMultipleHoldingRegistersRequested;
        /// <summary>
        /// 请求写单个线圈时触发的事件
        /// </summary>
        public event EventHandler<ModbusWriteRequestedEventArgs<WriteSingleCoilRegisterRequest>> WriteSingleCoilRegisterRequested;
        /// <summary>
        /// 请求写多个线圈时触发的事件
        /// </summary>
        public event EventHandler<ModbusWriteRequestedEventArgs<WriteMultipleCoilRegistersRequest>> WriteMultipleCoilRegistersRequested;
        #endregion

        /// <summary>
        /// 处理接收到的 Modbus 请求
        /// </summary>
        /// <param name="request">Modbus 请求实体</param>
        /// <param name="sender">发送该请求的通信客户端</param>
        protected void HandleRequest(ModbusRequest request, BytesClient sender)
        {
            if (request == null || sender == null) return;

            // 如果是非本机的转发请求，进行协议转译与定向会话路由
            if (request is STTech.BytesIO.Modbus.Entity.ModbusForwardRequest forwardReq)
            {
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
                    _forwardSessions[forwardReq.SlaveId] = new ForwardSession
                    {
                        SourceClient = sender,
                        TransactionId = forwardReq.TransactionId,
                        ProtocolId = forwardReq.ProtocolId,
                        SourceFormat = forwardReq.ProtocolFormat
                    };

                    byte[] dataToSend = forwardReq.RawBytes;

                    // 若上游请求为 MBAP，但下游通常为串口 RTU 设备，需自动完成协议转译 (去除 6 字节 MBAP 头并追加 CRC16)
                    if (forwardReq.ProtocolFormat == ModbusProtocolFormat.MBAP && forwardReq.RawBytes.Length >= 8)
                    {
                        int pduAndUnitLen = forwardReq.RawBytes.Length - 6;
                        byte[] rtuBytes = new byte[pduAndUnitLen + 2];
                        Buffer.BlockCopy(forwardReq.RawBytes, 6, rtuBytes, 0, pduAndUnitLen);
                        var crc = ModbusCrc16.Compute(rtuBytes.AsSpan(0, pduAndUnitLen));
                        rtuBytes[pduAndUnitLen] = (byte)(crc & 0xFF);
                        rtuBytes[pduAndUnitLen + 1] = (byte)(crc >> 8);
                        dataToSend = rtuBytes;
                    }

                    target.SendAsync(dataToSend);
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

                // Modbus 标准规范：对于广播写请求 (SlaveId == 0)，从站执行操作但绝不返回任何应答，以防物理总线冲突
                if (request.SlaveId == 0)
                {
                    return;
                }

                if (response == null) 
                {
                    response = new ModbusResponse(request.SlaveId, request.FunctionCode, request.ProtocolFormat, ModbusErrorCode.IllegalFunction);
                }

                // 复制请求的 TransactionId 和 ProtocolId (关键：用于 MBAP 全双工应答匹配)
                response.TransactionId = request.TransactionId;
                response.ProtocolId = request.ProtocolId;

                sender.SendAsync(response.GetBytes());
            }
            catch (Exception)
            {
                if (request.SlaveId == 0) return;

                var errorResponse = new ModbusResponse(request.SlaveId, request.FunctionCode, request.ProtocolFormat, ModbusErrorCode.SlaveDeviceFailure)
                {
                    TransactionId = request.TransactionId,
                    ProtocolId = request.ProtocolId
                };
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
        /// 发起请求的客户端
        /// </summary>
        public BytesClient Client { get; }

        /// <summary>
        /// 要返回的响应数据
        /// </summary>
        public TResponseData ResponseData { get; set; }

        /// <summary>
        /// 是否返回错误响应
        /// </summary>
        public bool IsError { get; set; } = false;

        /// <summary>
        /// 错误码
        /// </summary>
        public ModbusErrorCode ErrorCode { get; set; } = ModbusErrorCode.NoError;

        /// <summary>
        /// 初始化 <see cref="ModbusReadRequestedEventArgs{TResponseData}"/> 类的新实例。
        /// </summary>
        /// <param name="request">读取请求实体</param>
        /// <param name="client">通信客户端</param>
        public ModbusReadRequestedEventArgs(ReadRegisterRequest request, BytesClient client)
        {
            Request = request;
            Client = client;
        }
    }

    /// <summary>
    /// Modbus 写入请求事件参数
    /// </summary>
    /// <typeparam name="TRequest">写入请求类型</typeparam>
    public class ModbusWriteRequestedEventArgs<TRequest> : EventArgs where TRequest : ModbusRequest
    {
        /// <summary>
        /// 原始写入请求信息
        /// </summary>
        public TRequest Request { get; }

        /// <summary>
        /// 发起请求的客户端
        /// </summary>
        public BytesClient Client { get; }

        /// <summary>
        /// 是否返回错误响应
        /// </summary>
        public bool IsError { get; set; } = false;

        /// <summary>
        /// 错误码
        /// </summary>
        public ModbusErrorCode ErrorCode { get; set; } = ModbusErrorCode.NoError;

        /// <summary>
        /// 初始化 <see cref="ModbusWriteRequestedEventArgs{TRequest}"/> 类的新实例。
        /// </summary>
        /// <param name="request">写入请求实体</param>
        /// <param name="client">通信客户端</param>
        public ModbusWriteRequestedEventArgs(TRequest request, BytesClient client)
        {
            Request = request;
            Client = client;
        }
    }
}
