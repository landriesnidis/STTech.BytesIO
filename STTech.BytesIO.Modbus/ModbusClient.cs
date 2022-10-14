using STTech.BytesIO.Core;
using STTech.BytesIO.Core.Component;
using System.ComponentModel;

namespace STTech.BytesIO.Modbus
{
    /// <summary>
    /// Modbus虚拟客户端
    /// </summary>
    public abstract partial class ModbusClient : VirtualClient
    {
        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public Unpacker<ModbusResponse> Unpacker { get; }

        protected ModbusClient(BytesClient client) : base(client)
        {
            Unpacker = new ModbusUnpacker(this);
            this.BindUnpacker(Unpacker);

            Unpacker.OnDataParsed += Unpacker_OnDataParsed;
        }
    }

    public abstract partial class ModbusClient : IModbusClient
    {
        public Reply<ReadCoilRegisterResponse> ReadCoilRegister(ReadCoilRegisterRequest request, int timeout = 3000, SendOptions options = null)
        {
            var reply = this.Send(request, timeout, (sd, rd) => sd.SlaveId == rd.SlaveId, options);
            return reply.ConvertTo<ReadCoilRegisterResponse>();
        }

        public Reply<ReadCoilRegisterResponse> ReadCoilRegister(byte slaveId, ushort startAddress, ushort length, int timeout = 3000, SendOptions options = null)
        {
            return this.ReadCoilRegister(new ReadCoilRegisterRequest() { SlaveId = slaveId, StartAddress = startAddress, Length = length }, timeout, options);
        }

        public Reply<ReadDiscreteInputRegisterResponse> ReadDiscreteInputRegister(ReadDiscreteInputRegisterRequest request, int timeout = 3000, SendOptions options = null)
        {
            var reply = this.Send(request, timeout, (sd, rd) => sd.SlaveId == rd.SlaveId, options);
            return reply.ConvertTo<ReadDiscreteInputRegisterResponse>();
        }

        public Reply<ReadDiscreteInputRegisterResponse> ReadDiscreteInputRegister(byte slaveId, ushort startAddress, ushort length, int timeout = 3000, SendOptions options = null)
        {
            return this.ReadDiscreteInputRegister(new ReadDiscreteInputRegisterRequest() { SlaveId = slaveId, StartAddress = startAddress, Length = length }, timeout, options);
        }

        public Reply<ReadHoldRegisterResponse> ReadHoldRegister(ReadHoldRegisterRequest request, int timeout = 3000, SendOptions options = null)
        {
            var reply = this.Send(request, timeout, (sd, rd) => sd.SlaveId == rd.SlaveId, options);
            return reply.ConvertTo<ReadHoldRegisterResponse>();
        }

        public Reply<ReadHoldRegisterResponse> ReadHoldRegister(byte slaveId, ushort startAddress, ushort length, int timeout = 3000, SendOptions options = null)
        {
            return this.ReadHoldRegister(new ReadHoldRegisterRequest() { SlaveId = slaveId, StartAddress = startAddress, Length = length }, timeout, options);
        }

        public Reply<ReadInputRegisterResponse> ReadInputRegister(ReadInputRegisterRequest request, int timeout = 3000, SendOptions options = null)
        {
            var reply = this.Send(request, timeout, (sd, rd) => sd.SlaveId == rd.SlaveId, options);
            return reply.ConvertTo<ReadInputRegisterResponse>();
        }

        public Reply<ReadInputRegisterResponse> ReadInputRegister(byte slaveId, ushort startAddress, ushort length, int timeout = 3000, SendOptions options = null)
        {
            return this.ReadInputRegister(new ReadInputRegisterRequest() { SlaveId = slaveId, StartAddress = startAddress, Length = length }, timeout, options);
        }

        public Reply<WriteSingleCoilRegisterResponse> WriteSingleCoilRegister(WriteSingleCoilRegisterRequest request, int timeout = 3000, SendOptions options = null)
        {
            var reply = this.Send(request, timeout, (sd, rd) => sd.SlaveId == rd.SlaveId, options);
            return reply.ConvertTo<WriteSingleCoilRegisterResponse>();
        }

        public Reply<WriteSingleCoilRegisterResponse> WriteSingleCoilRegister(byte slaveId, ushort writeAddress, bool data, int timeout = 3000, SendOptions options = null)
        {
            return this.WriteSingleCoilRegister(new WriteSingleCoilRegisterRequest { SlaveId = slaveId, WriteAddress = writeAddress, Data = data });
        }

        public Reply<WriteSingleHoldRegisterResponse> WriteSingleHoldRegister(WriteSingleHoldRegisterRequest request, int timeout = 3000, SendOptions options = null)
        {
            var reply = this.Send(request, timeout, (sd, rd) => sd.SlaveId == rd.SlaveId, options);
            return reply.ConvertTo<WriteSingleHoldRegisterResponse>();
        }

        public Reply<WriteSingleHoldRegisterResponse> WriteSingleHoldRegister(byte slaveId, ushort writeAddress, byte[] data, int timeout = 3000, SendOptions options = null)
        {
            return this.WriteSingleHoldRegister(new WriteSingleHoldRegisterRequest { SlaveId = slaveId, WriteAddress = writeAddress, Data = data });
        }

        public Reply<WriteMultipleCoilRegistersResponse> WriteMultipleCoilRegisters(WriteMultipleCoilRegistersRequest request, int timeout = 3000, SendOptions options = null)
        {
            var reply = this.Send(request, timeout, (sd, rd) => sd.SlaveId == rd.SlaveId, options);
            return reply.ConvertTo<WriteMultipleCoilRegistersResponse>();
        }

        public Reply<WriteMultipleCoilRegistersResponse> WriteMultipleCoilRegisters(byte slaveId, ushort writeAddress, bool[] data, int timeout = 3000, SendOptions options = null)
        {
            return this.WriteMultipleCoilRegisters(new WriteMultipleCoilRegistersRequest { SlaveId = slaveId, WriteAddress = writeAddress, Data = data });
        }

        public Reply<WriteMultipleHoldRegistersResponse> WriteMultipleHoldRegisters(WriteMultipleHoldRegistersRequest request, int timeout = 3000, SendOptions options = null)
        {
            var reply = this.Send(request, timeout, (sd, rd) => sd.SlaveId == rd.SlaveId, options);
            return reply.ConvertTo<WriteMultipleHoldRegistersResponse>();
        }

        public Reply<WriteMultipleHoldRegistersResponse> WriteMultipleHoldRegisters(byte slaveId, ushort writeAddress, ushort writeLength, byte[] data, int timeout = 3000, SendOptions options = null)
        {
            return this.WriteMultipleHoldRegisters(new WriteMultipleHoldRegistersRequest { SlaveId = slaveId, WriteAddress = writeAddress, WriteLength = writeLength, Data = data, });
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
        public event PacketReceivedHandler<ReadHoldRegisterResponse> OnReadHoldRegisterPacketReceived;

        /// <summary>
        /// 当接收到读输入寄存器响应数据包时触发事件
        /// </summary>
        [Description("当接收到读输入寄存器响应数据包时触发事件")]
        public event PacketReceivedHandler<ReadInputRegisterResponse> OnReadInputRegisterPacketReceived;

        /// <summary>
        /// 当接收到写单个线圈寄存器响应数据包时触发事件
        /// </summary>
        [Description("当接收到写单个线圈寄存器响应数据包时触发事件")]
        public event PacketReceivedHandler<WriteSingleCoilRegisterResponse> OnWriteSingleCoilRegisterPacketReceived;

        /// <summary>
        /// 当接收到写单个保持寄存器响应数据包时触发事件
        /// </summary>
        [Description("当接收到写单个保持寄存器响应数据包时触发事件")]
        public event PacketReceivedHandler<WriteSingleHoldRegisterResponse> OnWriteSingleHoldRegisterPacketReceived;

        /// <summary>
        /// 当接收到写多个线圈寄存器响应数据包时触发事件
        /// </summary>
        [Description("当接收到写多个线圈寄存器响应数据包时触发事件")]
        public event PacketReceivedHandler<WriteMultipleCoilRegistersResponse> OnWriteMultipleCoilRegistersPacketReceived;

        /// <summary>
        /// 当接收到写多个保持寄存器响应数据包时触发事件
        /// </summary>
        [Description("当接收到写多个保持寄存器响应数据包时触发事件")]
        public event PacketReceivedHandler<WriteMultipleHoldRegistersResponse> OnWriteMultipleHoldRegistersPacketReceived;

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
        protected void RaiseReadHoldRegisterPacketReceived(object sender, PacketReceivedEventArgs<ReadHoldRegisterResponse> e)
        {
            SafelyInvokeCallback(() => OnReadHoldRegisterPacketReceived?.Invoke(sender, e));
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
        protected void RaiseWriteSingleCoilRegisterPacketReceived(object sender, PacketReceivedEventArgs<WriteSingleCoilRegisterResponse> e)
        {
            SafelyInvokeCallback(() => OnWriteSingleCoilRegisterPacketReceived?.Invoke(sender, e));
        }

        /// <summary>
        /// 触发当接收到写单个保持寄存器响应数据包的事件
        /// </summary>
        protected void RaiseWriteSingleHoldRegisterPacketReceived(object sender, PacketReceivedEventArgs<WriteSingleHoldRegisterResponse> e)
        {
            SafelyInvokeCallback(() => OnWriteSingleHoldRegisterPacketReceived?.Invoke(sender, e));
        }

        /// <summary>
        /// 触发当接收到写多个线圈寄存器响应数据包的事件
        /// </summary>
        protected void RaiseWriteMultipleCoilRegistersPacketReceived(object sender, PacketReceivedEventArgs<WriteMultipleCoilRegistersResponse> e)
        {
            SafelyInvokeCallback(() => OnWriteMultipleCoilRegistersPacketReceived?.Invoke(sender, e));
        }

        /// <summary>
        /// 触发当接收到写多个保持寄存器响应数据包的事件
        /// </summary>
        protected void RaiseWriteMultipleHoldRegistersPacketReceived(object sender, PacketReceivedEventArgs<WriteMultipleHoldRegistersResponse> e)
        {
            SafelyInvokeCallback(() => OnWriteMultipleHoldRegistersPacketReceived?.Invoke(sender, e));
        }

        private void Unpacker_OnDataParsed(object sender, DataParsedEventArgs<ModbusResponse> e)
        {
            RaisePacketReceived(sender, new PacketReceivedEventArgs<ModbusResponse>(e.Data));

            switch (e.Data.FunctionCode)
            {
                case FunctionCode.ReadCoilRegister:
                    RaiseReadCoilRegisterPacketReceived(sender, new PacketReceivedEventArgs<ReadCoilRegisterResponse>(new ReadCoilRegisterResponse(e.Data.GetOriginalData())));
                    break;
                case FunctionCode.ReadDiscreteInputRegister:
                    RaiseReadDiscreteInputRegisterPacketReceived(sender, new PacketReceivedEventArgs<ReadDiscreteInputRegisterResponse>(new ReadDiscreteInputRegisterResponse(e.Data.GetOriginalData())));
                    break;
                case FunctionCode.ReadHoldRegister:
                    RaiseReadHoldRegisterPacketReceived(sender, new PacketReceivedEventArgs<ReadHoldRegisterResponse>(new ReadHoldRegisterResponse(e.Data.GetOriginalData())));
                    break;
                case FunctionCode.ReadInputRegister:
                    RaiseReadInputRegisterPacketReceived(sender, new PacketReceivedEventArgs<ReadInputRegisterResponse>(new ReadInputRegisterResponse(e.Data.GetOriginalData())));
                    break;
                case FunctionCode.WriteSingleCoilRegister:
                    RaiseWriteSingleCoilRegisterPacketReceived(sender, new PacketReceivedEventArgs<WriteSingleCoilRegisterResponse>(new WriteSingleCoilRegisterResponse(e.Data.GetOriginalData())));
                    break;
                case FunctionCode.WriteSingleHoldRegister:
                    RaiseWriteSingleHoldRegisterPacketReceived(sender, new PacketReceivedEventArgs<WriteSingleHoldRegisterResponse>(new WriteSingleHoldRegisterResponse(e.Data.GetOriginalData())));
                    break;
                case FunctionCode.WriteMultipleCoilRegisters:
                    RaiseWriteMultipleCoilRegistersPacketReceived(sender, new PacketReceivedEventArgs<WriteMultipleCoilRegistersResponse>(new WriteMultipleCoilRegistersResponse(e.Data.GetOriginalData())));
                    break;
                case FunctionCode.WriteMultipleHoldRegisters:
                    RaiseWriteMultipleHoldRegistersPacketReceived(sender, new PacketReceivedEventArgs<WriteMultipleHoldRegistersResponse>(new WriteMultipleHoldRegistersResponse(e.Data.GetOriginalData())));
                    break;

            }
        }
    }
}
