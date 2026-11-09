using STTech.BytesIO.Core;
using STTech.BytesIO.Core.Component;
using System;
using System.Buffers;
using System.Buffers.Binary;

namespace STTech.BytesIO.Modbus
{
    /// <summary>
    /// Modbus 协议响应解包器 (用于客户端接收从机响应)
    /// </summary>
    public class ModbusUnpacker : Unpacker<ModbusResponse>
    {
        private const int checkSumLen = 2;
        private const int rtuSlaveIdLen = 1;
        private const int rtuFunctionCodeLen = 1;
        private const int rtuFixedHead = rtuSlaveIdLen + rtuFunctionCodeLen;

        /// <summary>
        /// Modbus 通信格式
        /// </summary>
        public ModbusProtocolFormat Format { get; internal set; }

        /// <summary>
        /// 构造 Modbus 响应解包器
        /// </summary>
        /// <param name="client">底层的通信客户端</param>
        /// <param name="format">协议格式</param>
        public ModbusUnpacker(ModbusClient client, ModbusProtocolFormat format) : base(client)
        {
            Format = format;
            InterruptFrameTimeoutValue = 100;

            if (Format == ModbusProtocolFormat.ASCII)
            {
                StartMark = new byte[] { (byte)':' };
            }
        }

        /// <inheritdoc/>
        protected override int CalculatePacketLength(ReadOnlySequence<byte> buffer)
        {
            switch (Format)
            {
                case ModbusProtocolFormat.MBAP:
                    return CalculateMbapPacketLengthHandler(ref buffer);
                case ModbusProtocolFormat.ASCII:
                    return CalculateAsciiPacketLengthHandler(ref buffer);
                case ModbusProtocolFormat.RTU:
                default:
                    return CalculateRtuPacketLengthHandler(ref buffer);
            }
        }

        private static int CalculateMbapPacketLengthHandler(ref ReadOnlySequence<byte> buffer)
        {
            // MBAP Header 为 7 字节
            if (buffer.Length < 7)
            {
                return 0;
            }

            Span<byte> head = stackalloc byte[7];
            buffer.Slice(0, 7).CopyTo(head);

            // 检查 Protocol ID (字节 2, 3 必须为 0x0000)
            ushort protocolId = BinaryPrimitives.ReadUInt16BigEndian(head.Slice(2, 2));
            if (protocolId != 0)
            {
                // 协议标识不匹配，非合法 MBAP 帧头，滑动跳过 1 字节自愈
                return -1;
            }

            // 读取 Length 字段 (字节 4, 5，后续字节数包含 UnitId + PDU)
            ushort length = BinaryPrimitives.ReadUInt16BigEndian(head.Slice(4, 2));
            if (length < 2 || length > 260)
            {
                // 长度字段异常 (UnitId+FuncCode 至少 2 字节，Modbus PDU 最大约 256 字节)
                return -1;
            }

            int totalLen = 6 + length;
            if (buffer.Length < totalLen)
            {
                return 0; // 尚未接收完整帧，等待后续数据
            }

            return totalLen;
        }

        private static int CalculateAsciiPacketLengthHandler(ref ReadOnlySequence<byte> buffer)
        {
            if (buffer.Length < 11)
            {
                return 0;
            }

            Span<byte> head = stackalloc byte[11];
            buffer.Slice(0, 11).CopyTo(head);

            // 起始必须是冒号 ':'
            if (head[0] != (byte)':')
            {
                return -1;
            }

            string codeStr = System.Text.Encoding.ASCII.GetString(head.Slice(3, 2).ToArray());
            if (!byte.TryParse(codeStr, System.Globalization.NumberStyles.HexNumber, null, out byte codeVal))
            {
                return -1;
            }

            int packetLen;
            if (codeVal >= 0x80)
            {
                packetLen = 11; // : + SlaveId(2) + ErrorCode(2) + LRC(2) + CRLF(2) = 11
            }
            else
            {
                var code = (FunctionCode)codeVal;
                switch (code)
                {
                    case FunctionCode.ReadCoilRegister:
                    case FunctionCode.ReadDiscreteInputRegister:
                    case FunctionCode.ReadHoldingRegister:
                    case FunctionCode.ReadInputRegister:
                        string byteCountStr = System.Text.Encoding.ASCII.GetString(head.Slice(5, 2).ToArray());
                        if (!byte.TryParse(byteCountStr, System.Globalization.NumberStyles.HexNumber, null, out byte byteCount) || byteCount == 0 || byteCount > 250)
                        {
                            return -1;
                        }
                        packetLen = 11 + (byteCount * 2);
                        break;

                    case FunctionCode.WriteSingleCoilRegister:
                    case FunctionCode.WriteSingleHoldingRegister:
                    case FunctionCode.WriteMultipleCoilRegisters:
                    case FunctionCode.WriteMultipleHoldingRegisters:
                        packetLen = 17; // : + SlaveId(2) + Func(2) + Addr(4) + Value(4) + LRC(2) + CRLF(2) = 17
                        break;

                    default:
                        // 未知功能码，滑动跳过 1 字节自愈
                        return -1;
                }
            }

            if (buffer.Length < packetLen)
            {
                return 0; // 数据尚未接收完整
            }

            // 严格 LRC 校验
            Span<byte> frame = packetLen <= 256 ? stackalloc byte[packetLen] : new byte[packetLen];
            buffer.Slice(0, packetLen).CopyTo(frame);

            // 检查末尾回车换行
            if (frame[packetLen - 2] != (byte)'\r' || frame[packetLen - 1] != (byte)'\n')
            {
                return -1;
            }

            string frameStr = System.Text.Encoding.ASCII.GetString(frame.Slice(0, packetLen - 2).ToArray());
            string payloadWithColon = frameStr.Substring(0, frameStr.Length - 2);
            string expectedLrc = ModbusRequest.LRC(payloadWithColon);
            string actualLrc = frameStr.Substring(frameStr.Length - 2, 2);
            if (!string.Equals(expectedLrc, actualLrc, StringComparison.OrdinalIgnoreCase))
            {
                return -1; // LRC 校验失败
            }

            return packetLen;
        }

        private static int CalculateRtuPacketLengthHandler(ref ReadOnlySequence<byte> buffer)
        {
            if (buffer.Length < 5)
            {
                return 0;
            }

            Span<byte> head = stackalloc byte[5];
            buffer.Slice(0, 5).CopyTo(head);

            byte codeByte = head[1];

            // 错误/故障应答：SlaveId(1) + (FuncCode|0x80)(1) + ErrorCode(1) + CRC(2) = 5 字节
            if (codeByte >= 0x80)
            {
                int errorPacketLen = 5;
                if (buffer.Length < errorPacketLen) return 0;

                Span<byte> errFrame = stackalloc byte[errorPacketLen];
                buffer.Slice(0, errorPacketLen).CopyTo(errFrame);
                if (!ModbusCrc16.Check(errFrame))
                {
                    return -1; // CRC 不匹配，滑动丢弃 1 字节自愈
                }
                return errorPacketLen;
            }

            var code = (FunctionCode)codeByte;
            int packetLen;
            switch (code)
            {
                case FunctionCode.ReadCoilRegister:
                case FunctionCode.ReadDiscreteInputRegister:
                case FunctionCode.ReadHoldingRegister:
                case FunctionCode.ReadInputRegister:
                    // SlaveId(1) + Func(1) + ByteCount(1) + Data(ByteCount) + CRC(2)
                    int byteCount = head[2];
                    if (byteCount == 0 || byteCount > 250)
                    {
                        return -1; // 非法数据长度 (Modbus 规范上限 250 字节)，滑动丢弃 1 字节自愈
                    }
                    packetLen = 1 + 1 + 1 + byteCount + 2;
                    break;

                case FunctionCode.WriteSingleCoilRegister:
                case FunctionCode.WriteSingleHoldingRegister:
                case FunctionCode.WriteMultipleCoilRegisters:
                case FunctionCode.WriteMultipleHoldingRegisters:
                    // SlaveId(1) + Func(1) + Addr(2) + Value(2) + CRC(2) = 8 字节
                    packetLen = 8;
                    break;

                default:
                    // 无法识别的功能码，绝不抛出异常导致解包器死锁，滑动丢弃 1 字节自愈
                    return -1;
            }

            if (buffer.Length < packetLen)
            {
                return 0; // 尚未收齐整包
            }

            // 严格执行 CRC16 校验 (零堆分配)
            Span<byte> fullFrame = packetLen <= 256 ? stackalloc byte[packetLen] : new byte[packetLen];
            buffer.Slice(0, packetLen).CopyTo(fullFrame);
            if (!ModbusCrc16.Check(fullFrame))
            {
                // CRC 校验失败，说明开头字节不是有效帧头或数据受损，滑动跳过 1 字节
                return -1;
            }

            return packetLen;
        }

        /// <inheritdoc/>
        protected override ModbusResponse ResponseSerializeHandler(UnpackContext context)
        {
            var seq = context.Data;
            var format = Format;
            if (seq.Length < 2) return new ModbusResponse(context, format);

            FunctionCode functionCode = FunctionCode.ReadHoldingRegister;

            if (format == ModbusProtocolFormat.MBAP)
            {
                if (seq.Length >= 8)
                {
                    Span<byte> head8 = stackalloc byte[8];
                    seq.Slice(0, 8).CopyTo(head8);
                    byte codeVal = head8[7];
                    functionCode = codeVal >= 0x80 ? (FunctionCode)(codeVal - 0x80) : (FunctionCode)codeVal;
                }
            }
            else if (format == ModbusProtocolFormat.ASCII)
            {
                if (seq.Length >= 5)
                {
                    Span<byte> asciiTemp = stackalloc byte[5];
                    seq.Slice(0, 5).CopyTo(asciiTemp);
                    var codeStr = System.Text.Encoding.ASCII.GetString(asciiTemp.Slice(3, 2).ToArray());
                    if (byte.TryParse(codeStr, System.Globalization.NumberStyles.HexNumber, null, out byte codeVal))
                    {
                        functionCode = codeVal >= 0x80 ? (FunctionCode)(codeVal - 0x80) : (FunctionCode)codeVal;
                    }
                }
            }
            else
            {
                Span<byte> rtuTemp = stackalloc byte[2];
                seq.Slice(0, 2).CopyTo(rtuTemp);
                byte codeVal = rtuTemp[1];
                functionCode = codeVal >= 0x80 ? (FunctionCode)(codeVal - 0x80) : (FunctionCode)codeVal;
            }

            switch (functionCode)
            {
                case FunctionCode.ReadCoilRegister:
                    return new ReadCoilRegisterResponse(context, format);
                case FunctionCode.ReadDiscreteInputRegister:
                    return new ReadDiscreteInputRegisterResponse(context, format);
                case FunctionCode.ReadHoldingRegister:
                    return new ReadHoldingRegisterResponse(context, format);
                case FunctionCode.ReadInputRegister:
                    return new ReadInputRegisterResponse(context, format);
                case FunctionCode.WriteSingleCoilRegister:
                case FunctionCode.WriteSingleHoldingRegister:
                case FunctionCode.WriteMultipleCoilRegisters:
                case FunctionCode.WriteMultipleHoldingRegisters:
                    return new WriteRegisterResponse(context, format);
                default:
                    return new ModbusResponse(context, format);
            }
        }
    }
}
