using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace STTech.BytesIO.Modbus
{
    /// <summary>
    /// 读离散输入寄存器响应
    /// </summary>
    public class ReadDiscreteInputRegisterResponse : ModbusResponse
    {
        /// <summary>
        /// 数据字节长度
        /// </summary>
        public byte Length { get; protected set; }

        /// <summary>
        /// 寄存器状态值列表
        /// </summary>
        public bool[] Values { get; protected set; }

        public ReadDiscreteInputRegisterResponse() { }

        /// <summary>
        /// 构造读离散输入寄存器响应
        /// </summary>
        /// <param name="slaveId">从站地址</param>
        /// <param name="format">协议格式</param>
        /// <param name="values">离散输入状态值</param>
        public ReadDiscreteInputRegisterResponse(ushort slaveId, ModbusProtocolFormat format, bool[] values) 
            : base(slaveId, FunctionCode.ReadDiscreteInputRegister, format)
        {
            Values = values;
            Length = (byte)Math.Ceiling((values?.Length ?? 0) / 8.0);
        }

        protected internal override void SerializePayloadHandle()
        {
            List<byte> bytes = new List<byte>();
            bytes.Add(Length);
            
            if (Values != null && Values.Length > 0)
            {
                var bits = new List<byte>();
                for (int i = 0; i < Values.Length; i += 8)
                {
                    byte b = 0;
                    for (int j = 0; j < 8 && i + j < Values.Length; j++)
                    {
                        if (Values[i + j])
                        {
                            b |= (byte)(1 << j);
                        }
                    }
                    bits.Add(b);
                }
                bytes.AddRange(bits);
            }
            Payload = bytes.ToArray();
        }
        /// <summary>
        /// 构造读离散输入寄存器响应 (基于字节数组)
        /// </summary>
        /// <param name="bytes">原始报文数据</param>
        public ReadDiscreteInputRegisterResponse(byte[] bytes) : base(bytes)
        {
            if (IsSuccess && Payload != null && Payload.Length > 0)
            {
                Length = Payload.ElementAt(0);
                Values = Payload.Skip(1).Select(x => Convert.ToString(x, 2).PadLeft(8, '0').Reverse()).SelectMany(x => x).Select(x => x != '0').ToArray();
            }
        }

        public ReadDiscreteInputRegisterResponse(STTech.BytesIO.Core.Component.UnpackContext context) : base(context)
        {
            if (IsSuccess && Payload != null && Payload.Length > 0)
            {
                Length = Payload.ElementAt(0);
                Values = Payload.Skip(1).Select(x => Convert.ToString(x, 2).PadLeft(8, '0').Reverse()).SelectMany(x => x).Select(x => x != '0').ToArray();
            }
        }
    }
}
