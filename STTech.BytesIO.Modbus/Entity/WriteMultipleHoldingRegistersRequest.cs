using STTech.BytesIO.Core;
using System;
using System.Buffers.Binary;
using System.ComponentModel;

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
        /// 获取由数据转换的 ushort 数组
        /// </summary>
        /// <returns></returns>
        public ushort[] GetUInt16Array()
        {
            if (Data == null || Data.Length < 2) return Array.Empty<ushort>();
            int count = Data.Length / 2;
            ushort[] result = new ushort[count];
            for (int i = 0; i < count; i++)
            {
                result[i] = BinaryPrimitives.ReadUInt16BigEndian(Data.AsSpan(i * 2, 2));
            }
            return result;
        }

        /// <summary>
        /// 构造写多个保持寄存器请求
        /// </summary>
        public WriteMultipleHoldingRegistersRequest() : base(FunctionCode.WriteMultipleHoldingRegisters) { }

        /// <summary>
        /// 构造写多个保持寄存器请求 (基于字节数组)
        /// </summary>
        /// <param name="data">原始报文数据</param>
        public WriteMultipleHoldingRegistersRequest(byte[] data) : this(data, null)
        {
        }

        /// <summary>
        /// 构造写多个保持寄存器请求 (基于字节数组和指定协议格式)
        /// </summary>
        /// <param name="data">原始报文数据</param>
        /// <param name="format">协议格式</param>
        public WriteMultipleHoldingRegistersRequest(byte[] data, ModbusProtocolFormat? format) : base(data, format)
        {
            DeserializePayloadHandle();
        }

        /// <summary>
        /// 构造写多个保持寄存器请求 (基于解包上下文)
        /// </summary>
        /// <param name="context">解包上下文</param>
        public WriteMultipleHoldingRegistersRequest(STTech.BytesIO.Core.Component.UnpackContext context) : this(context, null)
        {
        }

        /// <summary>
        /// 构造写多个保持寄存器请求 (基于解包上下文和指定协议格式)
        /// </summary>
        /// <param name="context">解包上下文</param>
        /// <param name="format">协议格式</param>
        public WriteMultipleHoldingRegistersRequest(STTech.BytesIO.Core.Component.UnpackContext context, ModbusProtocolFormat? format) : base(context, format)
        {
            DeserializePayloadHandle();
        }

        /// <inheritdoc/>
        protected internal override void SerializePayloadHandle()
        {
            ushort dataLen = (ushort)(Data?.Length ?? 0);
            ushort quantity = (ushort)(dataLen / 2);
            byte byteCount = (byte)dataLen;
            byte[] payload = new byte[5 + dataLen];

            BinaryPrimitives.WriteUInt16BigEndian(payload.AsSpan(0, 2), WriteAddress);
            BinaryPrimitives.WriteUInt16BigEndian(payload.AsSpan(2, 2), quantity);
            payload[4] = byteCount;

            if (Data != null && dataLen > 0)
            {
                Buffer.BlockCopy(Data, 0, payload, 5, dataLen);
            }
            Payload = payload;
        }

        /// <inheritdoc/>
        protected internal override void DeserializePayloadHandle()
        {
            if (Payload == null || Payload.Length < 5) return;
            WriteAddress = BinaryPrimitives.ReadUInt16BigEndian(Payload.AsSpan(0, 2));
            byte byteCount = Payload[4];
            if (Payload.Length >= 5 + byteCount)
            {
                byte[] data = new byte[byteCount];
                Buffer.BlockCopy(Payload, 5, data, 0, byteCount);
                Data = data;
            }
        }
    }
}
