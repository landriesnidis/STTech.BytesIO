using STTech.BytesIO.Core;
using STTech.BytesIO.Core.Entity;

namespace STTech.BytesIO.Modbus
{
    public static class ModbusClientExtension
    {
        /// <summary>
        /// 读线圈寄存器
        /// </summary>
        /// <param name="client">Modbus客户端</param>
        /// <param name="request">读线圈寄存器的请求实体</param>
        /// <param name="timeout">超时时长(ms)</param>
        /// <param name="options">发送可选参数</param>
        /// <returns></returns>
        public static Reply<ReadCoilRegisterResponse> ReadCoilRegister(this IModbusClient client, ReadCoilRegisterRequest request, int timeout = 3000, SendOptions options = null)
        {
            var reply = client.Send(request, timeout, (sd, rd) => sd.SlaveId == rd.SlaveId, options);
            return reply.ConvertTo<ReadCoilRegisterResponse>();
        }

        /// <summary>
        /// 读线圈寄存器
        /// </summary>
        /// <param name="client">Modbus客户端</param>
        /// <param name="slaveId">从机地址</param>
        /// <param name="startAddress">起始地址</param>
        /// <param name="length">读取长度</param>
        /// <param name="timeout">超时时长(ms)</param>
        /// <param name="options">发送可选参数</param>
        /// <returns></returns>
        public static Reply<ReadCoilRegisterResponse> ReadCoilRegister(this IModbusClient client, byte slaveId, ushort startAddress, ushort length, int timeout = 3000, SendOptions options = null)
        {
            return client.ReadCoilRegister(new ReadCoilRegisterRequest() { SlaveId = slaveId, StartAddress = startAddress, Length = length }, timeout, options);
        }

        /// <summary>
        /// 读离散输入寄存器
        /// </summary>
        /// <param name="client">Modbus客户端</param>
        /// <param name="request">读离散输入寄存器的请求实体</param>
        /// <param name="timeout">超时时长(ms)</param>
        /// <param name="options">发送可选参数</param>
        /// <returns></returns>
        public static Reply<ReadDiscreteInputRegisterResponse> ReadDiscreteInputRegister(this IModbusClient client, ReadDiscreteInputRegisterRequest request, int timeout = 3000, SendOptions options = null)
        {
            var reply = client.Send(request, timeout, (sd, rd) => sd.SlaveId == rd.SlaveId, options);
            return reply.ConvertTo<ReadDiscreteInputRegisterResponse>();
        }

        /// <summary>
        /// 读离散输入寄存器
        /// </summary>
        /// <param name="client">Modbus客户端</param>
        /// <param name="slaveId">从机地址</param>
        /// <param name="startAddress">起始地址</param>
        /// <param name="length">读取长度</param>
        /// <param name="timeout">超时时长(ms)</param>
        /// <param name="options">发送可选参数</param>
        /// <returns></returns>
        public static Reply<ReadDiscreteInputRegisterResponse> ReadDiscreteInputRegister(this IModbusClient client, byte slaveId, ushort startAddress, ushort length, int timeout = 3000, SendOptions options = null)
        {
            return client.ReadDiscreteInputRegister(new ReadDiscreteInputRegisterRequest() { SlaveId = slaveId, StartAddress = startAddress, Length = length }, timeout, options);
        }

        /// <summary>
        /// 读保持寄存器
        /// </summary>
        /// <param name="client">Modbus客户端</param>
        /// <param name="request">读保持寄存器的请求实体</param>
        /// <param name="timeout">超时时长(ms)</param>
        /// <param name="options">发送可选参数</param>
        /// <returns></returns>
        public static Reply<ReadHoldRegisterResponse> ReadHoldRegister(this IModbusClient client, ReadHoldRegisterRequest request, int timeout = 3000, SendOptions options = null)
        {
            var reply = client.Send(request, timeout, (sd, rd) => sd.SlaveId == rd.SlaveId, options);
            return reply.ConvertTo<ReadHoldRegisterResponse>();
        }

        /// <summary>
        /// 读保持寄存器
        /// </summary>
        /// <param name="client">Modbus客户端</param>
        /// <param name="slaveId">从机地址</param>
        /// <param name="startAddress">起始地址</param>
        /// <param name="length">读取长度</param>
        /// <param name="timeout">超时时长(ms)</param>
        /// <param name="options">发送可选参数</param>
        /// <returns></returns>
        public static Reply<ReadHoldRegisterResponse> ReadHoldRegister(this IModbusClient client, byte slaveId, ushort startAddress, ushort length, int timeout = 3000, SendOptions options = null)
        {
            return client.ReadHoldRegister(new ReadHoldRegisterRequest() { SlaveId = slaveId, StartAddress = startAddress, Length = length }, timeout, options);
        }

        /// <summary>
        /// 读输入寄存器
        /// </summary>
        /// <param name="client">Modbus客户端</param>
        /// <param name="request">读输入寄存器的请求实体</param>
        /// <param name="timeout">超时时长(ms)</param>
        /// <param name="options">发送可选参数</param>
        /// <returns></returns>
        public static Reply<ReadInputRegisterResponse> ReadInputRegister(this IModbusClient client, ReadInputRegisterRequest request, int timeout = 3000, SendOptions options = null)
        {
            var reply = client.Send(request, timeout, (sd, rd) => sd.SlaveId == rd.SlaveId, options);
            return reply.ConvertTo<ReadInputRegisterResponse>();
        }

        /// <summary>
        /// 读输入寄存器
        /// </summary>
        /// <param name="client">Modbus客户端</param>
        /// <param name="slaveId">从机地址</param>
        /// <param name="startAddress">起始地址</param>
        /// <param name="length">读取长度</param>
        /// <param name="timeout">超时时长(ms)</param>
        /// <param name="options">发送可选参数</param>
        /// <returns></returns>
        public static Reply<ReadInputRegisterResponse> ReadInputRegister(this IModbusClient client, byte slaveId, ushort startAddress, ushort length, int timeout = 3000, SendOptions options = null)
        {
            return client.ReadInputRegister(new ReadInputRegisterRequest() { SlaveId = slaveId, StartAddress = startAddress, Length = length }, timeout, options);
        }

        /// <summary>
        /// 写单个线圈寄存器
        /// </summary>
        /// <param name="client">Modbus客户端</param>
        /// <param name="request">写单个线圈寄存器的请求实体</param>
        /// <param name="timeout">超时时长(ms)</param>
        /// <param name="options">发送可选参数</param>
        /// <returns></returns>
        public static Reply<WriteSingleCoilRegisterResponse> WriteSingleCoilRegister(this IModbusClient client, WriteSingleCoilRegisterRequest request, int timeout = 3000, SendOptions options = null)
        {
            var reply = client.Send(request, timeout, (sd, rd) => sd.SlaveId == rd.SlaveId, options);
            return reply.ConvertTo<WriteSingleCoilRegisterResponse>();
        }

        /// <summary>
        /// 写单个线圈寄存器
        /// </summary>
        /// <param name="client">Modbus客户端</param>
        /// <param name="slaveId">从站地址</param>
        /// <param name="writeAddress">写入地址</param>
        /// <param name="data">写入值</param>
        /// <param name="timeout">超长时长(ms)</param>
        /// <param name="options">发送可选参数</param>
        /// <returns></returns>
        public static Reply<WriteSingleCoilRegisterResponse> WriteSingleCoilRegister(this IModbusClient client, byte slaveId, ushort writeAddress, bool data, int timeout = 3000, SendOptions options = null)
        {

            return client.WriteSingleCoilRegister(new WriteSingleCoilRegisterRequest { SlaveId = slaveId, WriteAddress = writeAddress, Data = data });
        }

        /// <summary>
        /// 写单个保持寄存器
        /// </summary>
        /// <param name="client">Modbus客户端</param>
        /// <param name="request">写单个保持寄存器的请求实体</param>
        /// <param name="timeout">超时时长(ms)</param>
        /// <param name="options">发送可选参数</param>
        /// <returns></returns>
        public static Reply<WriteSingleHoldRegisterResponse> WriteSingleHoldRegister(this IModbusClient client, WriteSingleHoldRegisterRequest request, int timeout = 3000, SendOptions options = null)
        {
            var reply = client.Send(request, timeout, (sd, rd) => sd.SlaveId == rd.SlaveId, options);
            return reply.ConvertTo<WriteSingleHoldRegisterResponse>();
        }

        /// <summary>
        /// 写单个保持寄存器
        /// </summary>
        /// <param name="client">Modbus客户端</param>
        /// <param name="slaveId">从站地址</param>
        /// <param name="writeAddress">写入地址</param>
        /// <param name="data">写入值</param>
        /// <param name="timeout">超时时长(ms)</param>
        /// <param name="options">发送可选参数</param>
        /// <returns></returns>
        public static Reply<WriteSingleHoldRegisterResponse> WriteSingleHoldRegister(this IModbusClient client, byte slaveId, ushort writeAddress, byte[] data, int timeout = 3000, SendOptions options = null)
        {
            return client.WriteSingleHoldRegister(new WriteSingleHoldRegisterRequest { SlaveId = slaveId, WriteAddress = writeAddress, Data = data });
        }

        /// <summary>
        /// 写多个线圈寄存器
        /// </summary>
        /// <param name="client">Modbus客户端</param>
        /// <param name="request">写多个线圈寄存器的请求实体</param>
        /// <param name="timeout">超时时长(ms)</param>
        /// <param name="options">发送可选参数</param>
        /// <returns></returns>
        public static Reply<WriteMultipleCoilRegistersResponse> WriteMultipleCoilRegisters(this IModbusClient client, WriteMultipleCoilRegistersRequest request, int timeout = 3000, SendOptions options = null)
        {
            var reply = client.Send(request, timeout, (sd, rd) => sd.SlaveId == rd.SlaveId, options);
            return reply.ConvertTo<WriteMultipleCoilRegistersResponse>();
        }

        /// <summary>
        /// 写多个线圈寄存器
        /// </summary>
        /// <param name="client">Modbus客户端</param>
        /// <param name="slaveId">从站地址</param>
        /// <param name="writeAddress">写入地址</param>
        /// <param name="data">写入值</param>
        /// <param name="timeout">超时时长(ms)</param>
        /// <param name="options">发送可选参数</param>
        /// <returns></returns>
        public static Reply<WriteMultipleCoilRegistersResponse> WriteMultipleCoilRegisters(this IModbusClient client, byte slaveId, ushort writeAddress, bool[] data, int timeout = 3000, SendOptions options = null)
        {
            return client.WriteMultipleCoilRegisters(new WriteMultipleCoilRegistersRequest { SlaveId = slaveId, WriteAddress = writeAddress, Data = data });
        }

        /// <summary>
        /// 写多个保持寄存器
        /// </summary>
        /// <param name="client">Modbus客户端</param>
        /// <param name="request">写多个保持寄存器的请求实体</param>
        /// <param name="timeout">超时时长(ms)</param>
        /// <param name="options">发送可选参数</param>
        /// <returns></returns>
        public static Reply<WriteMultipleHoldRegistersResponse> WriteMultipleHoldRegisters(this IModbusClient client, WriteMultipleHoldRegistersRequest request, int timeout = 3000, SendOptions options = null)
        {
            var reply = client.Send(request, timeout, (sd, rd) => sd.SlaveId == rd.SlaveId, options);
            return reply.ConvertTo<WriteMultipleHoldRegistersResponse>();
        }

        /// <summary>
        /// 写入多个保持寄存器
        /// </summary>
        /// <param name="client">Modbus客户端</param>
        /// <param name="slaveId">从站地址</param>
        /// <param name="writeAddress">写入地址</param>
        /// <param name="writeLength">写入长度</param>
        /// <param name="data">写入数据</param>
        /// <param name="timeout">超时时长(ms)</param>
        /// <param name="options">发送可选参数</param>
        /// <returns></returns>
        public static Reply<WriteMultipleHoldRegistersResponse> WriteMultipleHoldRegisters(this IModbusClient client, byte slaveId, ushort writeAddress, ushort writeLength, byte[] data, int timeout = 3000, SendOptions options = null)
        {
            return client.WriteMultipleHoldRegisters(new WriteMultipleHoldRegistersRequest { SlaveId = slaveId, WriteAddress = writeAddress, WriteLength = writeLength, Data = data, });
        }

    }
}
