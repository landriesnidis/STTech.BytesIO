using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace STTech.BytesIO.Modbus
{
    /// <summary>
    /// 写单个线圈寄存器请求
    /// </summary>
    public class WriteSingleCoilRegisterRequest : ModbusRequest
    {
        /// <summary>
        /// 写入地址
        /// </summary>
        [Description("写入地址")]
        public ushort WriteAddress { get; set; }

        /// <summary>
        /// 写入数据 (true 为通, false 为断)
        /// </summary>
        [Description("写入数据")]
        public bool Data { get; set; }

        /// <summary>
        /// 构造写单个线圈寄存器请求
        /// </summary>
        public WriteSingleCoilRegisterRequest() : base(FunctionCode.WriteSingleCoilRegister) { }

        /// <summary>
        /// 构造写单个线圈寄存器请求 (基于字节数组)
        /// </summary>
        /// <param name="data">原始报文数据</param>
        public WriteSingleCoilRegisterRequest(byte[] data) : base(data)
        {
            DeserializePayloadHandle();
        }

        /// <summary>
        /// 构造写单个线圈寄存器请求 (基于解包上下文)
        /// </summary>
        /// <param name="context">解包上下文</param>
        public WriteSingleCoilRegisterRequest(STTech.BytesIO.Core.Component.UnpackContext context) : base(context)
        {
            DeserializePayloadHandle();
        }

        /// <inheritdoc/>
        protected internal override void SerializePayloadHandle()
        {
            List<byte> bytes = new List<byte>();
            bytes.AddRange(Enumerable.Reverse(BitConverter.GetBytes(WriteAddress)));
            bytes.AddRange(Data ? [0xFF, 0x00] : [0x00, 0x00]);
            Payload = bytes.ToArray();
        }

        /// <inheritdoc/>
        protected internal override void DeserializePayloadHandle()
        {
            if (Payload == null || Payload.Length < 4) return;
            WriteAddress = BitConverter.ToUInt16(Payload.Take(2).Reverse().ToArray(), 0);
            Data = Payload[2] == 0xFF;
        }
    }
}
