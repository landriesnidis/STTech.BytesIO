using STTech.BytesIO.Core;
using System;
using System.Buffers.Binary;
using System.ComponentModel;

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

        /// <summary>
        /// 初始化 <see cref="ReadRegisterRequest"/> 类的新实例。
        /// </summary>
        public ReadRegisterRequest() : base()
        {
        }

        /// <summary>
        /// 构造读取寄存器请求
        /// </summary>
        /// <param name="functionCode">功能码</param>
        public ReadRegisterRequest(FunctionCode functionCode) : base(functionCode)
        {
        }

        /// <summary>
        /// 构造读取寄存器请求 (基于字节数组)
        /// </summary>
        /// <param name="data">原始报文数据</param>
        public ReadRegisterRequest(byte[] data) : this(data, null)
        {
        }

        /// <summary>
        /// 构造读取寄存器请求 (基于字节数组和指定协议格式)
        /// </summary>
        /// <param name="data">原始报文数据</param>
        /// <param name="format">协议格式</param>
        public ReadRegisterRequest(byte[] data, ModbusProtocolFormat? format) : base(data, format)
        {
            DeserializePayloadHandle();
        }

        /// <summary>
        /// 构造读取寄存器请求 (基于解包上下文)
        /// </summary>
        /// <param name="context">解包上下文</param>
        public ReadRegisterRequest(STTech.BytesIO.Core.Component.UnpackContext context) : this(context, null)
        {
        }

        /// <summary>
        /// 构造读取寄存器请求 (基于解包上下文和指定协议格式)
        /// </summary>
        /// <param name="context">解包上下文</param>
        /// <param name="format">协议格式</param>
        public ReadRegisterRequest(STTech.BytesIO.Core.Component.UnpackContext context, ModbusProtocolFormat? format) : base(context, format)
        {
            DeserializePayloadHandle();
        }

        /// <inheritdoc/>
        protected internal override void SerializePayloadHandle()
        {
            byte[] bytes = new byte[4];
            BinaryPrimitives.WriteUInt16BigEndian(bytes.AsSpan(0, 2), StartAddress);
            BinaryPrimitives.WriteUInt16BigEndian(bytes.AsSpan(2, 2), Length);
            Payload = bytes;
        }

        /// <inheritdoc/>
        protected internal override void DeserializePayloadHandle()
        {
            if (Payload == null || Payload.Length < 4) return;
            StartAddress = BinaryPrimitives.ReadUInt16BigEndian(Payload.AsSpan(0, 2));
            Length = BinaryPrimitives.ReadUInt16BigEndian(Payload.AsSpan(2, 2));
        }
    }
}
