using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace STTech.BytesIO.Modbus
{
    /// <summary>
    /// 读取寄存器请求基类
    /// </summary>
    public class ReadRegisterRequest : ModbusRequest
    {
        /// <summary>
        /// 起始地址
        /// </summary>
        [Description("起始地址")]
        public ushort StartAddress { get; set; }

        /// <summary>
        /// 读取长度
        /// </summary>
        [Description("读取长度")]
        public ushort Length { get; set; } = 1;

        internal ReadRegisterRequest(FunctionCode functionCode) : base(functionCode)
        {

        }

        /// <summary>
        /// 构造读取寄存器请求 (基于字节数组)
        /// </summary>
        /// <param name="data">原始报文数据</param>
        public ReadRegisterRequest(byte[] data) : base(data)
        {
            DeserializePayloadHandle();
        }

        /// <summary>
        /// 构造读取寄存器请求 (基于解包上下文)
        /// </summary>
        /// <param name="context">解包上下文</param>
        public ReadRegisterRequest(STTech.BytesIO.Core.Component.UnpackContext context) : base(context)
        {
            DeserializePayloadHandle();
        }

        /// <inheritdoc/>
        protected internal override void SerializePayloadHandle()
        {
            List<byte> bytes = new List<byte>();
            bytes.AddRange(Enumerable.Reverse(BitConverter.GetBytes(StartAddress)));
            bytes.AddRange(Enumerable.Reverse(BitConverter.GetBytes(Length)));
            Payload = bytes.ToArray();
        }

        /// <inheritdoc/>
        protected internal override void DeserializePayloadHandle()
        {
            if (Payload == null || Payload.Length < 4) return;
            StartAddress = BitConverter.ToUInt16(Payload.Take(2).Reverse().ToArray(), 0);
            Length = BitConverter.ToUInt16(Payload.Skip(2).Take(2).Reverse().ToArray(), 0);
        }
    }
}
