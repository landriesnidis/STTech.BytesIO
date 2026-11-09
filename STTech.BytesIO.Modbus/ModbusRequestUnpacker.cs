using STTech.BytesIO.Core;
using STTech.BytesIO.Core.Component;
using System;
using System.Buffers;
using System.Buffers.Binary;

namespace STTech.BytesIO.Modbus
{
    /// <summary>
    /// Modbus 请求报文解包器 (用于服务端接收客户端请求)
    /// </summary>
    public class ModbusRequestUnpacker : Unpacker<ModbusRequest>
    {
        internal ModbusProtocolFormat Format { get; set; }

        /// <summary>
        /// 用于判断本机 SlaveId 的回调。若返回 false，则封装为透明转发请求。
        /// </summary>
        public Func<byte, bool> IsLocalSlaveId { get; set; }

        /// <summary>
        /// 构造 Modbus 请求解包器
        /// </summary>
        /// <param name="client">底层的通信客户端</param>
        /// <param name="format">协议格式</param>
        public ModbusRequestUnpacker(BytesClient client, ModbusProtocolFormat format) : base(client)
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

            ushort length = BinaryPrimitives.ReadUInt16BigEndian(head.Slice(4, 2));
            if (length < 2 || length > 260)
            {
                return -1;
            }

            int totalLen = 6 + length;
            if (buffer.Length < totalLen)
            {
                return 0; // 等待后续数据
            }

            return totalLen;
        }

        private static int CalculateAsciiPacketLengthHandler(ref ReadOnlySequence<byte> buffer)
        {
            if (buffer.Length < 17)
            {
                return 0;
            }

            Span<byte> head = stackalloc byte[17];
            buffer.Slice(0, 17).CopyTo(head);

            if (head[0] != (byte)':')
            {
                return -1;
            }

            string codeStr = System.Text.Encoding.ASCII.GetString(head.Slice(3, 2).ToArray());
            if (!byte.TryParse(codeStr, System.Globalization.NumberStyles.HexNumber, null, out byte codeVal))
            {
                return -1;
            }

            var code = (FunctionCode)codeVal;
            int packetLen;

            switch (code)
            {
                case FunctionCode.ReadCoilRegister:
                case FunctionCode.ReadDiscreteInputRegister:
                case FunctionCode.ReadHoldingRegister:
                case FunctionCode.ReadInputRegister:
                case FunctionCode.WriteSingleCoilRegister:
                case FunctionCode.WriteSingleHoldingRegister:
                    packetLen = 17; // : + SlaveId(2) + Func(2) + Addr(4) + Val/Qty(4) + LRC(2) + CRLF(2) = 17
                    break;

                case FunctionCode.WriteMultipleCoilRegisters:
                case FunctionCode.WriteMultipleHoldingRegisters:
                    string byteCountStr = System.Text.Encoding.ASCII.GetString(head.Slice(13, 2).ToArray());
                    if (!byte.TryParse(byteCountStr, System.Globalization.NumberStyles.HexNumber, null, out byte byteCount) || byteCount == 0 || byteCount > 246)
                    {
                        return -1;
                    }
                    packetLen = 15 + (byteCount * 2) + 2 + 2; // fixed(15) + Data(N*2) + LRC(2) + CRLF(2)
                    break;

                default:
                    // 未知功能码，滑动丢弃 1 字节自愈，拒绝抛异常死锁
                    return -1;
            }

            if (buffer.Length < packetLen)
            {
                return 0;
            }

            // LRC 校验
            Span<byte> frame = packetLen <= 256 ? stackalloc byte[packetLen] : new byte[packetLen];
            buffer.Slice(0, packetLen).CopyTo(frame);

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
                return -1;
            }

            return packetLen;
        }

        private static int CalculateRtuPacketLengthHandler(ref ReadOnlySequence<byte> buffer)
        {
            if (buffer.Length < 8)
            {
                return 0;
            }

            Span<byte> head = stackalloc byte[8];
            buffer.Slice(0, 8).CopyTo(head);

            var code = (FunctionCode)head[1];
            int packetLen;

            switch (code)
            {
                case FunctionCode.ReadCoilRegister:
                case FunctionCode.ReadDiscreteInputRegister:
                case FunctionCode.ReadHoldingRegister:
                case FunctionCode.ReadInputRegister:
                case FunctionCode.WriteSingleCoilRegister:
                case FunctionCode.WriteSingleHoldingRegister:
                    packetLen = 8;
                    break;

                case FunctionCode.WriteMultipleCoilRegisters:
                case FunctionCode.WriteMultipleHoldingRegisters:
                    int byteCount = head[6];
                    if (byteCount == 0 || byteCount > 246)
                    {
                        return -1; // 非法数据长度 (Modbus 规范上限 246 字节)，滑动丢弃 1 字节自愈
                    }
                    packetLen = 7 + byteCount + 2; // SlaveId(1) + Func(1) + Addr(2) + Quantity(2) + ByteCount(1) + Data(N) + CRC(2)
                    break;

                default:
                    // 未知功能码，滑动丢弃 1 字节自愈，绝不抛出异常死锁
                    return -1;
            }

            if (buffer.Length < packetLen)
            {
                return 0;
            }

            // 严格执行 CRC16 校验 (零堆分配)
            Span<byte> fullFrame = packetLen <= 256 ? stackalloc byte[packetLen] : new byte[packetLen];
            buffer.Slice(0, packetLen).CopyTo(fullFrame);
            if (!ModbusCrc16.Check(fullFrame))
            {
                return -1;
            }

            return packetLen;
        }

        /// <inheritdoc/>
        protected override ModbusRequest ResponseSerializeHandler(UnpackContext context)
        {
            var seq = context.Data;
            if (seq.Length < 2) return null;

            FunctionCode functionCode;
            byte slaveId;
            var format = Format;

            if (format == ModbusProtocolFormat.MBAP)
            {
                if (seq.Length < 8) return null;
                Span<byte> mbapHead = stackalloc byte[8];
                seq.Slice(0, 8).CopyTo(mbapHead);
                slaveId = mbapHead[6];
                functionCode = (FunctionCode)mbapHead[7];
            }
            else if (format == ModbusProtocolFormat.ASCII)
            {
                if (seq.Length < 5) return null;
                Span<byte> asciiTemp = stackalloc byte[5];
                seq.Slice(0, 5).CopyTo(asciiTemp);
                var codeStr = System.Text.Encoding.ASCII.GetString(asciiTemp.Slice(3, 2).ToArray());
                functionCode = (FunctionCode)Convert.ToByte(codeStr, 16);
                
                var slaveIdStr = System.Text.Encoding.ASCII.GetString(asciiTemp.Slice(1, 2).ToArray());
                slaveId = Convert.ToByte(slaveIdStr, 16);
            }
            else
            {
                Span<byte> rtuHead = stackalloc byte[2];
                seq.Slice(0, 2).CopyTo(rtuHead);
                slaveId = rtuHead[0];
                functionCode = (FunctionCode)rtuHead[1];
            }

            // 透明转发判定：非本机报文直接拦截，抛出 ModbusForwardRequest
            if (IsLocalSlaveId != null && !IsLocalSlaveId(slaveId))
            {
                return new STTech.BytesIO.Modbus.Entity.ModbusForwardRequest(context, format);
            }

            switch (functionCode)
            {
                case FunctionCode.ReadCoilRegister:
                case FunctionCode.ReadDiscreteInputRegister:
                case FunctionCode.ReadHoldingRegister:
                case FunctionCode.ReadInputRegister:
                    return new ReadRegisterRequest(context, format);
                case FunctionCode.WriteSingleCoilRegister:
                    return new WriteSingleCoilRegisterRequest(context, format);
                case FunctionCode.WriteSingleHoldingRegister:
                    return new WriteSingleHoldingRegisterRequest(context, format);
                case FunctionCode.WriteMultipleCoilRegisters:
                    return new WriteMultipleCoilRegistersRequest(context, format);
                case FunctionCode.WriteMultipleHoldingRegisters:
                    return new WriteMultipleHoldingRegistersRequest(context, format);
                default:
                    return null;
            }
        }
    }
}
