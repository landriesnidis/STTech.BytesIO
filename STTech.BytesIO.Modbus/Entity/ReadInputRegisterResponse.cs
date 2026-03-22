using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace STTech.BytesIO.Modbus
{
    /// <summary>
    /// 读输入寄存器响应
    /// </summary>
    public class ReadInputRegisterResponse : ModbusResponse
    {
        /// <summary>
        /// 数据字节长度
        /// </summary>
        public byte Length { get; protected set; }

        /// <summary>
        /// 寄存器数据
        /// </summary>
        public byte[] Values { get; protected set; }

        public ReadInputRegisterResponse() { }

        /// <summary>
        /// 构造读输入寄存器响应
        /// </summary>
        /// <param name="slaveId">从站地址</param>
        /// <param name="format">协议格式</param>
        /// <param name="values">寄存器数据</param>
        public ReadInputRegisterResponse(ushort slaveId, ModbusProtocolFormat format, byte[] values)
            : base(slaveId, FunctionCode.ReadInputRegister, format)
        {
            Values = values;
            Length = (byte)(values?.Length ?? 0);
        }

        protected internal override void SerializePayloadHandle()
        {
            List<byte> bytes = new List<byte>();
            bytes.Add(Length);
            if (Values != null)
            {
                bytes.AddRange(Values);
            }
            Payload = bytes.ToArray();
        }

        /// <summary>
        /// 获取由原始数据转换的 ushort 数组
        /// </summary>
        /// <returns>ushort 数组</returns>
        public ushort[] GetUInt16Array()
        {
            return Values.Slice(2).Select(ba => BitConverter.ToUInt16(Enumerable.Reverse(ba).ToArray(), 0)).ToArray();
        }

        /// <summary>
        /// 构造读输入寄存器响应 (基于字节数组)
        /// </summary>
        /// <param name="bytes">原始报文数据</param>
        public ReadInputRegisterResponse(byte[] bytes) : base(bytes)
        {
            if (IsSuccess && Payload != null && Payload.Length > 0) 
            {
                Length = Payload.ElementAt(0);
                Values = Payload.Skip(1).ToArray();
            }
           
        }

        public ReadInputRegisterResponse(STTech.BytesIO.Core.Component.UnpackContext context) : base(context)
        {
            if (IsSuccess && Payload != null && Payload.Length > 0) 
            {
                Length = Payload.ElementAt(0);
                Values = Payload.Skip(1).ToArray();
            }
        }
    }
}
