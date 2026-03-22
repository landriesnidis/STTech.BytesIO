using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace STTech.BytesIO.Modbus
{
    /// <summary>
    /// 写单个保持寄存器请求
    /// </summary>
    public class WriteSingleHoldingRegisterRequest : ModbusRequest
    {
        /// <summary>
        /// 写入地址
        /// </summary>
        [Description("写入地址")]
        public ushort WriteAddress { get; set; }

        /// <summary>
        /// 写入数据 (2 字节)
        /// </summary>
        [Browsable(false)]
        [Description("写入数据")]
        public byte[] Data { get; set; } = new byte[2];

        /// <summary>
        /// 构造写单个保持寄存器请求
        /// </summary>
        public WriteSingleHoldingRegisterRequest() : base(FunctionCode.WriteSingleHoldingRegister) { }

        /// <summary>
        /// 构造写单个保持寄存器请求 (基于字节数组)
        /// </summary>
        /// <param name="data">原始报文数据</param>
        public WriteSingleHoldingRegisterRequest(byte[] data) : base(data)
        {
            DeserializePayloadHandle();
        }

        /// <summary>
        /// 构造写单个保持寄存器请求 (基于解包上下文)
        /// </summary>
        /// <param name="context">解包上下文</param>
        public WriteSingleHoldingRegisterRequest(STTech.BytesIO.Core.Component.UnpackContext context) : base(context)
        {
            DeserializePayloadHandle();
        }

        /// <inheritdoc/>
        protected internal override void SerializePayloadHandle()
        {
            List<byte> bytes = [.. Enumerable.Reverse(BitConverter.GetBytes(WriteAddress)), .. Data];
            Payload = bytes.ToArray();
        }

        /// <inheritdoc/>
        protected internal override void DeserializePayloadHandle()
        {
            if (Payload == null || Payload.Length < 4) return;
            WriteAddress = BitConverter.ToUInt16(Payload.Take(2).Reverse().ToArray(), 0);
            Data = Payload.Skip(2).Take(2).ToArray();
        }
    }
}
