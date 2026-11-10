using STTech.BytesIO.Core;
using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace STTech.BytesIO.Modbus
{
    /// <summary>
    /// Modbus 响应实体基类
    /// </summary>
    public class ModbusResponse : Response
    {
        private byte[] payload;

        /// <summary>
        /// 事务元标识符 (用于 MBAP / Modbus TCP 全双工请求-响应匹配)
        /// </summary>
        [Description("事务元标识符")]
        public ushort TransactionId { get; set; }

        /// <summary>
        /// 协议标识符 (MBAP 模式下固定为 0x0000)
        /// </summary>
        [Description("协议标识符")]
        public ushort ProtocolId { get; set; } = 0;

        /// <summary>
        /// 从机地址 / 单元标识符
        /// </summary>
        public ushort SlaveId { get; protected set; }

        /// <summary>
        /// 功能码
        /// </summary>
        public FunctionCode FunctionCode { get; protected set; }

        /// <summary>
        /// 校验码
        /// </summary>
        public ushort Checksum { get; protected set; }

        /// <summary>
        /// 有效载荷
        /// </summary>
        protected byte[] Payload
        {
            get
            {
                // 如果有故障码时获取Payload，则会抛出错误码异常
                if (ErrorCode != ModbusErrorCode.NoError)
                {
                    throw new InvalidOperationException($"Unable to acquire payload. (ErrorCode = {ErrorCode})", new ModbusErrorCodeException(ErrorCode));
                }

                return payload;
            }
            set => payload = value;
        }

        /// <summary>
        /// 协议格式
        /// </summary>
        public ModbusProtocolFormat ProtocolFormat { get; protected set; }

        /// <summary>
        /// 请求是否成功
        /// </summary>
        public bool IsSuccess { get; protected set; } = true;

        /// <summary>
        /// 故障码
        /// </summary>
        public ModbusErrorCode ErrorCode { get; protected set; }

        /// <summary>
        /// 初始化 <see cref="ModbusResponse"/> 类的新实例。
        /// </summary>
        public ModbusResponse() : base(new byte[0])
        { 
        }

        /// <summary>
        /// 关联的解包上下文
        /// </summary>
        public STTech.BytesIO.Core.Component.UnpackContext UnpackContext { get; protected set; }

        /// <summary>
        /// 构造 Modbus 响应对象
        /// </summary>
        public ModbusResponse(ushort slaveId, FunctionCode functionCode, ModbusProtocolFormat format) : base(new byte[0])
        {
            SlaveId = slaveId;
            FunctionCode = functionCode;
            ProtocolFormat = format;
            IsSuccess = true;
            ErrorCode = ModbusErrorCode.NoError;
        }

        /// <summary>
        /// 构造 Modbus 响应对象 (含错误码)
        /// </summary>
        public ModbusResponse(ushort slaveId, FunctionCode functionCode, ModbusProtocolFormat format, ModbusErrorCode errorCode) : base(new byte[0])
        {
            SlaveId = slaveId;
            FunctionCode = functionCode;
            ProtocolFormat = format;
            IsSuccess = false;
            ErrorCode = errorCode;
        }

        /// <summary>
        /// 构造 Modbus 响应对象 (基于字节数组)
        /// </summary>
        public ModbusResponse(byte[] data) : base(data ?? new byte[0])
        {
            if (data == null || data.Length == 0) return;
            ParseBytes(data, null);
        }

        /// <summary>
        /// 构造 Modbus 响应对象 (基于字节数组和指定协议格式)
        /// </summary>
        public ModbusResponse(byte[] data, ModbusProtocolFormat format) : base(data ?? new byte[0])
        {
            ProtocolFormat = format;
            if (data == null || data.Length == 0) return;
            ParseBytes(data, format);
        }

        /// <summary>
        /// 构造 Modbus 响应对象 (基于解包上下文)
        /// </summary>
        public ModbusResponse(STTech.BytesIO.Core.Component.UnpackContext context) : this(context, ModbusProtocolFormat.RTU)
        {
        }

        /// <summary>
        /// 构造 Modbus 响应对象 (基于解包上下文和指定协议格式)
        /// </summary>
        public ModbusResponse(STTech.BytesIO.Core.Component.UnpackContext context, ModbusProtocolFormat format) : base(context)
        {
            UnpackContext = context;
            ProtocolFormat = format;
            int len = (int)context.Data.Length;
            if (len <= 0) return;

            const int MaxStackAlloc = 256;
            byte[] rented = null;
            Span<byte> bytes = len <= MaxStackAlloc ? stackalloc byte[len] : (rented = ArrayPool<byte>.Shared.Rent(len));
            try
            {
                context.Data.CopyTo(bytes);
                ParseBytes(bytes.Slice(0, len), format);
            }
            finally
            {
                if (rented != null)
                {
                    ArrayPool<byte>.Shared.Return(rented);
                }
            }
        }

        private void ParseBytes(ReadOnlySpan<byte> bytes, ModbusProtocolFormat? specifiedFormat)
        {
            ModbusProtocolFormat fmt;
            if (specifiedFormat.HasValue)
            {
                fmt = specifiedFormat.Value;
            }
            else
            {
                // 仅在未指定格式时做基础 ASCII 冒号检测，绝不使用魔数盲猜 MBAP
                fmt = bytes[0] == (byte)':' ? ModbusProtocolFormat.ASCII : ModbusProtocolFormat.RTU;
            }
            ProtocolFormat = fmt;

            switch (fmt)
            {
                case ModbusProtocolFormat.ASCII:
                    {
                        if (bytes.Length < 11) return;
                        string hexString = bytes.Slice(1, bytes.Length - 3).ToArray().EncodeToString();

                        SlaveId = Convert.ToByte(hexString.Substring(0, 2), 16);
                        byte codeVal = Convert.ToByte(hexString.Substring(2, 2), 16);

                        if (codeVal >= 0x80)
                        {
                            IsSuccess = false;
                            FunctionCode = (FunctionCode)(codeVal - 0x80);
                            ErrorCode = (ModbusErrorCode)Convert.ToByte(hexString.Substring(4, 2), 16);
                        }
                        else
                        {
                            FunctionCode = (FunctionCode)codeVal;
                            int payloadHexLen = hexString.Length - 4 - 2;
                            if (payloadHexLen > 0)
                            {
                                payload = hexString.Substring(4, payloadHexLen).HexStringToBytes();
                            }
                        }
                        Checksum = Convert.ToByte(hexString.Substring(hexString.Length - 2, 2), 16);
                        break;
                    }

                case ModbusProtocolFormat.MBAP:
                    {
                        if (bytes.Length < 7) return;
                        TransactionId = BinaryPrimitives.ReadUInt16BigEndian(bytes.Slice(0, 2));
                        ProtocolId = BinaryPrimitives.ReadUInt16BigEndian(bytes.Slice(2, 2));
                        SlaveId = bytes[6];

                        if (bytes.Length >= 8)
                        {
                            byte codeVal = bytes[7];
                            if (codeVal >= 0x80)
                            {
                                IsSuccess = false;
                                FunctionCode = (FunctionCode)(codeVal - 0x80);
                                if (bytes.Length >= 9)
                                {
                                    ErrorCode = (ModbusErrorCode)bytes[8];
                                }
                            }
                            else
                            {
                                IsSuccess = true;
                                FunctionCode = (FunctionCode)codeVal;
                                int payloadLen = bytes.Length - 8;
                                if (payloadLen > 0)
                                {
                                    payload = bytes.Slice(8, payloadLen).ToArray();
                                }
                            }
                        }
                        break;
                    }

                case ModbusProtocolFormat.RTU:
                default:
                    {
                        if (bytes.Length < 4) return;
                        SlaveId = bytes[0];

                        if (bytes.Length == 5 && bytes[1] >= 0x80)
                        {
                            IsSuccess = false;
                            FunctionCode = (FunctionCode)(bytes[1] - 0x80);
                            ErrorCode = (ModbusErrorCode)bytes[2];
                            Checksum = (ushort)(bytes[3] | (bytes[4] << 8));
                            return;
                        }

                        FunctionCode = (FunctionCode)bytes[1];
                        int payloadLen = bytes.Length - 4;
                        if (payloadLen > 0)
                        {
                            payload = bytes.Slice(2, payloadLen).ToArray();
                        }
                        Checksum = (ushort)(bytes[bytes.Length - 2] | (bytes[bytes.Length - 1] << 8));
                        break;
                    }
            }
        }

        /// <summary>
        /// 序列化载荷数据处理
        /// </summary>
        protected internal virtual void SerializePayloadHandle()
        {
        }

        /// <summary>
        /// 获取序列化后的完整报文字节数组
        /// </summary>
        /// <returns>字节数组</returns>
        public byte[] GetBytes()
        {
            switch (ProtocolFormat)
            {
                case ModbusProtocolFormat.MBAP:
                    {
                        if (!IsSuccess)
                        {
                            byte[] buffer = new byte[6 + 3];
                            BinaryPrimitives.WriteUInt16BigEndian(buffer.AsSpan(0, 2), TransactionId);
                            BinaryPrimitives.WriteUInt16BigEndian(buffer.AsSpan(2, 2), ProtocolId);
                            BinaryPrimitives.WriteUInt16BigEndian(buffer.AsSpan(4, 2), 3);
                            buffer[6] = (byte)SlaveId;
                            buffer[7] = (byte)((byte)FunctionCode | 0x80);
                            buffer[8] = (byte)ErrorCode;
                            return buffer;
                        }
                        else
                        {
                            SerializePayloadHandle();
                            int payloadLen = payload?.Length ?? 0;
                            int lengthField = 1 + 1 + payloadLen;
                            byte[] buffer = new byte[6 + lengthField];
                            BinaryPrimitives.WriteUInt16BigEndian(buffer.AsSpan(0, 2), TransactionId);
                            BinaryPrimitives.WriteUInt16BigEndian(buffer.AsSpan(2, 2), ProtocolId);
                            BinaryPrimitives.WriteUInt16BigEndian(buffer.AsSpan(4, 2), (ushort)lengthField);
                            buffer[6] = (byte)SlaveId;
                            buffer[7] = (byte)FunctionCode;
                            if (payloadLen > 0)
                            {
                                Buffer.BlockCopy(payload, 0, buffer, 8, payloadLen);
                            }
                            return buffer;
                        }
                    }
                case ModbusProtocolFormat.RTU:
                    {
                        if (!IsSuccess)
                        {
                            byte[] buffer = new byte[5];
                            buffer[0] = (byte)SlaveId;
                            buffer[1] = (byte)((byte)FunctionCode | 0x80);
                            buffer[2] = (byte)ErrorCode;
                            ushort crc = ModbusCrc16.Compute(buffer.AsSpan(0, 3));
                            buffer[3] = (byte)(crc & 0xFF);
                            buffer[4] = (byte)(crc >> 8);
                            return buffer;
                        }
                        else
                        {
                            SerializePayloadHandle();
                            int payloadLen = payload?.Length ?? 0;
                            byte[] buffer = new byte[1 + 1 + payloadLen + 2];
                            buffer[0] = (byte)SlaveId;
                            buffer[1] = (byte)FunctionCode;
                            if (payloadLen > 0)
                            {
                                Buffer.BlockCopy(payload, 0, buffer, 2, payloadLen);
                            }
                            ushort crc = ModbusCrc16.Compute(buffer.AsSpan(0, 2 + payloadLen));
                            buffer[buffer.Length - 2] = (byte)(crc & 0xFF);
                            buffer[buffer.Length - 1] = (byte)(crc >> 8);
                            return buffer;
                        }
                    }
                case ModbusProtocolFormat.ASCII:
                    {
                        string line;
                        if (!IsSuccess)
                        {
                            line = $":{SlaveId:X2}{(byte)((byte)FunctionCode | 0x80):X2}{(byte)ErrorCode:X2}";
                        }
                        else
                        {
                            SerializePayloadHandle();
                            line = $":{SlaveId:X2}{(byte)FunctionCode:X2}{payload?.ToHexString() ?? ""}";
                        }

                        line = line + ModbusRequest.LRC(line) + "\r\n";
                        return line.GetBytes();
                    }
            }
            return null;
        }
    }
}
