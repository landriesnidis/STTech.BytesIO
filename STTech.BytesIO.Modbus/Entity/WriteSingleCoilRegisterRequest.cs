using STTech.BytesIO.Core;
using System;
using System.Buffers.Binary;
using System.ComponentModel;

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
        public WriteSingleCoilRegisterRequest(byte[] data) : this(data, null)
        {
        }

        /// <summary>
        /// 构造写单个线圈寄存器请求 (基于字节数组和指定协议格式)
        /// </summary>
        /// <param name="data">原始报文数据</param>
        /// <param name="format">协议格式</param>
        public WriteSingleCoilRegisterRequest(byte[] data, ModbusProtocolFormat? format) : base(data, format)
        {
            DeserializePayloadHandle();
        }

        /// <summary>
        /// 构造写单个线圈寄存器请求 (基于解包上下文)
        /// </summary>
        /// <param name="context">解包上下文</param>
        public WriteSingleCoilRegisterRequest(STTech.BytesIO.Core.Component.UnpackContext context) : this(context, null)
        {
        }

        /// <summary>
        /// 构造写单个线圈寄存器请求 (基于解包上下文和指定协议格式)
        /// </summary>
        /// <param name="context">解包上下文</param>
        /// <param name="format">协议格式</param>
        public WriteSingleCoilRegisterRequest(STTech.BytesIO.Core.Component.UnpackContext context, ModbusProtocolFormat? format) : base(context, format)
        {
            DeserializePayloadHandle();
        }

        /// <inheritdoc/>
        protected internal override void SerializePayloadHandle()
        {
            byte[] bytes = new byte[4];
            BinaryPrimitives.WriteUInt16BigEndian(bytes.AsSpan(0, 2), WriteAddress);
            bytes[2] = (byte)(Data ? 0xFF : 0x00);
            bytes[3] = 0x00;
            Payload = bytes;
        }

        /// <inheritdoc/>
        protected internal override void DeserializePayloadHandle()
        {
            if (Payload == null || Payload.Length < 4) return;
            WriteAddress = BinaryPrimitives.ReadUInt16BigEndian(Payload.AsSpan(0, 2));
            Data = Payload[2] == 0xFF;
        }
    }
}
