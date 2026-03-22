using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace STTech.BytesIO.Modbus
{
    /// <summary>
    /// 写入寄存器响应
    /// </summary>
    public class WriteRegisterResponse : ModbusResponse
    {
        /// <summary>
        /// 写入地址
        /// </summary>
        public ushort WriteAddress { get; protected set; }

        /// <summary>
        /// 写入的数据
        /// </summary>
        public byte[] Values { get; protected set; }

        public WriteRegisterResponse() { }

        /// <summary>
        /// 构造写入寄存器响应
        /// </summary>
        /// <param name="slaveId">从站地址</param>
        /// <param name="functionCode">功能码</param>
        /// <param name="format">协议格式</param>
        /// <param name="writeAddress">写入地址</param>
        /// <param name="values">写入的数据</param>
        public WriteRegisterResponse(ushort slaveId, FunctionCode functionCode, ModbusProtocolFormat format, ushort writeAddress, byte[] values) 
            : base(slaveId, functionCode, format)
        {
            WriteAddress = writeAddress;
            Values = values;
        }

        protected internal override void SerializePayloadHandle()
        {
            List<byte> bytes = new List<byte>();
            bytes.AddRange(Enumerable.Reverse(BitConverter.GetBytes(WriteAddress)));
            if (Values != null)
            {
                bytes.AddRange(Values);
            }
            Payload = bytes.ToArray();
        }

        /// <summary>
        /// 构造写入寄存器响应 (基于字节数组)
        /// </summary>
        /// <param name="bytes">原始报文数据</param>
        public WriteRegisterResponse(byte[] bytes) : base(bytes)
        {
            if (IsSuccess && Payload != null && Payload.Length >= 2)
            {
                WriteAddress = BitConverter.ToUInt16(new byte[] { Payload[1], Payload[0] }, 0);
                Values = Payload.Skip(2).ToArray();
            }
        }

        /// <summary>
        /// 构造写入寄存器响应 (基于解包上下文)
        /// </summary>
        /// <param name="context">解包上下文</param>
        public WriteRegisterResponse(STTech.BytesIO.Core.Component.UnpackContext context) : base(context)
        {
            if (IsSuccess && Payload != null && Payload.Length >= 2)
            {
                WriteAddress = BitConverter.ToUInt16(new byte[] { Payload[1], Payload[0] }, 0);
                Values = Payload.Skip(2).ToArray();
            }
        }

        /// <summary>
        /// 获取由转数据转换的ushort值
        /// </summary>
        /// <returns></returns>
        public ushort GetUInt16()
        {
            return BitConverter.ToUInt16([Values[1], Values[0]], 0);
        }
    }
}
