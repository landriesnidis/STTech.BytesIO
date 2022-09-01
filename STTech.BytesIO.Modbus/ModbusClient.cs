using STTech.BytesIO.Core;
using STTech.BytesIO.Core.Component;
using STTech.BytesIO.Core.Entity;
using System.ComponentModel;

namespace STTech.BytesIO.Modbus
{
    public abstract partial class ModbusClient<TClient> : VirtualClient<TClient>, IModbusClient where TClient : BytesClient
    {
        public Unpacker<ModbusResponse> Unpacker { get; }

        public ModbusClient()
        {
            Unpacker = new ModbusUnpacker(this);
            this.BindUnpacker(Unpacker);

            Unpacker.OnDataParsed += Unpacker_OnDataParsed;
        }
    }

    public abstract partial class ModbusClient<TClient>
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
