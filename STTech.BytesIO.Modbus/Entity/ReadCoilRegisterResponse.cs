using System;
using System.Collections.Generic;
using System.Linq;

namespace STTech.BytesIO.Modbus
{
    /// <summary>
    /// 读线圈寄存器响应
    /// </summary>
    public class ReadCoilRegisterResponse : ModbusResponse
    {
        /// <summary>
        /// 数据字节长度
        /// </summary>
        public byte Length { get; protected set; }

        /// <summary>
        /// 寄存器状态值列表
        /// </summary>
        public bool[] Values { get; protected set; }

        /// <summary>
        /// 构造读线圈寄存器响应
        /// </summary>
        public ReadCoilRegisterResponse() { }

        /// <summary>
        /// 构造读线圈寄存器响应
        /// </summary>
        /// <param name="slaveId">从站地址</param>
        /// <param name="format">协议格式</param>
        /// <param name="values">线圈状态值</param>
        public ReadCoilRegisterResponse(ushort slaveId, ModbusProtocolFormat format, bool[] values) 
            : base(slaveId, FunctionCode.ReadCoilRegister, format)
        {
            Values = values;
            Length = (byte)Math.Ceiling((values?.Length ?? 0) / 8.0);
        }

        /// <inheritdoc/>
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
        /// 构造读线圈寄存器响应 (基于字节数组)
        /// </summary>
        /// <param name="bytes">原始报文数据</param>
        public ReadCoilRegisterResponse(byte[] bytes) : base(bytes)
        {
            if (IsSuccess && Payload != null && Payload.Length > 0)
            {
                Length = Payload.ElementAt(0);
                Values = ParseBits(Payload, Length);
            }
        }

        /// <summary>
        /// 构造读线圈寄存器响应 (基于解包上下文)
        /// </summary>
        /// <param name="context">解包上下文</param>
        public ReadCoilRegisterResponse(STTech.BytesIO.Core.Component.UnpackContext context) : base(context)
        {
            if (IsSuccess && Payload != null && Payload.Length > 0)
            {
                Length = Payload.ElementAt(0);
                Values = ParseBits(Payload, Length);
            }
        }

        private static bool[] ParseBits(byte[] payload, int byteCount)
        {
            if (payload == null || payload.Length < 1 + byteCount) return Array.Empty<bool>();
            var values = new bool[byteCount * 8];
            for (int i = 0; i < byteCount; i++)
            {
                byte b = payload[1 + i];
                int offset = i * 8;
                for (int j = 0; j < 8; j++)
                {
                    values[offset + j] = (b & (1 << j)) != 0;
                }
            }
            return values;
        }
    }

}
