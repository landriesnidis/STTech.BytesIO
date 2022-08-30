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
            var reply = client.Send(request, timeout, (sd, rd) => sd.SlaveId == rd.SlaveId && sd.FunctionCode == rd.FunctionCode, options);
            return reply.ConvertTo<ReadCoilRegisterResponse>();
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
            var reply = client.Send(request, timeout, (sd, rd) => sd.SlaveId == rd.SlaveId && sd.FunctionCode == rd.FunctionCode, options);
            return reply.ConvertTo<ReadDiscreteInputRegisterResponse>();
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
            var reply = client.Send(request, timeout, (sd, rd) => sd.SlaveId == rd.SlaveId && sd.FunctionCode == rd.FunctionCode, options);
            return reply.ConvertTo<ReadHoldRegisterResponse>();
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
            var reply = client.Send(request, timeout, (sd, rd) => sd.SlaveId == rd.SlaveId && sd.FunctionCode == rd.FunctionCode, options);
            return reply.ConvertTo<ReadInputRegisterResponse>();
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
            var reply = client.Send(request, timeout, (sd, rd) => sd.SlaveId == rd.SlaveId && sd.FunctionCode == rd.FunctionCode, options);
            return reply.ConvertTo<WriteSingleCoilRegisterResponse>();
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
            var reply = client.Send(request, timeout, (sd, rd) => sd.SlaveId == rd.SlaveId && sd.FunctionCode == rd.FunctionCode, options);
            return reply.ConvertTo<WriteSingleHoldRegisterResponse>();
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
            var reply = client.Send(request, timeout, (sd, rd) => sd.SlaveId == rd.SlaveId && sd.FunctionCode == rd.FunctionCode, options);
            return reply.ConvertTo<WriteMultipleCoilRegistersResponse>();
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
            var reply = client.Send(request, timeout, (sd, rd) => sd.SlaveId == rd.SlaveId && sd.FunctionCode == rd.FunctionCode, options);
            return reply.ConvertTo<WriteMultipleHoldRegistersResponse>();
        }
    }
}
