using STTech.BytesIO.Core;
using STTech.BytesIO.Core.Component;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;

namespace STTech.BytesIO.Modbus
{
    /// <summary>
    /// Modbus请求报文解包器 (用于服务端)
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
            if (Format == ModbusProtocolFormat.RTU)
            {
                return CalculateRtuPacketLengthHandler(ref buffer);
            }
            else
            {
                return CalculateAsciiPacketLengthHandler(ref buffer);
            }
        }

        private static int CalculateAsciiPacketLengthHandler(ref ReadOnlySequence<byte> buffer)
        {
            if (buffer.Length < 17)
            {
                return 0;
            }

            Span<byte> head = stackalloc byte[17];
            buffer.Slice(0, 17).CopyTo(head);

            string codeStr = System.Text.Encoding.ASCII.GetString(head.Slice(3, 2).ToArray());
            var code = (FunctionCode)ushort.Parse(codeStr);

            switch (code)
            {
                case FunctionCode.ReadCoilRegister:
                case FunctionCode.ReadDiscreteInputRegister:
                case FunctionCode.ReadHoldingRegister:
                case FunctionCode.ReadInputRegister:
                case FunctionCode.WriteSingleCoilRegister:
                case FunctionCode.WriteSingleHoldingRegister:
                    return 17;

                case FunctionCode.WriteMultipleCoilRegisters:
                case FunctionCode.WriteMultipleHoldingRegisters:
                    if (buffer.Length < 17) return 0;
                    Span<byte> head17 = stackalloc byte[17];
                    buffer.Slice(0, 17).CopyTo(head17);
                    string byteCountStr = System.Text.Encoding.ASCII.GetString(head17.Slice(13, 2).ToArray());
                    var byteCount = ushort.Parse(byteCountStr);
                    return 15 + (byteCount * 2) + 2 + 2; // fixed(15) + Data(N*2) + LRC(2) + CRLF(2)

                default:
                    throw new Exception($"解包失败(未知功能码)：{buffer.ToArray().ToHexString()}");
            }
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

            switch (code)
            {
                case FunctionCode.ReadCoilRegister:
                case FunctionCode.ReadDiscreteInputRegister:
                case FunctionCode.ReadHoldingRegister:
                case FunctionCode.ReadInputRegister:
                case FunctionCode.WriteSingleCoilRegister:
                case FunctionCode.WriteSingleHoldingRegister:
                    return 8;

                case FunctionCode.WriteMultipleCoilRegisters:
                case FunctionCode.WriteMultipleHoldingRegisters:
                    var byteCount = head[6];
                    return 7 + byteCount + 2; // SlaveId(1) + Func(1) + Addr(2) + Quantity(2) + ByteCount(1) + Data(N) + CRC(2)

                default:
                    throw new Exception($"解包失败(未知功能码)：{buffer.ToArray().ToHexString()}");
            }
        }

        /// <inheritdoc/>
        protected override ModbusRequest ResponseSerializeHandler(UnpackContext context)
        {
            var seq = context.Data;
            if (seq.Length < 2) return null;

            byte firstByte;
            byte secondByte;
            if (seq.IsSingleSegment)
            {
                var span = seq.First.Span;
                firstByte = span[0];
                secondByte = span[1];
            }
            else
            {
                Span<byte> temp = stackalloc byte[2];
                seq.Slice(0, 2).CopyTo(temp);
                firstByte = temp[0];
                secondByte = temp[1];
            }

            FunctionCode functionCode;
            var format = firstByte == (byte)':' ? ModbusProtocolFormat.ASCII : ModbusProtocolFormat.RTU;

            byte slaveId;

            if (format == ModbusProtocolFormat.ASCII)
            {
                if (seq.Length < 5) return null;
                Span<byte> asciiTemp = stackalloc byte[5];
                seq.Slice(0, 5).CopyTo(asciiTemp);
                var codeStr = System.Text.Encoding.ASCII.GetString(asciiTemp.Slice(3, 2).ToArray());
                functionCode = (FunctionCode)ushort.Parse(codeStr);
                
                var slaveIdStr = System.Text.Encoding.ASCII.GetString(asciiTemp.Slice(1, 2).ToArray());
                slaveId = byte.Parse(slaveIdStr);
            }
            else
            {
                slaveId = firstByte;
                functionCode = (FunctionCode)secondByte;
            }

            // 透明转发判定：非本机报文直接拦截，抛出 ModbusForwardRequest
            if (IsLocalSlaveId != null && !IsLocalSlaveId(slaveId))
            {
                return new STTech.BytesIO.Modbus.Entity.ModbusForwardRequest(context);
            }

            switch (functionCode)
            {
                case FunctionCode.ReadCoilRegister:
                case FunctionCode.ReadDiscreteInputRegister:
                case FunctionCode.ReadHoldingRegister:
                case FunctionCode.ReadInputRegister:
                    return new ReadRegisterRequest(context);
                case FunctionCode.WriteSingleCoilRegister:
                    return new WriteSingleCoilRegisterRequest(context);
                case FunctionCode.WriteSingleHoldingRegister:
                    return new WriteSingleHoldingRegisterRequest(context);
                case FunctionCode.WriteMultipleCoilRegisters:
                    return new WriteMultipleCoilRegistersRequest(context);
                case FunctionCode.WriteMultipleHoldingRegisters:
                    return new WriteMultipleHoldingRegistersRequest(context);
                default:
                    return null;
            }
        }
    }
}
