using STTech.BytesIO.Core;
using System;
using System.Buffers;
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
        /// 从机地址
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
        public ModbusProtocolFormat ProtocolFormat { get; internal set; }

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
        protected ModbusRequest(byte[] data)
        {
            var bytes = data.ToArray();
            ProtocolFormat = bytes.ElementAt(0) == (byte)':' ? ModbusProtocolFormat.ASCII : ModbusProtocolFormat.RTU;

            if (ProtocolFormat == ModbusProtocolFormat.ASCII)
            {
                if (bytes.Length < 11) return; // : + SlaveId(2) + Func(2) + LRC(2) + CRLF(2) = 11
                string hexString = bytes.Skip(1).Take(bytes.Length - 3).EncodeToString(); // 去除 : 和 CRLF
                
                SlaveId = Convert.ToByte(hexString.Substring(0, 2), 16);
                FunctionCode = (FunctionCode)Convert.ToByte(hexString.Substring(2, 2), 16);
                
                int payloadHexLen = hexString.Length - 4 - 2; // 去除 SlaveId(2) 和 Func(2) 和 LRC(2)
                if (payloadHexLen > 0)
                {
                    Payload = hexString.Substring(4, payloadHexLen).HexStringToBytes();
                }
                Checksum = Convert.ToUInt16(hexString.Substring(hexString.Length - 2, 2), 16);
            }
            else if (ProtocolFormat == ModbusProtocolFormat.RTU)
            {
                if (bytes.Length < 4) return;
                SlaveId = bytes.ElementAt(0);
                FunctionCode = (FunctionCode)bytes.ElementAt(1);
                var arr = bytes.Skip(2);
                var payloadLen = arr.Count() - 2;
                Payload = arr.Take(payloadLen).ToArray();
                Checksum = BitConverter.ToUInt16(bytes.Skip(payloadLen).ToArray(), 0);
            }
        }

        /// <summary>
        /// 构造 Modbus 请求对象 (基于解包上下文)
        /// </summary>
        /// <param name="context">解包上下文</param>
        protected ModbusRequest(STTech.BytesIO.Core.Component.UnpackContext context)
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
                FunctionCode = (FunctionCode)Convert.ToByte(hexString.Substring(2, 2), 16);
                
                int payloadHexLen = hexString.Length - 4 - 2; // 去除 SlaveId(2) 和 Func(2) 和 LRC(2)
                if (payloadHexLen > 0)
                {
                    Payload = hexString.Substring(4, payloadHexLen).HexStringToBytes();
                }
                Checksum = Convert.ToByte(hexString.Substring(hexString.Length - 2, 2), 16);
            }
            else if (ProtocolFormat == ModbusProtocolFormat.RTU)
            {
                if (bytes.Length < 4) return;
                SlaveId = bytes[0];
                FunctionCode = (FunctionCode)bytes[1];
                var payloadLen = bytes.Length - 4;
                if (payloadLen > 0)
                {
                    Payload = bytes.Slice(2, payloadLen).ToArray();
                }
                Checksum = (ushort)(bytes[bytes.Length - 2] | (bytes[bytes.Length - 1] << 8));
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
                case ModbusProtocolFormat.RTU:
                    {
                        List<byte> bytes = new List<byte>();
                        bytes.Add(SlaveId);
                        bytes.Add((byte)FunctionCode);
                        SerializePayloadHandle();
                        bytes.AddRange(Payload);
                        return CRC16(bytes.ToArray());
                    }
                case ModbusProtocolFormat.ASCII:
                    {
                        SerializePayloadHandle();
                        var line = $":{SlaveId:00}{(byte)FunctionCode:X2}{Payload?.ToHexString() ?? ""}";
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
            string hex = "";
            int strLen = message.Length;
            for (int i = 1; i < strLen - 1; i = i + 2)
            {
                string temp = message.Substring(i, 2);
                sum = sum + Convert.ToInt32(temp, 16);
            }
            if (sum >= 256)
                sum = sum % 256;
            hex = Convert.ToInt32(~sum + 1).ToString("X");
            if (hex.Length > 2)
                hex = hex.Substring(hex.Length - 2, 2);
            return hex;
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
            if (value == null || !value.Any())
                throw new ArgumentException("");

            //运算
            ushort crc = crcInit;
            for (int i = 0; i < value.Length; i++)
            {
                crc = (ushort)(crc ^ (value[i]));
                for (int j = 0; j < 8; j++)
                {
                    crc = (crc & 1) != 0 ? (ushort)((crc >> 1) ^ poly) : (ushort)(crc >> 1);
                }
            }
            byte hi = (byte)((crc & 0xFF00) >> 8);
            byte lo = (byte)(crc & 0x00FF);
            List<byte> buffer = new List<byte>();
            buffer.AddRange(value);
            buffer.Add(lo);
            buffer.Add(hi);
            return buffer.ToArray();
        }
    }

}
