using STTech.BytesIO.Core;
using System;
using System.Text;
using System.Threading.Tasks;

namespace STTech.BytesIO.Modbus
{
    public abstract partial class ModbusClient
    {
        private static Reply<ModbusDataResponse<T>> WrapDataResponse<T>(Reply<ReadHoldingRegisterResponse> reply, int minByteLength, Func<byte[], T> converter)
        {
            var res = reply.GetResponse();
            if (!reply.IsCompleted || res == null)
            {
                return new Reply<ModbusDataResponse<T>>(reply.Client, reply.Status, reply.Exception);
            }

            if (!res.IsSuccess)
            {
                // 从机返回故障码，保留原始故障码，GetResponse() 返回包装了原始响应的实体，调用方可安全检查 IsSuccess 和 ErrorCode
                return new Reply<ModbusDataResponse<T>>(reply.Client, new ModbusDataResponse<T>(default, res));
            }

            if (res.Values == null || res.Values.Length < minByteLength)
            {
                return new Reply<ModbusDataResponse<T>>(reply.Client, ReplyStatus.Error, new FormatException($"从站返回数据长度不足 (期望至少 {minByteLength} 字节，实际 {res.Values?.Length ?? 0} 字节)"));
            }

            T val = converter(res.Values);
            return new Reply<ModbusDataResponse<T>>(reply.Client, new ModbusDataResponse<T>(val, res));
        }

        #region Float (Single, 2 Registers, 4 Bytes)

        /// <summary>
        /// 读取 32 位单精度浮点数 (float / Single)
        /// </summary>
        public Reply<ModbusDataResponse<float>> ReadFloat(byte slaveId, ushort startAddress, EndianFormat? format = null, int timeout = 3000, SendOptions options = null)
        {
            var reply = ReadHoldingRegister(slaveId, startAddress, 2, timeout, options);
            return WrapDataResponse(reply, 4, bytes => ModbusByteConverter.ToSingle(bytes, format ?? DefaultEndianFormat));
        }

        /// <summary>
        /// 异步读取 32 位单精度浮点数 (float / Single)
        /// </summary>
        public async Task<Reply<ModbusDataResponse<float>>> ReadFloatAsync(byte slaveId, ushort startAddress, EndianFormat? format = null, int timeout = 3000, SendOptions options = null)
        {
            var reply = await ReadHoldingRegisterAsync(slaveId, startAddress, 2, timeout, options).ConfigureAwait(false);
            return WrapDataResponse(reply, 4, bytes => ModbusByteConverter.ToSingle(bytes, format ?? DefaultEndianFormat));
        }

        /// <summary>
        /// 写入 32 位单精度浮点数 (float / Single)
        /// </summary>
        public Reply<WriteRegisterResponse> WriteFloat(byte slaveId, ushort startAddress, float value, EndianFormat? format = null, int timeout = 3000, SendOptions options = null)
        {
            byte[] bytes = ModbusByteConverter.GetBytes(value, format ?? DefaultEndianFormat);
            return WriteMultipleHoldingRegisters(slaveId, startAddress, bytes, timeout, options);
        }

        /// <summary>
        /// 异步写入 32 位单精度浮点数 (float / Single)
        /// </summary>
        public Task<Reply<WriteRegisterResponse>> WriteFloatAsync(byte slaveId, ushort startAddress, float value, EndianFormat? format = null, int timeout = 3000, SendOptions options = null)
        {
            byte[] bytes = ModbusByteConverter.GetBytes(value, format ?? DefaultEndianFormat);
            return WriteMultipleHoldingRegistersAsync(slaveId, startAddress, bytes, timeout, options);
        }

        #endregion

        #region Int32 (int, 2 Registers, 4 Bytes)

        /// <summary>
        /// 读取 32 位有符号整数 (int / Int32)
        /// </summary>
        public Reply<ModbusDataResponse<int>> ReadInt32(byte slaveId, ushort startAddress, EndianFormat? format = null, int timeout = 3000, SendOptions options = null)
        {
            var reply = ReadHoldingRegister(slaveId, startAddress, 2, timeout, options);
            return WrapDataResponse(reply, 4, bytes => ModbusByteConverter.ToInt32(bytes, format ?? DefaultEndianFormat));
        }

        /// <summary>
        /// 异步读取 32 位有符号整数 (int / Int32)
        /// </summary>
        public async Task<Reply<ModbusDataResponse<int>>> ReadInt32Async(byte slaveId, ushort startAddress, EndianFormat? format = null, int timeout = 3000, SendOptions options = null)
        {
            var reply = await ReadHoldingRegisterAsync(slaveId, startAddress, 2, timeout, options).ConfigureAwait(false);
            return WrapDataResponse(reply, 4, bytes => ModbusByteConverter.ToInt32(bytes, format ?? DefaultEndianFormat));
        }

        /// <summary>
        /// 写入 32 位有符号整数 (int / Int32)
        /// </summary>
        public Reply<WriteRegisterResponse> WriteInt32(byte slaveId, ushort startAddress, int value, EndianFormat? format = null, int timeout = 3000, SendOptions options = null)
        {
            byte[] bytes = ModbusByteConverter.GetBytes(value, format ?? DefaultEndianFormat);
            return WriteMultipleHoldingRegisters(slaveId, startAddress, bytes, timeout, options);
        }

        /// <summary>
        /// 异步写入 32 位有符号整数 (int / Int32)
        /// </summary>
        public Task<Reply<WriteRegisterResponse>> WriteInt32Async(byte slaveId, ushort startAddress, int value, EndianFormat? format = null, int timeout = 3000, SendOptions options = null)
        {
            byte[] bytes = ModbusByteConverter.GetBytes(value, format ?? DefaultEndianFormat);
            return WriteMultipleHoldingRegistersAsync(slaveId, startAddress, bytes, timeout, options);
        }

        #endregion

        #region UInt32 (uint, 2 Registers, 4 Bytes)

        /// <summary>
        /// 读取 32 位无符号整数 (uint / UInt32)
        /// </summary>
        public Reply<ModbusDataResponse<uint>> ReadUInt32(byte slaveId, ushort startAddress, EndianFormat? format = null, int timeout = 3000, SendOptions options = null)
        {
            var reply = ReadHoldingRegister(slaveId, startAddress, 2, timeout, options);
            return WrapDataResponse(reply, 4, bytes => ModbusByteConverter.ToUInt32(bytes, format ?? DefaultEndianFormat));
        }

        /// <summary>
        /// 异步读取 32 位无符号整数 (uint / UInt32)
        /// </summary>
        public async Task<Reply<ModbusDataResponse<uint>>> ReadUInt32Async(byte slaveId, ushort startAddress, EndianFormat? format = null, int timeout = 3000, SendOptions options = null)
        {
            var reply = await ReadHoldingRegisterAsync(slaveId, startAddress, 2, timeout, options).ConfigureAwait(false);
            return WrapDataResponse(reply, 4, bytes => ModbusByteConverter.ToUInt32(bytes, format ?? DefaultEndianFormat));
        }

        /// <summary>
        /// 写入 32 位无符号整数 (uint / UInt32)
        /// </summary>
        public Reply<WriteRegisterResponse> WriteUInt32(byte slaveId, ushort startAddress, uint value, EndianFormat? format = null, int timeout = 3000, SendOptions options = null)
        {
            byte[] bytes = ModbusByteConverter.GetBytes(value, format ?? DefaultEndianFormat);
            return WriteMultipleHoldingRegisters(slaveId, startAddress, bytes, timeout, options);
        }

        /// <summary>
        /// 异步写入 32 位无符号整数 (uint / UInt32)
        /// </summary>
        public Task<Reply<WriteRegisterResponse>> WriteUInt32Async(byte slaveId, ushort startAddress, uint value, EndianFormat? format = null, int timeout = 3000, SendOptions options = null)
        {
            byte[] bytes = ModbusByteConverter.GetBytes(value, format ?? DefaultEndianFormat);
            return WriteMultipleHoldingRegistersAsync(slaveId, startAddress, bytes, timeout, options);
        }

        #endregion

        #region Double (8 Bytes, 4 Registers)

        /// <summary>
        /// 读取 64 位双精度浮点数 (double)
        /// </summary>
        public Reply<ModbusDataResponse<double>> ReadDouble(byte slaveId, ushort startAddress, EndianFormat? format = null, int timeout = 3000, SendOptions options = null)
        {
            var reply = ReadHoldingRegister(slaveId, startAddress, 4, timeout, options);
            return WrapDataResponse(reply, 8, bytes => ModbusByteConverter.ToDouble(bytes, format ?? DefaultEndianFormat));
        }

        /// <summary>
        /// 异步读取 64 位双精度浮点数 (double)
        /// </summary>
        public async Task<Reply<ModbusDataResponse<double>>> ReadDoubleAsync(byte slaveId, ushort startAddress, EndianFormat? format = null, int timeout = 3000, SendOptions options = null)
        {
            var reply = await ReadHoldingRegisterAsync(slaveId, startAddress, 4, timeout, options).ConfigureAwait(false);
            return WrapDataResponse(reply, 8, bytes => ModbusByteConverter.ToDouble(bytes, format ?? DefaultEndianFormat));
        }

        /// <summary>
        /// 写入 64 位双精度浮点数 (double)
        /// </summary>
        public Reply<WriteRegisterResponse> WriteDouble(byte slaveId, ushort startAddress, double value, EndianFormat? format = null, int timeout = 3000, SendOptions options = null)
        {
            byte[] bytes = ModbusByteConverter.GetBytes(value, format ?? DefaultEndianFormat);
            return WriteMultipleHoldingRegisters(slaveId, startAddress, bytes, timeout, options);
        }

        /// <summary>
        /// 异步写入 64 位双精度浮点数 (double)
        /// </summary>
        public Task<Reply<WriteRegisterResponse>> WriteDoubleAsync(byte slaveId, ushort startAddress, double value, EndianFormat? format = null, int timeout = 3000, SendOptions options = null)
        {
            byte[] bytes = ModbusByteConverter.GetBytes(value, format ?? DefaultEndianFormat);
            return WriteMultipleHoldingRegistersAsync(slaveId, startAddress, bytes, timeout, options);
        }

        #endregion

        #region Int64 (long, 8 Bytes, 4 Registers)

        /// <summary>
        /// 读取 64 位长整数 (long / Int64)
        /// </summary>
        public Reply<ModbusDataResponse<long>> ReadInt64(byte slaveId, ushort startAddress, EndianFormat? format = null, int timeout = 3000, SendOptions options = null)
        {
            var reply = ReadHoldingRegister(slaveId, startAddress, 4, timeout, options);
            return WrapDataResponse(reply, 8, bytes => ModbusByteConverter.ToInt64(bytes, format ?? DefaultEndianFormat));
        }

        /// <summary>
        /// 异步读取 64 位长整数 (long / Int64)
        /// </summary>
        public async Task<Reply<ModbusDataResponse<long>>> ReadInt64Async(byte slaveId, ushort startAddress, EndianFormat? format = null, int timeout = 3000, SendOptions options = null)
        {
            var reply = await ReadHoldingRegisterAsync(slaveId, startAddress, 4, timeout, options).ConfigureAwait(false);
            return WrapDataResponse(reply, 8, bytes => ModbusByteConverter.ToInt64(bytes, format ?? DefaultEndianFormat));
        }

        /// <summary>
        /// 写入 64 位长整数 (long / Int64)
        /// </summary>
        public Reply<WriteRegisterResponse> WriteInt64(byte slaveId, ushort startAddress, long value, EndianFormat? format = null, int timeout = 3000, SendOptions options = null)
        {
            byte[] bytes = ModbusByteConverter.GetBytes(value, format ?? DefaultEndianFormat);
            return WriteMultipleHoldingRegisters(slaveId, startAddress, bytes, timeout, options);
        }

        /// <summary>
        /// 异步写入 64 位长整数 (long / Int64)
        /// </summary>
        public Task<Reply<WriteRegisterResponse>> WriteInt64Async(byte slaveId, ushort startAddress, long value, EndianFormat? format = null, int timeout = 3000, SendOptions options = null)
        {
            byte[] bytes = ModbusByteConverter.GetBytes(value, format ?? DefaultEndianFormat);
            return WriteMultipleHoldingRegistersAsync(slaveId, startAddress, bytes, timeout, options);
        }

        #endregion

        #region UInt64 (ulong, 8 Bytes, 4 Registers)

        /// <summary>
        /// 读取 64 位无符号长整数 (ulong / UInt64)
        /// </summary>
        public Reply<ModbusDataResponse<ulong>> ReadUInt64(byte slaveId, ushort startAddress, EndianFormat? format = null, int timeout = 3000, SendOptions options = null)
        {
            var reply = ReadHoldingRegister(slaveId, startAddress, 4, timeout, options);
            return WrapDataResponse(reply, 8, bytes => ModbusByteConverter.ToUInt64(bytes, format ?? DefaultEndianFormat));
        }

        /// <summary>
        /// 异步读取 64 位无符号长整数 (ulong / UInt64)
        /// </summary>
        public async Task<Reply<ModbusDataResponse<ulong>>> ReadUInt64Async(byte slaveId, ushort startAddress, EndianFormat? format = null, int timeout = 3000, SendOptions options = null)
        {
            var reply = await ReadHoldingRegisterAsync(slaveId, startAddress, 4, timeout, options).ConfigureAwait(false);
            return WrapDataResponse(reply, 8, bytes => ModbusByteConverter.ToUInt64(bytes, format ?? DefaultEndianFormat));
        }

        /// <summary>
        /// 写入 64 位无符号长整数 (ulong / UInt64)
        /// </summary>
        public Reply<WriteRegisterResponse> WriteUInt64(byte slaveId, ushort startAddress, ulong value, EndianFormat? format = null, int timeout = 3000, SendOptions options = null)
        {
            byte[] bytes = ModbusByteConverter.GetBytes(value, format ?? DefaultEndianFormat);
            return WriteMultipleHoldingRegisters(slaveId, startAddress, bytes, timeout, options);
        }

        /// <summary>
        /// 异步写入 64 位无符号长整数 (ulong / UInt64)
        /// </summary>
        public Task<Reply<WriteRegisterResponse>> WriteUInt64Async(byte slaveId, ushort startAddress, ulong value, EndianFormat? format = null, int timeout = 3000, SendOptions options = null)
        {
            byte[] bytes = ModbusByteConverter.GetBytes(value, format ?? DefaultEndianFormat);
            return WriteMultipleHoldingRegistersAsync(slaveId, startAddress, bytes, timeout, options);
        }

        #endregion

        #region String (Registers to String)

        /// <summary>
        /// 读取字符串
        /// </summary>
        public Reply<ModbusDataResponse<string>> ReadString(byte slaveId, ushort startAddress, ushort registerLength, Encoding encoding = null, int timeout = 3000, SendOptions options = null)
        {
            var reply = ReadHoldingRegister(slaveId, startAddress, registerLength, timeout, options);
            return WrapDataResponse(reply, registerLength * 2, bytes => ModbusByteConverter.ToString(bytes, encoding));
        }

        /// <summary>
        /// 异步读取字符串
        /// </summary>
        public async Task<Reply<ModbusDataResponse<string>>> ReadStringAsync(byte slaveId, ushort startAddress, ushort registerLength, Encoding encoding = null, int timeout = 3000, SendOptions options = null)
        {
            var reply = await ReadHoldingRegisterAsync(slaveId, startAddress, registerLength, timeout, options).ConfigureAwait(false);
            return WrapDataResponse(reply, registerLength * 2, bytes => ModbusByteConverter.ToString(bytes, encoding));
        }

        /// <summary>
        /// 写入字符串
        /// </summary>
        public Reply<WriteRegisterResponse> WriteString(byte slaveId, ushort startAddress, string value, Encoding encoding = null, int timeout = 3000, SendOptions options = null)
        {
            encoding = encoding ?? Encoding.ASCII;
            byte[] rawBytes = encoding.GetBytes(value ?? string.Empty);
            int regCount = (rawBytes.Length + 1) / 2;
            byte[] buffer = new byte[regCount * 2];
            Buffer.BlockCopy(rawBytes, 0, buffer, 0, rawBytes.Length);
            return WriteMultipleHoldingRegisters(slaveId, startAddress, buffer, timeout, options);
        }

        /// <summary>
        /// 异步写入字符串
        /// </summary>
        public Task<Reply<WriteRegisterResponse>> WriteStringAsync(byte slaveId, ushort startAddress, string value, Encoding encoding = null, int timeout = 3000, SendOptions options = null)
        {
            encoding = encoding ?? Encoding.ASCII;
            byte[] rawBytes = encoding.GetBytes(value ?? string.Empty);
            int regCount = (rawBytes.Length + 1) / 2;
            byte[] buffer = new byte[regCount * 2];
            Buffer.BlockCopy(rawBytes, 0, buffer, 0, rawBytes.Length);
            return WriteMultipleHoldingRegistersAsync(slaveId, startAddress, buffer, timeout, options);
        }

        #endregion
    }
}
