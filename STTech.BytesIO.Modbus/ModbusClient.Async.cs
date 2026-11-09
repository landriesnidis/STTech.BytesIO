using STTech.BytesIO.Core;
using System;
using System.Threading.Tasks;

namespace STTech.BytesIO.Modbus
{
    public abstract partial class ModbusClient
    {
        #region ReadCoilRegisterAsync

        /// <summary>
        /// 异步读线圈寄存器
        /// </summary>
        public Task<Reply<ReadCoilRegisterResponse>> ReadCoilRegisterAsync(ReadCoilRegisterRequest request, int timeout = 3000, SendOptions options = null)
        {
            return SendModbusRequestAsync<ReadCoilRegisterResponse>(request, timeout, options);
        }

        /// <summary>
        /// 异步读线圈寄存器
        /// </summary>
        public Task<Reply<ReadCoilRegisterResponse>> ReadCoilRegisterAsync(byte slaveId, ushort startAddress, ushort length, int timeout = 3000, SendOptions options = null)
        {
            return this.ReadCoilRegisterAsync(new ReadCoilRegisterRequest() { SlaveId = slaveId, StartAddress = startAddress, Length = length, ProtocolFormat = ProtocolFormat }, timeout, options);
        }

        #endregion

        #region ReadDiscreteInputRegisterAsync

        /// <summary>
        /// 异步读离散输入寄存器
        /// </summary>
        public Task<Reply<ReadDiscreteInputRegisterResponse>> ReadDiscreteInputRegisterAsync(ReadDiscreteInputRegisterRequest request, int timeout = 3000, SendOptions options = null)
        {
            return SendModbusRequestAsync<ReadDiscreteInputRegisterResponse>(request, timeout, options);
        }

        /// <summary>
        /// 异步读离散输入寄存器
        /// </summary>
        public Task<Reply<ReadDiscreteInputRegisterResponse>> ReadDiscreteInputRegisterAsync(byte slaveId, ushort startAddress, ushort length, int timeout = 3000, SendOptions options = null)
        {
            return this.ReadDiscreteInputRegisterAsync(new ReadDiscreteInputRegisterRequest() { SlaveId = slaveId, StartAddress = startAddress, Length = length, ProtocolFormat = ProtocolFormat }, timeout, options);
        }

        #endregion

        #region ReadHoldingRegisterAsync

        /// <summary>
        /// 异步读保持寄存器
        /// </summary>
        public Task<Reply<ReadHoldingRegisterResponse>> ReadHoldingRegisterAsync(ReadHoldingRegisterRequest request, int timeout = 3000, SendOptions options = null)
        {
            return SendModbusRequestAsync<ReadHoldingRegisterResponse>(request, timeout, options);
        }

        /// <summary>
        /// 异步读保持寄存器
        /// </summary>
        public Task<Reply<ReadHoldingRegisterResponse>> ReadHoldingRegisterAsync(byte slaveId, ushort startAddress, ushort length, int timeout = 3000, SendOptions options = null)
        {
            return this.ReadHoldingRegisterAsync(new ReadHoldingRegisterRequest() { SlaveId = slaveId, StartAddress = startAddress, Length = length, ProtocolFormat = ProtocolFormat }, timeout, options);
        }

        #endregion

        #region ReadInputRegisterAsync

        /// <summary>
        /// 异步读输入寄存器
        /// </summary>
        public Task<Reply<ReadInputRegisterResponse>> ReadInputRegisterAsync(ReadInputRegisterRequest request, int timeout = 3000, SendOptions options = null)
        {
            return SendModbusRequestAsync<ReadInputRegisterResponse>(request, timeout, options);
        }

        /// <summary>
        /// 异步读输入寄存器
        /// </summary>
        public Task<Reply<ReadInputRegisterResponse>> ReadInputRegisterAsync(byte slaveId, ushort startAddress, ushort length, int timeout = 3000, SendOptions options = null)
        {
            return this.ReadInputRegisterAsync(new ReadInputRegisterRequest() { SlaveId = slaveId, StartAddress = startAddress, Length = length, ProtocolFormat = ProtocolFormat }, timeout, options);
        }

        #endregion

        #region WriteRegisterResponse Async

        /// <summary>
        /// 异步发送Modbus写入请求
        /// </summary>
        public Task<Reply<WriteRegisterResponse>> SendModbusWriteRequestAsync(ModbusRequest request, int timeout = 3000, SendOptions options = null)
        {
            return SendModbusRequestAsync<WriteRegisterResponse>(request, timeout, options);
        }

        /// <summary>
        /// 异步写单个线圈寄存器
        /// </summary>
        public Task<Reply<WriteRegisterResponse>> WriteSingleCoilRegisterAsync(WriteSingleCoilRegisterRequest request, int timeout = 3000, SendOptions options = null)
        {
            return SendModbusWriteRequestAsync(request, timeout, options);
        }

        /// <summary>
        /// 异步写单个线圈寄存器
        /// </summary>
        public Task<Reply<WriteRegisterResponse>> WriteSingleCoilRegisterAsync(byte slaveId, ushort writeAddress, bool data, int timeout = 3000, SendOptions options = null)
        {
            return this.WriteSingleCoilRegisterAsync(new WriteSingleCoilRegisterRequest() { SlaveId = slaveId, WriteAddress = writeAddress, Data = data, ProtocolFormat = ProtocolFormat }, timeout, options);
        }

        /// <summary>
        /// 异步写单个保持寄存器
        /// </summary>
        public Task<Reply<WriteRegisterResponse>> WriteSingleHoldingRegisterAsync(WriteSingleHoldingRegisterRequest request, int timeout = 3000, SendOptions options = null)
        {
            return SendModbusWriteRequestAsync(request, timeout, options);
        }

        /// <summary>
        /// 异步写单个保持寄存器
        /// </summary>
        public Task<Reply<WriteRegisterResponse>> WriteSingleHoldingRegisterAsync(byte slaveId, ushort writeAddress, byte[] data, int timeout = 3000, SendOptions options = null)
        {
            return this.WriteSingleHoldingRegisterAsync(new WriteSingleHoldingRegisterRequest() { SlaveId = slaveId, WriteAddress = writeAddress, Data = data, ProtocolFormat = ProtocolFormat }, timeout, options);
        }

        /// <summary>
        /// 异步写单个保持寄存器
        /// </summary>
        public Task<Reply<WriteRegisterResponse>> WriteSingleHoldingRegisterAsync(byte slaveId, ushort writeAddress, ushort data, int timeout = 3000, SendOptions options = null)
        {
            byte[] bytes = new byte[] { (byte)(data >> 8), (byte)(data & 0xFF) };
            return this.WriteSingleHoldingRegisterAsync(new WriteSingleHoldingRegisterRequest() { SlaveId = slaveId, WriteAddress = writeAddress, Data = bytes, ProtocolFormat = ProtocolFormat }, timeout, options);
        }

        /// <summary>
        /// 异步写单个保持寄存器
        /// </summary>
        public Task<Reply<WriteRegisterResponse>> WriteSingleHoldingRegisterAsync(byte slaveId, ushort writeAddress, short data, int timeout = 3000, SendOptions options = null)
        {
            byte[] bytes = new byte[] { (byte)(data >> 8), (byte)(data & 0xFF) };
            return this.WriteSingleHoldingRegisterAsync(new WriteSingleHoldingRegisterRequest() { SlaveId = slaveId, WriteAddress = writeAddress, Data = bytes, ProtocolFormat = ProtocolFormat }, timeout, options);
        }

        /// <summary>
        /// 异步写多个线圈寄存器
        /// </summary>
        public Task<Reply<WriteRegisterResponse>> WriteMultipleCoilRegistersAsync(WriteMultipleCoilRegistersRequest request, int timeout = 3000, SendOptions options = null)
        {
            return SendModbusWriteRequestAsync(request, timeout, options);
        }

        /// <summary>
        /// 异步写多个线圈寄存器
        /// </summary>
        public Task<Reply<WriteRegisterResponse>> WriteMultipleCoilRegistersAsync(byte slaveId, ushort writeAddress, bool[] data, int timeout = 3000, SendOptions options = null)
        {
            return this.WriteMultipleCoilRegistersAsync(new WriteMultipleCoilRegistersRequest() { SlaveId = slaveId, WriteAddress = writeAddress, Data = data, ProtocolFormat = ProtocolFormat }, timeout, options);
        }

        /// <summary>
        /// 异步写入多个保持寄存器
        /// </summary>
        public Task<Reply<WriteRegisterResponse>> WriteMultipleHoldingRegistersAsync(WriteMultipleHoldingRegistersRequest request, int timeout = 3000, SendOptions options = null)
        {
            return SendModbusWriteRequestAsync(request, timeout, options);
        }

        /// <summary>
        /// 异步写入多个保持寄存器
        /// </summary>
        public Task<Reply<WriteRegisterResponse>> WriteMultipleHoldingRegistersAsync(byte slaveId, ushort writeAddress, ushort[] data, int timeout = 3000, SendOptions options = null)
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
            return this.WriteMultipleHoldingRegistersAsync(slaveId, writeAddress, bytes, timeout, options);
        }

        /// <summary>
        /// 异步写入多个保持寄存器
        /// </summary>
        public Task<Reply<WriteRegisterResponse>> WriteMultipleHoldingRegistersAsync(byte slaveId, ushort writeAddress, byte[] data, int timeout = 3000, SendOptions options = null)
        {
            return this.WriteMultipleHoldingRegistersAsync(new WriteMultipleHoldingRegistersRequest() { SlaveId = slaveId, WriteAddress = writeAddress, Data = data, ProtocolFormat = ProtocolFormat }, timeout, options);
        }

        #endregion
    }
}
