using STTech.BytesIO.Core;
using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace STTech.BytesIO.Modbus
{
    /// <summary>
    /// Modbus请求类
    /// </summary>
    public abstract class ModbusRequest : IRequest
    {
        public ModbusRequest() { }

        /// <summary>
        /// 关联的解包上下文
        /// </summary>
        public STTech.BytesIO.Core.Component.UnpackContext UnpackContext { get; protected set; }

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
        /// 从机地址 / 单元标识符 (Unit ID)
        /// </summary>
        [Description("从机地址")]
        public byte SlaveId { get; set; } = 1;

        /// <summary>
        /// 功能码
        /// </summary>
        [Description("功能码")]
        public FunctionCode FunctionCode { get; protected set; }

        /// <summary>
        /// 构造 Modbus 请求对象
        /// </summary>
        /// <param name="functionCode">功能码</param>
        protected ModbusRequest(FunctionCode functionCode)
        {
            FunctionCode = functionCode;
        }

        /// <summary>
        /// 协议格式
        /// </summary>
        public ModbusProtocolFormat ProtocolFormat { get; set; }

        /// <summary>
        /// 有效荷载
        /// </summary>
        protected byte[] Payload { get; set; }

        /// <summary>
        /// 校验码
        /// </summary>
        public ushort Checksum { get; internal set; }

        /// <summary>
        /// 构造 Modbus 请求对象 (基于字节数组)
        /// </summary>
        /// <param name="data">原始报文数据</param>
        protected ModbusRequest(byte[] data) : this(data, null)
        {
        }

        /// <summary>
        /// 构造 Modbus 请求对象 (基于字节数组和指定协议格式)
        /// </summary>
        /// <param name="data">原始报文数据</param>
        /// <param name="format">指定协议格式 (若为 null 则自动检测 ASCII 冒号，否则默认 RTU)</param>
        protected ModbusRequest(byte[] data, ModbusProtocolFormat? format)
        {
            if (data == null || data.Length == 0) return;
            ParseBytes(data, format);
        }

        /// <summary>
        /// 构造 Modbus 请求对象 (基于解包上下文)
        /// </summary>
        /// <param name="context">解包上下文</param>
        protected ModbusRequest(STTech.BytesIO.Core.Component.UnpackContext context) : this(context, null)
        {
        }

        /// <summary>
        /// 构造 Modbus 请求对象 (基于解包上下文和指定协议格式)
        /// </summary>
        /// <param name="context">解包上下文</param>
        /// <param name="format">指定协议格式</param>
        protected ModbusRequest(STTech.BytesIO.Core.Component.UnpackContext context, ModbusProtocolFormat? format)
        {
            UnpackContext = context;
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
                    if (bytes.Length < 11) return; // : + SlaveId(2) + Func(2) + LRC(2) + CRLF(2) = 11
                    string hexString = bytes.Slice(1, bytes.Length - 3).ToArray().EncodeToString(); // 去除 : 和 CRLF

                    SlaveId = Convert.ToByte(hexString.Substring(0, 2), 16);
                    FunctionCode = (FunctionCode)Convert.ToByte(hexString.Substring(2, 2), 16);

                    int payloadHexLen = hexString.Length - 4 - 2; // 去除 SlaveId(2) 和 Func(2) 和 LRC(2)
                    if (payloadHexLen > 0)
                    {
                        Payload = hexString.Substring(4, payloadHexLen).HexStringToBytes();
                    }
                    Checksum = Convert.ToByte(hexString.Substring(hexString.Length - 2, 2), 16);
                    break;

                case ModbusProtocolFormat.MBAP:
                    if (bytes.Length < 8) return;
                    TransactionId = BinaryPrimitives.ReadUInt16BigEndian(bytes.Slice(0, 2));
                    ProtocolId = BinaryPrimitives.ReadUInt16BigEndian(bytes.Slice(2, 2));
                    ushort length = BinaryPrimitives.ReadUInt16BigEndian(bytes.Slice(4, 2));
                    SlaveId = bytes[6];
                    FunctionCode = (FunctionCode)bytes[7];
                    int mbapPayloadLen = bytes.Length - 8;
                    if (mbapPayloadLen > 0)
                    {
                        Payload = bytes.Slice(8, mbapPayloadLen).ToArray();
                    }
                    break;

                case ModbusProtocolFormat.RTU:
                default:
                    if (bytes.Length < 4) return;
                    SlaveId = bytes[0];
                    FunctionCode = (FunctionCode)bytes[1];
                    int rtuPayloadLen = bytes.Length - 4;
                    if (rtuPayloadLen > 0)
                    {
                        Payload = bytes.Slice(2, rtuPayloadLen).ToArray();
                    }
                    Checksum = (ushort)(bytes[bytes.Length - 2] | (bytes[bytes.Length - 1] << 8));
                    break;
            }
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <returns></returns>
        public byte[] GetBytes()
        {
            switch (ProtocolFormat)
            {
                case ModbusProtocolFormat.MBAP:
                    {
                        SerializePayloadHandle();
                        int payloadLen = Payload?.Length ?? 0;
                        int lengthField = 1 + 1 + payloadLen; // UnitId(1) + FuncCode(1) + Payload
                        byte[] buffer = new byte[6 + lengthField];

                        BinaryPrimitives.WriteUInt16BigEndian(buffer.AsSpan(0, 2), TransactionId);
                        BinaryPrimitives.WriteUInt16BigEndian(buffer.AsSpan(2, 2), ProtocolId);
                        BinaryPrimitives.WriteUInt16BigEndian(buffer.AsSpan(4, 2), (ushort)lengthField);
                        buffer[6] = SlaveId;
                        buffer[7] = (byte)FunctionCode;

                        if (payloadLen > 0)
                        {
                            Buffer.BlockCopy(Payload, 0, buffer, 8, payloadLen);
                        }
                        return buffer;
                    }
                case ModbusProtocolFormat.RTU:
                    {
                        SerializePayloadHandle();
                        int payloadLen = Payload?.Length ?? 0;
                        byte[] buffer = new byte[1 + 1 + payloadLen + 2];
                        buffer[0] = SlaveId;
                        buffer[1] = (byte)FunctionCode;
                        if (payloadLen > 0)
                        {
                            Buffer.BlockCopy(Payload, 0, buffer, 2, payloadLen);
                        }
                        ushort crc = ModbusCrc16.Compute(buffer.AsSpan(0, 2 + payloadLen));
                        buffer[buffer.Length - 2] = (byte)(crc & 0xFF);
                        buffer[buffer.Length - 1] = (byte)(crc >> 8);
                        return buffer;
                    }
                case ModbusProtocolFormat.ASCII:
                    {
                        SerializePayloadHandle();
                        var line = $":{SlaveId:X2}{(byte)FunctionCode:X2}{Payload?.ToHexString() ?? ""}";
                        line = line + LRC(line) + "\r\n";
                        return line.GetBytes();
                    }
            }
            return null;
        }

        /// <summary>
        /// 序列化有效载荷的方法
        /// </summary>
        protected internal abstract void SerializePayloadHandle();

        /// <summary>
        /// 反序列化有效载荷的方法
        /// </summary>
        protected internal virtual void DeserializePayloadHandle() { }

        /// <summary>
        /// ASCII模式校验和
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public static string LRC(string message)
        {
            int sum = 0;
            int strLen = message.Length;
            for (int i = 1; i < strLen - 1; i += 2)
            {
                string temp = message.Substring(i, 2);
                sum += Convert.ToByte(temp, 16);
            }
            byte lrc = (byte)(-(byte)sum);
            return lrc.ToString("X2");
        }

        /// <summary>
        /// RTU模式CRC16校验
        /// </summary>
        /// <param name="value"></param>
        /// <param name="poly"></param>
        /// <param name="crcInit"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        internal static byte[] CRC16(byte[] value, ushort poly = 0xA001, ushort crcInit = 0xFFFF)
        {
            if (value == null || value.Length == 0)
                throw new ArgumentException("Value cannot be null or empty.", nameof(value));

            ushort crc = ModbusCrc16.Compute(value);
            byte lo = (byte)(crc & 0xFF);
            byte hi = (byte)(crc >> 8);
            byte[] result = new byte[value.Length + 2];
            Buffer.BlockCopy(value, 0, result, 0, value.Length);
            result[result.Length - 2] = lo;
            result[result.Length - 1] = hi;
            return result;
        }
    }
}
