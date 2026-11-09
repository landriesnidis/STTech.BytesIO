using STTech.BytesIO.Core;
using System;
using System.Buffers.Binary;
using System.ComponentModel;

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
        public WriteSingleHoldingRegisterRequest(byte[] data) : this(data, null)
        {
        }

        /// <summary>
        /// 构造写单个保持寄存器请求 (基于字节数组和指定协议格式)
        /// </summary>
        /// <param name="data">原始报文数据</param>
        /// <param name="format">协议格式</param>
        public WriteSingleHoldingRegisterRequest(byte[] data, ModbusProtocolFormat? format) : base(data, format)
        {
            DeserializePayloadHandle();
        }

        /// <summary>
        /// 构造写单个保持寄存器请求 (基于解包上下文)
        /// </summary>
        /// <param name="context">解包上下文</param>
        public WriteSingleHoldingRegisterRequest(STTech.BytesIO.Core.Component.UnpackContext context) : this(context, null)
        {
        }

        /// <summary>
        /// 构造写单个保持寄存器请求 (基于解包上下文和指定协议格式)
        /// </summary>
        /// <param name="context">解包上下文</param>
        /// <param name="format">协议格式</param>
        public WriteSingleHoldingRegisterRequest(STTech.BytesIO.Core.Component.UnpackContext context, ModbusProtocolFormat? format) : base(context, format)
        {
            DeserializePayloadHandle();
        }

        /// <inheritdoc/>
        protected internal override void SerializePayloadHandle()
        {
            byte[] bytes = new byte[4];
            BinaryPrimitives.WriteUInt16BigEndian(bytes.AsSpan(0, 2), WriteAddress);
            if (Data != null && Data.Length >= 2)
            {
                bytes[2] = Data[0];
                bytes[3] = Data[1];
            }
            Payload = bytes;
        }

        /// <inheritdoc/>
        protected internal override void DeserializePayloadHandle()
        {
            if (Payload == null || Payload.Length < 4) return;
            WriteAddress = BinaryPrimitives.ReadUInt16BigEndian(Payload.AsSpan(0, 2));
            Data = new byte[] { Payload[2], Payload[3] };
        }
    }
}
