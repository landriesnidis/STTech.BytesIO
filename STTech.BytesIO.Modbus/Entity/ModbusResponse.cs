using STTech.BytesIO.Core;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;

namespace STTech.BytesIO.Modbus
{
    public class ModbusResponse : Response
    {
        private byte[] payload;

        /// <summary>
        /// 从机地址
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
        /// <param name="slaveId">从站地址</param>
        /// <param name="functionCode">功能码</param>
        /// <param name="format">协议格式</param>
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
        /// <param name="slaveId">从站地址</param>
        /// <param name="functionCode">功能码</param>
        /// <param name="format">协议格式</param>
        /// <param name="errorCode">错误码</param>
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
        /// <param name="data">原始报文数据</param>
        public ModbusResponse(byte[] data) : base(data) // 这里的Bytes只当做原始数据保存
        {
            var bytes = data.ToArray();

            ProtocolFormat = bytes.ElementAt(0) == (byte)':' ? ModbusProtocolFormat.ASCII : ModbusProtocolFormat.RTU;

            if (ProtocolFormat == ModbusProtocolFormat.ASCII)
            {
                if (bytes.Length < 11) return;
                string hexString = bytes.Skip(1).Take(bytes.Length - 3).EncodeToString(); // 去除 : 和 CRLF

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
                    int payloadHexLen = hexString.Length - 4 - 2; // 去除 SlaveId(2) 和 Func(2) 和 LRC(2)
                    if (payloadHexLen > 0)
                    {
                        payload = hexString.Substring(4, payloadHexLen).HexStringToBytes();
                    }
                }
                Checksum = Convert.ToByte(hexString.Substring(hexString.Length - 2, 2), 16);
            }
            else if (ProtocolFormat == ModbusProtocolFormat.RTU)
            {
                SlaveId = bytes.ElementAt(0);

                if (bytes.Length == 5 && bytes.ElementAt(1) >= 0x80)
                {
                    IsSuccess = false;
                    FunctionCode = (FunctionCode)(bytes.ElementAt(1) - 0x80);
                    ErrorCode = (ModbusErrorCode)bytes.ElementAt(2);
                    Checksum = BitConverter.ToUInt16(bytes, 3);
                    return;
                }

                FunctionCode = (FunctionCode)bytes.ElementAt(1);
                var arr = bytes.Skip(2);
                var payloadLen = arr.Count() - 2;
                payload = arr.Take(payloadLen).ToArray();
                Checksum = BitConverter.ToUInt16(bytes.Skip(payloadLen).ToArray(), 0);
            }
        }

        /// <summary>
        /// 构造 Modbus 响应对象 (基于解包上下文)
        /// </summary>
        /// <param name="context">解包上下文</param>
        public ModbusResponse(STTech.BytesIO.Core.Component.UnpackContext context) : base(context)
        {
            UnpackContext = context;
            int len = (int)context.Data.Length;
            Span<byte> bytes = stackalloc byte[len];
            context.Data.CopyTo(bytes);

            ProtocolFormat = bytes[0] == (byte)':' ? ModbusProtocolFormat.ASCII : ModbusProtocolFormat.RTU;

            if (ProtocolFormat == ModbusProtocolFormat.ASCII)
            {
                if (bytes.Length < 11) return;
                string hexString = bytes.Slice(1, bytes.Length - 3).ToArray().EncodeToString(); // 去除 : 和 CRLF

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
                    int payloadHexLen = hexString.Length - 4 - 2; // 去除 SlaveId(2) 和 Func(2) 和 LRC(2)
                    if (payloadHexLen > 0)
                    {
                        payload = hexString.Substring(4, payloadHexLen).HexStringToBytes();
                    }
                }
                Checksum = Convert.ToByte(hexString.Substring(hexString.Length - 2, 2), 16);
            }
            else if (ProtocolFormat == ModbusProtocolFormat.RTU)
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
                var payloadLen = bytes.Length - 4;
                if (payloadLen > 0)
                {
                    payload = bytes.Slice(2, payloadLen).ToArray();
                }
                Checksum = (ushort)(bytes[bytes.Length - 2] | (bytes[bytes.Length - 1] << 8));
            }
        }

        protected internal virtual void SerializePayloadHandle()
        {
        }

        public byte[] GetBytes()
        {
            switch (ProtocolFormat)
            {
                case ModbusProtocolFormat.RTU:
                    {
                        List<byte> bytes = new List<byte>();
                        bytes.Add((byte)SlaveId);

                        if (!IsSuccess)
                        {
                            bytes.Add((byte)((byte)FunctionCode | 0x80));
                            bytes.Add((byte)ErrorCode);
                        }
                        else
                        {
                            bytes.Add((byte)FunctionCode);
                            SerializePayloadHandle();
                            if (payload != null)
                            {
                                bytes.AddRange(payload);
                            }
                        }

                        return ModbusRequest.CRC16(bytes.ToArray());
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
