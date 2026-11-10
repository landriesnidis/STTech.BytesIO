using STTech.BytesIO.Core;
using STTech.BytesIO.Core.Component;
using System;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace STTech.BytesIO.Modbus
{
    /// <summary>
    /// Modbus 客户端 (主站)
    /// <para>在网络中作为 Master 角色，负责向从站 (Slave) 发起读写请求。</para>
    /// </summary>
    public abstract partial class ModbusClient<TClient> : ModbusClient where TClient : BytesClient
    {
        /// <summary>
        /// 内部客户端
        /// </summary>
        public new TClient InnerClient => (TClient)base.InnerClient;

        /// <summary>
        /// 构造 Modbus 客户端
        /// </summary>
        /// <param name="client">内部客户端</param>
        /// <param name="format">协议格式</param>
        public ModbusClient(TClient client, ModbusProtocolFormat format) : base(client, format)
        {
        }
    }

    /// <summary>
    /// Modbus 客户端 (主站)基类
    /// </summary>
    public abstract partial class ModbusClient : VirtualClient
    {
        private ModbusProtocolFormat protocolFormat;
        private readonly SemaphoreSlim _channelLock = new SemaphoreSlim(1, 1);
        private int _transactionIdCounter = 0;

        /// <summary>
        /// 默认工业端序格式 (默认为大端模式 ABCD，适用于西门子 PLC 等)
        /// </summary>
        public EndianFormat DefaultEndianFormat { get; set; } = EndianFormat.ABCD;

        /// <summary>
        /// 是否允许 MBAP 模式下的无锁并发流水线 (Pipelining)。
        /// <para>
        /// 默认为 <c>false</c>。在工业现场，绝大多数真实 PLC（如西门子 S7-200 SMART/S7-1200、施耐德 M221、汇川及各种串口网关）
        /// 的 Modbus TCP 协议栈均为单任务串行处理。如果上位机并发下发请求，PLC 会返回 0x06 (Slave Device Busy) 或直接丢弃闪断。
        /// 因此默认启用请求-响应串行互斥保护。
        /// 仅当连接高性能高并发软件从站或支持多任务并发握手的设备时，可明确置为 <c>true</c> 以开启无锁并发流水线。
        /// </para>
        /// </summary>
        public bool AllowPipelining { get; set; } = false;

        /// <summary>
        /// 生成下一个 MBAP 事务元标识符 (原子自增)
        /// </summary>
        public ushort GetNextTransactionId()
        {
            return (ushort)(Interlocked.Increment(ref _transactionIdCounter) & 0xFFFF);
        }

        /// <inheritdoc/>
        public Unpacker<ModbusResponse> Unpacker { get; }

        /// <summary>
        /// Modbus协议格式
        /// </summary>
        public ModbusProtocolFormat ProtocolFormat
        {
            get => protocolFormat;
            set
            {
                protocolFormat = value;
                if (Unpacker is ModbusUnpacker unpacker)
                {
                    unpacker.Format = value;
                    unpacker.StartMark = value == ModbusProtocolFormat.ASCII ? new byte[] { (byte)':' } : null;
                }
            }
        }

        /// <summary>
        /// 构造 Modbus 客户端
        /// </summary>
        /// <param name="client">底层的通信客户端</param>
        /// <param name="format">协议格式</param>
        protected ModbusClient(BytesClient client, ModbusProtocolFormat format) : base(client)
        {
            protocolFormat = format;
            Unpacker = new ModbusUnpacker(this, format);
            this.BindUnpacker(Unpacker);
            Unpacker.OnDataParsed += Unpacker_OnDataParsed;
        }

        /// <inheritdoc/>
        public override void Dispose()
        {
            _channelLock?.Dispose();
            base.Dispose();
        }
    }

    public abstract partial class ModbusClient : IUnpackerSupport<ModbusResponse>
    {
        /// <summary>
        /// 发送Modbus请求
        /// </summary>
        /// <typeparam name="T">Modbus响应类型</typeparam>
        /// <param name="request">Modbus请求实体</param>
        /// <param name="timeout">超时时长(ms)</param>
        /// <param name="options">发送可选参数</param>
        /// <returns></returns>
        public Reply<T> SendModbusRequest<T>(ModbusRequest request, int timeout = 3000, SendOptions options = null) where T : ModbusResponse
        {
            request.ProtocolFormat = ProtocolFormat;

            // 1. 广播地址检查 (SlaveId == 0)
            if (request.SlaveId == 0)
            {
                // 读请求严禁广播
                if (request is ReadRegisterRequest || (byte)request.FunctionCode <= 0x04)
                {
                    throw new ArgumentException("Modbus 规范严禁向广播地址 (SlaveId = 0) 发起读操作。", nameof(request));
                }

                // 写请求：全网从站同时执行但严禁返回任何应答。必须获取信道锁保护物理信道避免踩踏！
                if (!_channelLock.Wait(timeout))
                {
                    return new Reply<T>(this, ReplyStatus.Timeout, null);
                }

                try
                {
                    this.Send(request.GetBytes(), options);
                    // 广播写入后从站需要处理时间，总线增加静默延迟
                    System.Threading.Thread.Sleep(10);
                    return new Reply<T>(this, null);
                }
                finally
                {
                    _channelLock.Release();
                }
            }

            // 2. 差异化信道调度
            if (ProtocolFormat == ModbusProtocolFormat.MBAP && AllowPipelining)
            {
                // MBAP 流水线模式：分配事务元 ID，支持全双工高并发多路复用无锁发送
                request.TransactionId = GetNextTransactionId();
                var reply = this.Send(request, timeout, ReplyMatchHandle, options);
                return reply.CastTo<T>();
            }
            else
            {
                // 工业级默认单问单答串行排队保护 (适用于 RTU / ASCII 以及工业现场真实 PLC 的 MBAP)
                if (ProtocolFormat == ModbusProtocolFormat.MBAP)
                {
                    request.TransactionId = GetNextTransactionId();
                }

                var sw = System.Diagnostics.Stopwatch.StartNew();
                if (!_channelLock.Wait(timeout))
                {
                    return new Reply<T>(this, ReplyStatus.Timeout, null);
                }

                int remainingTimeout = timeout - (int)sw.ElapsedMilliseconds;
                if (remainingTimeout <= 0)
                {
                    _channelLock.Release();
                    return new Reply<T>(this, ReplyStatus.Timeout, null);
                }

                try
                {
                    var reply = this.Send(request, remainingTimeout, ReplyMatchHandle, options);
                    if (reply.IsTimeout && ProtocolFormat != ModbusProtocolFormat.MBAP)
                    {
                        // 半双工 RTU/ASCII 超时保护：短暂静默隔离，避免迟到报文与后续请求总线踩踏
                        System.Threading.Thread.Sleep(50);
                    }
                    return reply.CastTo<T>();
                }
                finally
                {
                    _channelLock.Release();
                }
            }
        }

        /// <summary>
        /// 异步发送Modbus请求
        /// </summary>
        /// <typeparam name="T">Modbus响应类型</typeparam>
        /// <param name="request">Modbus请求实体</param>
        /// <param name="timeout">超时时长(ms)</param>
        /// <param name="options">发送可选参数</param>
        /// <returns></returns>
        public async Task<Reply<T>> SendModbusRequestAsync<T>(ModbusRequest request, int timeout = 3000, SendOptions options = null) where T : ModbusResponse
        {
            request.ProtocolFormat = ProtocolFormat;

            // 1. 广播地址检查 (SlaveId == 0)
            if (request.SlaveId == 0)
            {
                if (request is ReadRegisterRequest || (byte)request.FunctionCode <= 0x04)
                {
                    throw new ArgumentException("Modbus 规范严禁向广播地址 (SlaveId = 0) 发起读操作。", nameof(request));
                }

                if (!await _channelLock.WaitAsync(timeout).ConfigureAwait(false))
                {
                    return new Reply<T>(this, ReplyStatus.Timeout, null);
                }

                try
                {
                    await this.SendAsync(request.GetBytes(), options).ConfigureAwait(false);
                    await Task.Delay(10).ConfigureAwait(false);
                    return new Reply<T>(this, null);
                }
                finally
                {
                    _channelLock.Release();
                }
            }

            // 2. 差异化信道调度
            if (ProtocolFormat == ModbusProtocolFormat.MBAP && AllowPipelining)
            {
                request.TransactionId = GetNextTransactionId();
                var reply = await this.SendAsync(request, timeout, ReplyMatchHandle, options).ConfigureAwait(false);
                return reply.CastTo<T>();
            }
            else
            {
                if (ProtocolFormat == ModbusProtocolFormat.MBAP)
                {
                    request.TransactionId = GetNextTransactionId();
                }

                var sw = System.Diagnostics.Stopwatch.StartNew();
                if (!await _channelLock.WaitAsync(timeout).ConfigureAwait(false))
                {
                    return new Reply<T>(this, ReplyStatus.Timeout, null);
                }

                int remainingTimeout = timeout - (int)sw.ElapsedMilliseconds;
                if (remainingTimeout <= 0)
                {
                    _channelLock.Release();
                    return new Reply<T>(this, ReplyStatus.Timeout, null);
                }

                try
                {
                    var reply = await this.SendAsync(request, remainingTimeout, ReplyMatchHandle, options).ConfigureAwait(false);
                    if (reply.IsTimeout && ProtocolFormat != ModbusProtocolFormat.MBAP)
                    {
                        await Task.Delay(50).ConfigureAwait(false);
                    }
                    return reply.CastTo<T>();
                }
                finally
                {
                    _channelLock.Release();
                }
            }
        }

        #region ReadCoilRegister

        /// <summary>
        /// 读线圈寄存器
        /// </summary>
        /// <param name="request">读线圈寄存器的请求实体</param>
        /// <param name="timeout">超时时长(ms)</param>
        /// <param name="options">发送可选参数</param>
        /// <returns></returns>
        public Reply<ReadCoilRegisterResponse> ReadCoilRegister(ReadCoilRegisterRequest request, int timeout = 3000, SendOptions options = null)
        {
            var reply = SendModbusRequest<ReadCoilRegisterResponse>(request, timeout, options);
            return reply;
        }

        /// <summary>
        /// 读线圈寄存器
        /// </summary>
        /// <param name="slaveId">从机地址</param>
        /// <param name="startAddress">起始地址</param>
        /// <param name="length">读取长度</param>
        /// <param name="timeout">超时时长(ms)</param>
        /// <param name="options">发送可选参数</param>
        /// <returns></returns>
        public Reply<ReadCoilRegisterResponse> ReadCoilRegister(byte slaveId, ushort startAddress, ushort length, int timeout = 3000, SendOptions options = null)
        {
            return this.ReadCoilRegister(new ReadCoilRegisterRequest() { SlaveId = slaveId, StartAddress = startAddress, Length = length, ProtocolFormat = ProtocolFormat }, timeout, options);
        }

        #endregion

        #region ReadDiscreteInputRegister

        /// <summary>
        /// 读离散输入寄存器
        /// </summary>
        /// <param name="request">读离散输入寄存器的请求实体</param>
        /// <param name="timeout">超时时长(ms)</param>
        /// <param name="options">发送可选参数</param>
        /// <returns></returns>
        public Reply<ReadDiscreteInputRegisterResponse> ReadDiscreteInputRegister(ReadDiscreteInputRegisterRequest request, int timeout = 3000, SendOptions options = null)
        {
            var reply = SendModbusRequest<ReadDiscreteInputRegisterResponse>(request, timeout, options);
            return reply;
        }

        /// <summary>
        /// 读离散输入寄存器
        /// </summary>
        /// <param name="slaveId">从机地址</param>
        /// <param name="startAddress">起始地址</param>
        /// <param name="length">读取长度</param>
        /// <param name="timeout">超时时长(ms)</param>
        /// <param name="options">发送可选参数</param>
        /// <returns></returns>
        public Reply<ReadDiscreteInputRegisterResponse> ReadDiscreteInputRegister(byte slaveId, ushort startAddress, ushort length, int timeout = 3000, SendOptions options = null)
        {
            return this.ReadDiscreteInputRegister(new ReadDiscreteInputRegisterRequest() { SlaveId = slaveId, StartAddress = startAddress, Length = length, ProtocolFormat = ProtocolFormat }, timeout, options);
        }

        #endregion

        #region ReadHoldingRegister

        /// <summary>
        /// 读保持寄存器
        /// </summary>
        /// <param name="request">读保持寄存器的请求实体</param>
        /// <param name="timeout">超时时长(ms)</param>
        /// <param name="options">发送可选参数</param>
        /// <returns></returns>
        public Reply<ReadHoldingRegisterResponse> ReadHoldingRegister(ReadHoldingRegisterRequest request, int timeout = 3000, SendOptions options = null)
        {
            var reply = SendModbusRequest<ReadHoldingRegisterResponse>(request, timeout, options);
            return reply;
        }

        /// <summary>
        /// 读保持寄存器
        /// </summary>
        /// <param name="slaveId">从机地址</param>
        /// <param name="startAddress">起始地址</param>
        /// <param name="length">读取长度</param>
        /// <param name="timeout">超时时长(ms)</param>
        /// <param name="options">发送可选参数</param>
        /// <returns></returns>
        public Reply<ReadHoldingRegisterResponse> ReadHoldingRegister(byte slaveId, ushort startAddress, ushort length, int timeout = 3000, SendOptions options = null)
        {
            return this.ReadHoldingRegister(new ReadHoldingRegisterRequest() { SlaveId = slaveId, StartAddress = startAddress, Length = length, ProtocolFormat = ProtocolFormat }, timeout, options);
        }

        #endregion

        #region ReadInputRegister

        /// <summary>
        /// 读输入寄存器
        /// </summary>
        /// <param name="request">读输入寄存器的请求实体</param>
        /// <param name="timeout">超时时长(ms)</param>
        /// <param name="options">发送可选参数</param>
        /// <returns></returns>
        public Reply<ReadInputRegisterResponse> ReadInputRegister(ReadInputRegisterRequest request, int timeout = 3000, SendOptions options = null)
        {
            var reply = SendModbusRequest<ReadInputRegisterResponse>(request, timeout, options);
            return reply;
        }

        /// <summary>
        /// 读输入寄存器
        /// </summary>
        /// <param name="slaveId">从机地址</param>
        /// <param name="startAddress">起始地址</param>
        /// <param name="length">读取长度</param>
        /// <param name="timeout">超时时长(ms)</param>
        /// <param name="options">发送可选参数</param>
        /// <returns></returns>
        public Reply<ReadInputRegisterResponse> ReadInputRegister(byte slaveId, ushort startAddress, ushort length, int timeout = 3000, SendOptions options = null)
        {
            return this.ReadInputRegister(new ReadInputRegisterRequest() { SlaveId = slaveId, StartAddress = startAddress, Length = length, ProtocolFormat = ProtocolFormat }, timeout, options);
        }

        #endregion

        #region WriteRegisterResponse

        /// <summary>
        /// 发送Modbus写入请求
        /// </summary>
        /// <param name="request">Modbus请求实体</param>
        /// <param name="timeout">超时时长(ms)</param>
        /// <param name="options">发送可选参数</param>
        /// <returns></returns>
        public Reply<WriteRegisterResponse> SendModbusWriteRequest(ModbusRequest request, int timeout = 3000, SendOptions options = null)
        {
            return SendModbusRequest<WriteRegisterResponse>(request, timeout, options);
        }

        /// <summary>
        /// 写单个线圈寄存器
        /// </summary>
        /// <param name="request">写单个线圈寄存器的请求实体</param>
        /// <param name="timeout">超时时长(ms)</param>
        /// <param name="options">发送可选参数</param>
        /// <returns></returns>
        public Reply<WriteRegisterResponse> WriteSingleCoilRegister(WriteSingleCoilRegisterRequest request, int timeout = 3000, SendOptions options = null)
        {
            return SendModbusWriteRequest(request, timeout, options);
        }

        /// <summary>
        /// 写单个线圈寄存器
        /// </summary>
        /// <param name="slaveId">从机地址</param>
        /// <param name="writeAddress">写入地址</param>
        /// <param name="data">写入数据</param>
        /// <param name="timeout">超时时长(ms)</param>
        /// <param name="options">发送可选参数</param>
        /// <returns></returns>
        public Reply<WriteRegisterResponse> WriteSingleCoilRegister(byte slaveId, ushort writeAddress, bool data, int timeout = 3000, SendOptions options = null)
        {
            return this.WriteSingleCoilRegister(new WriteSingleCoilRegisterRequest() { SlaveId = slaveId, WriteAddress = writeAddress, Data = data, ProtocolFormat = ProtocolFormat }, timeout, options);
        }

        /// <summary>
        /// 写单个保持寄存器
        /// </summary>
        /// <param name="request">写单个保持寄存器的请求实体</param>
        /// <param name="timeout">超时时长(ms)</param>
        /// <param name="options">发送可选参数</param>
        /// <returns></returns>
        public Reply<WriteRegisterResponse> WriteSingleHoldingRegister(WriteSingleHoldingRegisterRequest request, int timeout = 3000, SendOptions options = null)
        {
            return SendModbusWriteRequest(request, timeout, options);
        }

        /// <summary>
        /// 写单个保持寄存器
        /// </summary>
        /// <param name="slaveId">从机地址</param>
        /// <param name="writeAddress">写入地址</param>
        /// <param name="data">写入数据</param>
        /// <param name="timeout">超时时长(ms)</param>
        /// <param name="options">发送可选参数</param>
        /// <returns></returns>
        public Reply<WriteRegisterResponse> WriteSingleHoldingRegister(byte slaveId, ushort writeAddress, byte[] data, int timeout = 3000, SendOptions options = null)
        {
            return this.WriteSingleHoldingRegister(new WriteSingleHoldingRegisterRequest() { SlaveId = slaveId, WriteAddress = writeAddress, Data = data, ProtocolFormat = ProtocolFormat }, timeout, options);
        }

        /// <summary>
        /// 写单个保持寄存器
        /// </summary>
        /// <param name="slaveId">从机地址</param>
        /// <param name="writeAddress">写入地址</param>
        /// <param name="data">写入数据</param>
        /// <param name="timeout">超时时长(ms)</param>
        /// <param name="options">发送可选参数</param>
        /// <returns></returns>
        public Reply<WriteRegisterResponse> WriteSingleHoldingRegister(byte slaveId, ushort writeAddress, ushort data, int timeout = 3000, SendOptions options = null)
        {
            byte[] bytes = new byte[] { (byte)(data >> 8), (byte)(data & 0xFF) };
            return this.WriteSingleHoldingRegister(new WriteSingleHoldingRegisterRequest() { SlaveId = slaveId, WriteAddress = writeAddress, Data = bytes, ProtocolFormat = ProtocolFormat }, timeout, options);
        }

        /// <summary>
        /// 写单个保持寄存器
        /// </summary>
        /// <param name="slaveId">从机地址</param>
        /// <param name="writeAddress">写入地址</param>
        /// <param name="data">写入数据</param>
        /// <param name="timeout">超时时长(ms)</param>
        /// <param name="options">发送可选参数</param>
        /// <returns></returns>
        public Reply<WriteRegisterResponse> WriteSingleHoldingRegister(byte slaveId, ushort writeAddress, short data, int timeout = 3000, SendOptions options = null)
        {
            byte[] bytes = new byte[] { (byte)(data >> 8), (byte)(data & 0xFF) };
            return this.WriteSingleHoldingRegister(new WriteSingleHoldingRegisterRequest() { SlaveId = slaveId, WriteAddress = writeAddress, Data = bytes, ProtocolFormat = ProtocolFormat }, timeout, options);
        }

        /// <summary>
        /// 写多个线圈寄存器
        /// </summary>
        /// <param name="request">写多个线圈寄存器的请求实体</param>
        /// <param name="timeout">超时时长(ms)</param>
        /// <param name="options">发送可选参数</param>
        /// <returns></returns>
        public Reply<WriteRegisterResponse> WriteMultipleCoilRegisters(WriteMultipleCoilRegistersRequest request, int timeout = 3000, SendOptions options = null)
        {
            return SendModbusWriteRequest(request, timeout, options);
        }

        /// <summary>
        /// 写多个线圈寄存器
        /// </summary>
        /// <param name="slaveId">从机地址</param>
        /// <param name="writeAddress">写入地址</param>
        /// <param name="data">写入数据</param>
        /// <param name="timeout">超时时长(ms)</param>
        /// <param name="options">发送可选参数</param>
        /// <returns></returns>
        public Reply<WriteRegisterResponse> WriteMultipleCoilRegisters(byte slaveId, ushort writeAddress, bool[] data, int timeout = 3000, SendOptions options = null)
        {
            return this.WriteMultipleCoilRegisters(new WriteMultipleCoilRegistersRequest() { SlaveId = slaveId, WriteAddress = writeAddress, Data = data, ProtocolFormat = ProtocolFormat }, timeout, options);
        }

        /// <summary>
        /// 写入多个保持寄存器
        /// </summary>
        /// <param name="request">写入多个保持寄存器的请求实体</param>
        /// <param name="timeout">超时时长(ms)</param>
        /// <param name="options">发送可选参数</param>
        /// <returns></returns>
        public Reply<WriteRegisterResponse> WriteMultipleHoldingRegisters(WriteMultipleHoldingRegistersRequest request, int timeout = 3000, SendOptions options = null)
        {
            return SendModbusWriteRequest(request, timeout, options);
        }

        /// <summary>
        /// 写入多个保持寄存器
        /// </summary>
        /// <param name="slaveId">从机地址</param>
        /// <param name="writeAddress">写入地址</param>
        /// <param name="data">写入数据</param>
        /// <param name="timeout">超时时长(ms)</param>
        /// <param name="options">发送可选参数</param>
        /// <returns></returns>
        public Reply<WriteRegisterResponse> WriteMultipleHoldingRegisters(byte slaveId, ushort writeAddress, ushort[] data, int timeout = 3000, SendOptions options = null)
        {
            byte[] bytes = new byte[(data?.Length ?? 0) * 2];
            if (data != null)
            {
                for (int i = 0; i < data.Length; i++)
                {
                    bytes[i * 2] = (byte)(data[i] >> 8);
                    bytes[i * 2 + 1] = (byte)(data[i] & 0xFF);
                }
            }
            return this.WriteMultipleHoldingRegisters(slaveId, writeAddress, bytes, timeout, options);
        }

        /// <summary>
        /// 写入多个保持寄存器
        /// </summary>
        /// <param name="slaveId">从站地址</param>
        /// <param name="writeAddress">写入地址</param>
        /// <param name="data">写入数据</param>
        /// <param name="timeout">超时时长(ms)</param>
        /// <param name="options">发送可选参数</param>
        /// <returns></returns>
        public Reply<WriteRegisterResponse> WriteMultipleHoldingRegisters(byte slaveId, ushort writeAddress, byte[] data, int timeout = 3000, SendOptions options = null)
        {
            return this.WriteMultipleHoldingRegisters(new WriteMultipleHoldingRegistersRequest() { SlaveId = slaveId, WriteAddress = writeAddress, Data = data, ProtocolFormat = ProtocolFormat }, timeout, options);
        }

        #endregion

        /// <summary>
        /// Modbus的请求与响应匹配过程
        /// </summary>
        protected virtual bool ReplyMatchHandle(ModbusRequest req, ModbusResponse resp)
        {
            try
            {
                // MBAP 模式下：直接基于事务元标识符 (TransactionId) 精确匹配，支持全双工高并发响应分发
                if (req.ProtocolFormat == ModbusProtocolFormat.MBAP && resp.ProtocolFormat == ModbusProtocolFormat.MBAP)
                {
                    return req.TransactionId == resp.TransactionId;
                }

                if (req.SlaveId != resp.SlaveId)
                {
                    return false;
                }

                if (req.FunctionCode != resp.FunctionCode)
                {
                    return false;
                }

                if (!resp.IsSuccess)
                {
                    // 如果请求失败（从机回复故障码），则匹配成功
                    return true;
                }

                // 读取类型
                if (req is ReadRegisterRequest sd)
                {
                    switch (req.FunctionCode)
                    {
                        case FunctionCode.ReadCoilRegister:
                            if (resp is ReadCoilRegisterResponse rd1) return (sd.Length + 7) / 8 == rd1.Length;
                            break;
                        case FunctionCode.ReadDiscreteInputRegister:
                            if (resp is ReadDiscreteInputRegisterResponse rd2) return (sd.Length + 7) / 8 == rd2.Length;
                            break;
                        case FunctionCode.ReadHoldingRegister:
                            if (resp is ReadHoldingRegisterResponse rd3) return sd.Length == rd3.Length / 2;
                            break;
                        case FunctionCode.ReadInputRegister:
                            if (resp is ReadInputRegisterResponse rd4) return sd.Length == rd4.Length / 2;
                            break;
                    }
                }

                // 写入类型
                if (req is WriteMultipleHoldingRegistersRequest sd1 && resp is WriteRegisterResponse rdWrite1)
                {
                    return sd1.WriteAddress == rdWrite1.WriteAddress && sd1.Data != null && rdWrite1.Values != null && rdWrite1.Values.Length >= 2 && (sd1.Data.Length / 2) == rdWrite1.GetUInt16();
                }

                if (req is WriteMultipleCoilRegistersRequest sd2 && resp is WriteRegisterResponse rdWrite2)
                {
                    return sd2.WriteAddress == rdWrite2.WriteAddress && sd2.Data != null && rdWrite2.Values != null && rdWrite2.Values.Length >= 2 && sd2.Data.Length == rdWrite2.GetUInt16();
                }

                if (req is WriteSingleHoldingRegisterRequest sd3 && resp is WriteRegisterResponse rdWrite3)
                {
                    return sd3.WriteAddress == rdWrite3.WriteAddress && rdWrite3.Values != null && rdWrite3.Values.SequenceEqual(sd3.Data);
                }

                if (req is WriteSingleCoilRegisterRequest sd4 && resp is WriteRegisterResponse rdWrite4)
                {
                    byte[] expectedBytes = sd4.Data ? new byte[] { 0xFF, 0x00 } : new byte[] { 0x00, 0x00 };
                    return sd4.WriteAddress == rdWrite4.WriteAddress && rdWrite4.Values != null && rdWrite4.Values.SequenceEqual(expectedBytes);
                }
            }
            catch (Exception ex)
            {
                RaiseExceptionOccurs(this, new ExceptionOccursEventArgs(new Exception("Reply match error.", ex)));
            }

            return false;
        }
    }

    public abstract partial class ModbusClient
    {
        /// <summary>
        /// 当接收到Modbus响应数据包时触发事件
        /// </summary>
        [Description("当接收到Modbus响应数据包时触发事件")]
        public event PacketReceivedHandler<ModbusResponse> OnModbusPacketReceived;

        /// <summary>
        /// 当接收到读线圈寄存器响应数据包时触发事件
        /// </summary>
        [Description("当接收到读线圈寄存器响应数据包时触发事件")]
        public event PacketReceivedHandler<ReadCoilRegisterResponse> OnReadCoilRegisterPacketReceived;

        /// <summary>
        /// 当接收到读离散输入寄存器响应数据包时触发事件
        /// </summary>
        [Description("当接收到读离散输入寄存器响应数据包时触发事件")]
        public event PacketReceivedHandler<ReadDiscreteInputRegisterResponse> OnReadDiscreteInputRegisterPacketReceived;

        /// <summary>
        /// 当接收到读保持寄存器响应数据包时触发事件
        /// </summary>
        [Description("当接收到读保持寄存器响应数据包时触发事件")]
        public event PacketReceivedHandler<ReadHoldingRegisterResponse> OnReadHoldingRegisterPacketReceived;

        /// <summary>
        /// 当接收到读输入寄存器响应数据包时触发事件
        /// </summary>
        [Description("当接收到读输入寄存器响应数据包时触发事件")]
        public event PacketReceivedHandler<ReadInputRegisterResponse> OnReadInputRegisterPacketReceived;

        /// <summary>
        /// 当接收到写单个线圈寄存器响应数据包时触发事件
        /// </summary>
        [Description("当接收到写单个线圈寄存器响应数据包时触发事件")]
        public event PacketReceivedHandler<WriteRegisterResponse> OnWriteSingleCoilRegisterPacketReceived;

        /// <summary>
        /// 当接收到写单个保持寄存器响应数据包时触发事件
        /// </summary>
        [Description("当接收到写单个保持寄存器响应数据包时触发事件")]
        public event PacketReceivedHandler<WriteRegisterResponse> OnWriteSingleHoldingRegisterPacketReceived;

        /// <summary>
        /// 当接收到写多个线圈寄存器响应数据包时触发事件
        /// </summary>
        [Description("当接收到写多个线圈寄存器响应数据包时触发事件")]
        public event PacketReceivedHandler<WriteRegisterResponse> OnWriteMultipleCoilRegistersPacketReceived;

        /// <summary>
        /// 当接收到写多个保持寄存器响应数据包时触发事件
        /// </summary>
        [Description("当接收到写多个保持寄存器响应数据包时触发事件")]
        public event PacketReceivedHandler<WriteRegisterResponse> OnWriteMultipleHoldingRegistersPacketReceived;

        /// <summary>
        /// 触发接收到Modbus响应数据包的事件
        /// </summary>
        protected void RaisePacketReceived(object sender, PacketReceivedEventArgs<ModbusResponse> e)
        {
            SafelyInvokeCallback(() => OnModbusPacketReceived?.Invoke(sender, e));
        }

        /// <summary>
        /// 触发当接收到读线圈寄存器响应数据包的事件
        /// </summary>
        protected void RaiseReadCoilRegisterPacketReceived(object sender, PacketReceivedEventArgs<ReadCoilRegisterResponse> e)
        {
            SafelyInvokeCallback(() => OnReadCoilRegisterPacketReceived?.Invoke(sender, e));
        }

        /// <summary>
        /// 触发当接收到读离散输入寄存器响应数据包的事件
        /// </summary>
        protected void RaiseReadDiscreteInputRegisterPacketReceived(object sender, PacketReceivedEventArgs<ReadDiscreteInputRegisterResponse> e)
        {
            SafelyInvokeCallback(() => OnReadDiscreteInputRegisterPacketReceived?.Invoke(sender, e));
        }

        /// <summary>
        /// 触发当接收到读保持寄存器响应数据包的事件
        /// </summary>
        protected void RaiseReadHoldingRegisterPacketReceived(object sender, PacketReceivedEventArgs<ReadHoldingRegisterResponse> e)
        {
            SafelyInvokeCallback(() => OnReadHoldingRegisterPacketReceived?.Invoke(sender, e));
        }

        /// <summary>
        /// 触发当接收到读输入寄存器响应数据包的事件
        /// </summary>
        protected void RaiseReadInputRegisterPacketReceived(object sender, PacketReceivedEventArgs<ReadInputRegisterResponse> e)
        {
            SafelyInvokeCallback(() => OnReadInputRegisterPacketReceived?.Invoke(sender, e));
        }

        /// <summary>
        /// 触发当接收到写单个线圈寄存器响应数据包的事件
        /// </summary>
        protected void RaiseWriteSingleCoilRegisterPacketReceived(object sender, PacketReceivedEventArgs<WriteRegisterResponse> e)
        {
            SafelyInvokeCallback(() => OnWriteSingleCoilRegisterPacketReceived?.Invoke(sender, e));
        }

        /// <summary>
        /// 触发当接收到写单个保持寄存器响应数据包的事件
        /// </summary>
        protected void RaiseWriteSingleHoldingRegisterPacketReceived(object sender, PacketReceivedEventArgs<WriteRegisterResponse> e)
        {
            SafelyInvokeCallback(() => OnWriteSingleHoldingRegisterPacketReceived?.Invoke(sender, e));
        }

        /// <summary>
        /// 触发当接收到写多个线圈寄存器响应数据包的事件
        /// </summary>
        protected void RaiseWriteMultipleCoilRegistersPacketReceived(object sender, PacketReceivedEventArgs<WriteRegisterResponse> e)
        {
            SafelyInvokeCallback(() => OnWriteMultipleCoilRegistersPacketReceived?.Invoke(sender, e));
        }

        /// <summary>
        /// 触发当接收到写多个保持寄存器响应数据包的事件
        /// </summary>
        protected void RaiseWriteMultipleHoldingRegistersPacketReceived(object sender, PacketReceivedEventArgs<WriteRegisterResponse> e)
        {
            SafelyInvokeCallback(() => OnWriteMultipleHoldingRegistersPacketReceived?.Invoke(sender, e));
        }

        /// <summary>
        /// 解包器解析完成数据时的事件处理函数
        /// </summary>
        /// <param name="sender">事件源</param>
        /// <param name="e">事件参数</param>
        protected void Unpacker_OnDataParsed(object sender, DataParsedEventArgs<ModbusResponse> e)
        {
            RaisePacketReceived(sender, new PacketReceivedEventArgs<ModbusResponse>(e.Data));

            if (e.Data is ReadCoilRegisterResponse res1)
                RaiseReadCoilRegisterPacketReceived(sender, new PacketReceivedEventArgs<ReadCoilRegisterResponse>(res1));
            else if (e.Data is ReadDiscreteInputRegisterResponse res2)
                RaiseReadDiscreteInputRegisterPacketReceived(sender, new PacketReceivedEventArgs<ReadDiscreteInputRegisterResponse>(res2));
            else if (e.Data is ReadHoldingRegisterResponse res3)
                RaiseReadHoldingRegisterPacketReceived(sender, new PacketReceivedEventArgs<ReadHoldingRegisterResponse>(res3));
            else if (e.Data is ReadInputRegisterResponse res4)
                RaiseReadInputRegisterPacketReceived(sender, new PacketReceivedEventArgs<ReadInputRegisterResponse>(res4));
            else if (e.Data is WriteRegisterResponse res5)
            {
                switch (e.Data.FunctionCode)
                {
                    case FunctionCode.WriteSingleCoilRegister:
                        RaiseWriteSingleCoilRegisterPacketReceived(sender, new PacketReceivedEventArgs<WriteRegisterResponse>(res5));
                        break;
                    case FunctionCode.WriteSingleHoldingRegister:
                        RaiseWriteSingleHoldingRegisterPacketReceived(sender, new PacketReceivedEventArgs<WriteRegisterResponse>(res5));
                        break;
                    case FunctionCode.WriteMultipleCoilRegisters:
                        RaiseWriteMultipleCoilRegistersPacketReceived(sender, new PacketReceivedEventArgs<WriteRegisterResponse>(res5));
                        break;
                    case FunctionCode.WriteMultipleHoldingRegisters:
                        RaiseWriteMultipleHoldingRegistersPacketReceived(sender, new PacketReceivedEventArgs<WriteRegisterResponse>(res5));
                        break;
                }
            }
        }
    }

    /// <summary>
    /// Modbus 协议格式
    /// </summary>
    public enum ModbusProtocolFormat
    {
        /// <summary>
        /// RTU 格式 (带从站站号与 CRC16 校验)
        /// </summary>
        [Description("RTU 格式")]
        RTU,

        /// <summary>
        /// ASCII 格式 (带冒号起始符、十六进制 ASCII 字符与 LRC 校验)
        /// </summary>
        [Description("ASCII 格式")]
        ASCII,

        /// <summary>
        /// MBAP 格式 (标准 Modbus TCP，带 7 字节 MBAP 报文头，无末尾 CRC 校验，支持全双工并发)
        /// </summary>
        [Description("MBAP 格式 (Modbus TCP)")]
        MBAP
    }
}
