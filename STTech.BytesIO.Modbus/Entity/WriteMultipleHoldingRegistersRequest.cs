using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace STTech.BytesIO.Modbus
{
    /// <summary>
    /// 写多个保持寄存器请求
    /// </summary>
    public class WriteMultipleHoldingRegistersRequest : ModbusRequest
    {
        /// <summary>
        /// 写入地址
        /// </summary>
        [Description("写入地址")]
        public ushort WriteAddress { get; set; }

        /// <summary>
        /// 写入数据
        /// </summary>
        [Browsable(false)]
        [Description("写入数据")]
        public byte[] Data { get; set; } = new byte[0];

        /// <summary>
        /// 获取由转数据转换的ushort数组
        /// </summary>
        /// <returns></returns>
        public ushort[] GetUInt16Array()
        {
            return Data.Slice(2).Select(ba => BitConverter.ToUInt16(Enumerable.Reverse(ba).ToArray(), 0)).ToArray();
        }

        /// <summary>
        /// 构造写多个保持寄存器请求
        /// </summary>
        public WriteMultipleHoldingRegistersRequest() : base(FunctionCode.WriteMultipleHoldingRegisters) { }

        /// <summary>
        /// 构造写多个保持寄存器请求 (基于字节数组)
        /// </summary>
        /// <param name="data">原始报文数据</param>
        public WriteMultipleHoldingRegistersRequest(byte[] data) : base(data)
        {
            DeserializePayloadHandle();
        }

        /// <summary>
        /// 构造写多个保持寄存器请求 (基于解包上下文)
        /// </summary>
        /// <param name="context">解包上下文</param>
        public WriteMultipleHoldingRegistersRequest(STTech.BytesIO.Core.Component.UnpackContext context) : base(context)
        {
            DeserializePayloadHandle();
        }

        /// <inheritdoc/>
        protected internal override void SerializePayloadHandle()
        {
            List<byte> bytes = new List<byte>();
            bytes.AddRange(Enumerable.Reverse(BitConverter.GetBytes(WriteAddress)));
            bytes.AddRange(Enumerable.Reverse(BitConverter.GetBytes((ushort)(Data.Length / 2))));     // 寄存器个数
            bytes.Add((byte)Data.Length);                                                   // 字节数  （寄存器个数*2）
            bytes.AddRange(Data);
            Payload = bytes.ToArray();
        }

        /// <inheritdoc/>
        protected internal override void DeserializePayloadHandle()
        {
            if (Payload == null || Payload.Length < 5) return;
            WriteAddress = BitConverter.ToUInt16(Payload.Take(2).Reverse().ToArray(), 0);
            var byteCount = Payload[4];
            if (Payload.Length >= 5 + byteCount)
            {
                Data = Payload.Skip(5).Take(byteCount).ToArray();
            }
        }
    }
}
