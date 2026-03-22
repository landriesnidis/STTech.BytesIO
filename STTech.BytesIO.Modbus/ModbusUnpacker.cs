
using STTech.BytesIO.Core;
using STTech.BytesIO.Core.Component;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;

namespace STTech.BytesIO.Modbus
{
    /// <summary>
    /// Modbus协议解包器
    /// </summary>
    public class ModbusUnpacker : Unpacker<ModbusResponse>
    {
        private const int checkSumLen = 2;
        private const int rtuSlaveIdLen = 1;
        private const int rtuFunctionCodeLen = 1;
        private const int rtuFixedHead = rtuSlaveIdLen + rtuFunctionCodeLen;
        private const int asciiStartCharLen = 1;
        private const int asciiSlaveIdLen = 2;
        private const int asciiFunctionCodeLen = 2;
        private const int asciiFixedHead = asciiStartCharLen + asciiSlaveIdLen + asciiFunctionCodeLen;

        /// <summary>
        /// Modbus通信格式
        /// </summary>
        internal ModbusProtocolFormat Format { get;  set; }    // 如果Unpacker能支持清空缓存的话就好了，这里应该需要清空一下缓存的 

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
            if (buffer.Length < 11)
            {
                return 0;
            }

            Span<byte> head = stackalloc byte[11];
            buffer.Slice(0, 11).CopyTo(head);

            // Fetch function code (2 chars at index 3, 4)
            string codeStr = System.Text.Encoding.ASCII.GetString(head.Slice(3, 2).ToArray());
            var code = (FunctionCode)Convert.ToByte(codeStr, 16);

            if ((byte)code > 0x80)
            {
                return 11; // : + SlaveId(2) + ErrorCode(2) + LRC(2) + CRLF(2) = 11
            }

            switch (code)
            {
                case FunctionCode.ReadCoilRegister:
                case FunctionCode.ReadDiscreteInputRegister:
                case FunctionCode.ReadHoldingRegister:
                case FunctionCode.ReadInputRegister:
                    // : + SlaveId(2) + Func(2) + ByteCount(2) + Data(N*2) + LRC(2) + CRLF(2)
                    string byteCountStr = System.Text.Encoding.ASCII.GetString(head.Slice(5, 2).ToArray());
                    var byteCount = Convert.ToByte(byteCountStr, 16);
                    return 11 + (byteCount * 2);

                case FunctionCode.WriteSingleCoilRegister:
                case FunctionCode.WriteSingleHoldingRegister:
                case FunctionCode.WriteMultipleCoilRegisters:
                case FunctionCode.WriteMultipleHoldingRegisters:
                    return 17; // : + SlaveId(2) + Func(2) + Addr(4) + Value(4) + LRC(2) + CRLF(2) = 17

                default:
                    throw new Exception($"解包失败(未知功能码)：{buffer.ToArray().ToHexString()}");
            }
        }

        private static int CalculateRtuPacketLengthHandler(ref ReadOnlySequence<byte> buffer)
        {
            if (buffer.Length < 5)
            {
                return 0;
            }

            Span<byte> head = stackalloc byte[5];
            buffer.Slice(0, 5).CopyTo(head);

            var code = (FunctionCode)head[1];

            if ((byte)code > 0x80)
            {
                return 5;
            }

            switch (code)
            {
                case FunctionCode.ReadCoilRegister:
                case FunctionCode.ReadDiscreteInputRegister:
                case FunctionCode.ReadHoldingRegister:
                case FunctionCode.ReadInputRegister:
                    return rtuFixedHead + 1 + head[rtuFixedHead] + checkSumLen;

                case FunctionCode.WriteSingleCoilRegister:
                case FunctionCode.WriteSingleHoldingRegister:
                case FunctionCode.WriteMultipleCoilRegisters:
                case FunctionCode.WriteMultipleHoldingRegisters:
                    return rtuFixedHead + 4 + checkSumLen;

                default:
                    throw new Exception($"解包失败：{buffer.ToArray().ToHexString()}");
            }
        }

        /// <inheritdoc/>
        protected override ModbusResponse ResponseSerializeHandler(UnpackContext context)
        {
            var seq = context.Data;
            if (seq.Length < 2) return new ModbusResponse(context);

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

            if (format == ModbusProtocolFormat.ASCII)
            {
                if (seq.Length < 5) return new ModbusResponse(context);
                Span<byte> asciiTemp = stackalloc byte[5];
                seq.Slice(0, 5).CopyTo(asciiTemp);
                // ASCII decode: characters 3 and 4 
                var codeStr = System.Text.Encoding.ASCII.GetString(asciiTemp.Slice(3, 2).ToArray());
                var codeVal = ushort.Parse(codeStr);
                functionCode = codeVal >= 80 ? (FunctionCode)(codeVal - 0x80) : (FunctionCode)codeVal;
            }
            else
            {
                var codeVal = secondByte;
                functionCode = codeVal >= 0x80 ? (FunctionCode)(codeVal - 0x80) : (FunctionCode)codeVal;
            }

            switch (functionCode)
            {
                case FunctionCode.ReadCoilRegister:
                    return new ReadCoilRegisterResponse(context);
                case FunctionCode.ReadDiscreteInputRegister:
                    return new ReadDiscreteInputRegisterResponse(context);
                case FunctionCode.ReadHoldingRegister:
                    return new ReadHoldingRegisterResponse(context);
                case FunctionCode.ReadInputRegister:
                    return new ReadInputRegisterResponse(context);
                case FunctionCode.WriteSingleCoilRegister:
                case FunctionCode.WriteSingleHoldingRegister:
                case FunctionCode.WriteMultipleCoilRegisters:
                case FunctionCode.WriteMultipleHoldingRegisters:
                    return new WriteRegisterResponse(context);
                default:
                    return new ModbusResponse(context);
            }
        }
    }
}
