using STTech.BytesIO.Core;
using STTech.BytesIO.Core.Component;
using STTech.BytesIO.Modbus;
using System.Text;

namespace STTech.BytesIO.Modbus
{
    public interface IModbusClient : IBytesClient, IUnpackerSupport<ModbusResponse>
    {
        /// <summary>
        /// 读线圈寄存器
        /// </summary>
        /// <param name="request">读线圈寄存器的请求实体</param>
        /// <param name="timeout">超时时长(ms)</param>
        /// <param name="options">发送可选参数</param>
        /// <returns></returns>
        Reply<ReadCoilRegisterResponse> ReadCoilRegister(ReadCoilRegisterRequest request, int timeout = 3000, SendOptions options = null);

        /// <summary>
        /// 读线圈寄存器
        /// </summary>
        /// <param name="slaveId">从机地址</param>
        /// <param name="startAddress">起始地址</param>
        /// <param name="length">读取长度</param>
        /// <param name="timeout">超时时长(ms)</param>
        /// <param name="options">发送可选参数</param>
        /// <returns></returns>
        Reply<ReadCoilRegisterResponse> ReadCoilRegister(byte slaveId, ushort startAddress, ushort length, int timeout = 3000, SendOptions options = null);

        /// <summary>
        /// 读离散输入寄存器
        /// </summary>
        /// <param name="request">读离散输入寄存器的请求实体</param>
        /// <param name="timeout">超时时长(ms)</param>
        /// <param name="options">发送可选参数</param>
        /// <returns></returns>
        Reply<ReadDiscreteInputRegisterResponse> ReadDiscreteInputRegister(ReadDiscreteInputRegisterRequest request, int timeout = 3000, SendOptions options = null);

        /// <summary>
        /// 读离散输入寄存器
        /// </summary>
        /// <param name="slaveId">从机地址</param>
        /// <param name="startAddress">起始地址</param>
        /// <param name="length">读取长度</param>
        /// <param name="timeout">超时时长(ms)</param>
        /// <param name="options">发送可选参数</param>
        /// <returns></returns>
        Reply<ReadDiscreteInputRegisterResponse> ReadDiscreteInputRegister(byte slaveId, ushort startAddress, ushort length, int timeout = 3000, SendOptions options = null);

        /// <summary>
        /// 读保持寄存器
        /// </summary>
        /// <param name="request">读保持寄存器的请求实体</param>
        /// <param name="timeout">超时时长(ms)</param>
        /// <param name="options">发送可选参数</param>
        /// <returns></returns>
        Reply<ReadHoldRegisterResponse> ReadHoldRegister(ReadHoldRegisterRequest request, int timeout = 3000, SendOptions options = null);

        /// <summary>
        /// 读保持寄存器
        /// </summary>
        /// <param name="slaveId">从机地址</param>
        /// <param name="startAddress">起始地址</param>
        /// <param name="length">读取长度</param>
        /// <param name="timeout">超时时长(ms)</param>
        /// <param name="options">发送可选参数</param>
        /// <returns></returns>
        Reply<ReadHoldRegisterResponse> ReadHoldRegister(byte slaveId, ushort startAddress, ushort length, int timeout = 3000, SendOptions options = null);

        /// <summary>
        /// 读输入寄存器
        /// </summary>
        /// <param name="request">读输入寄存器的请求实体</param>
        /// <param name="timeout">超时时长(ms)</param>
        /// <param name="options">发送可选参数</param>
        /// <returns></returns>
        Reply<ReadInputRegisterResponse> ReadInputRegister(ReadInputRegisterRequest request, int timeout = 3000, SendOptions options = null);

        /// <summary>
        /// 读输入寄存器
        /// </summary>
        /// <param name="slaveId">从机地址</param>
        /// <param name="startAddress">起始地址</param>
        /// <param name="length">读取长度</param>
        /// <param name="timeout">超时时长(ms)</param>
        /// <param name="options">发送可选参数</param>
        /// <returns></returns>
        Reply<ReadInputRegisterResponse> ReadInputRegister(byte slaveId, ushort startAddress, ushort length, int timeout = 3000, SendOptions options = null);

        /// <summary>
        /// 写单个线圈寄存器
        /// </summary>
        /// <param name="request">写单个线圈寄存器的请求实体</param>
        /// <param name="timeout">超时时长(ms)</param>
        /// <param name="options">发送可选参数</param>
        /// <returns></returns>
        Reply<WriteSingleCoilRegisterResponse> WriteSingleCoilRegister(WriteSingleCoilRegisterRequest request, int timeout = 3000, SendOptions options = null);

        /// <summary>
        /// 写单个线圈寄存器
        /// </summary>
        /// <param name="slaveId">从站地址</param>
        /// <param name="writeAddress">写入地址</param>
        /// <param name="data">写入值</param>
        /// <param name="timeout">超长时长(ms)</param>
        /// <param name="options">发送可选参数</param>
        /// <returns></returns>
        Reply<WriteSingleCoilRegisterResponse> WriteSingleCoilRegister(byte slaveId, ushort writeAddress, bool data, int timeout = 3000, SendOptions options = null);

        /// <summary>
        /// 写单个保持寄存器
        /// </summary>
        /// <param name="request">写单个保持寄存器的请求实体</param>
        /// <param name="timeout">超时时长(ms)</param>
        /// <param name="options">发送可选参数</param>
        /// <returns></returns>
        Reply<WriteSingleHoldRegisterResponse> WriteSingleHoldRegister(WriteSingleHoldRegisterRequest request, int timeout = 3000, SendOptions options = null);

        /// <summary>
        /// 写单个保持寄存器
        /// </summary>
        /// <param name="slaveId">从站地址</param>
        /// <param name="writeAddress">写入地址</param>
        /// <param name="data">写入值</param>
        /// <param name="timeout">超时时长(ms)</param>
        /// <param name="options">发送可选参数</param>
        /// <returns></returns>
        Reply<WriteSingleHoldRegisterResponse> WriteSingleHoldRegister(byte slaveId, ushort writeAddress, byte[] data, int timeout = 3000, SendOptions options = null);

        /// <summary>
        /// 写多个线圈寄存器
        /// </summary>
        /// <param name="request">写多个线圈寄存器的请求实体</param>
        /// <param name="timeout">超时时长(ms)</param>
        /// <param name="options">发送可选参数</param>
        /// <returns></returns>
        Reply<WriteMultipleCoilRegistersResponse> WriteMultipleCoilRegisters(WriteMultipleCoilRegistersRequest request, int timeout = 3000, SendOptions options = null);

        /// <summary>
        /// 写多个线圈寄存器
        /// </summary>
        /// <param name="slaveId">从站地址</param>
        /// <param name="writeAddress">写入地址</param>
        /// <param name="data">写入值</param>
        /// <param name="timeout">超时时长(ms)</param>
        /// <param name="options">发送可选参数</param>
        /// <returns></returns>
        Reply<WriteMultipleCoilRegistersResponse> WriteMultipleCoilRegisters(byte slaveId, ushort writeAddress, bool[] data, int timeout = 3000, SendOptions options = null);

        /// <summary>
        /// 写多个保持寄存器
        /// </summary>
        /// <param name="request">写多个保持寄存器的请求实体</param>
        /// <param name="timeout">超时时长(ms)</param>
        /// <param name="options">发送可选参数</param>
        /// <returns></returns>
        Reply<WriteMultipleHoldRegistersResponse> WriteMultipleHoldRegisters(WriteMultipleHoldRegistersRequest request, int timeout = 3000, SendOptions options = null);

        /// <summary>
        /// 写入多个保持寄存器
        /// </summary>
        /// <param name="slaveId">从站地址</param>
        /// <param name="writeAddress">写入地址</param>
        /// <param name="writeLength">写入长度</param>
        /// <param name="data">写入数据</param>
        /// <param name="timeout">超时时长(ms)</param>
        /// <param name="options">发送可选参数</param>
        /// <returns></returns>
        Reply<WriteMultipleHoldRegistersResponse> WriteMultipleHoldRegisters(byte slaveId, ushort writeAddress, ushort writeLength, byte[] data, int timeout = 3000, SendOptions options = null);
    }
}
