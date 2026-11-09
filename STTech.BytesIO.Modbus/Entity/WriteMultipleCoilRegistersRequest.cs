using STTech.BytesIO.Core;
using System;
using System.Buffers.Binary;
using System.ComponentModel;

namespace STTech.BytesIO.Modbus
{
    /// <summary>
    /// 写多个线圈寄存器请求
    /// </summary>
    public class WriteMultipleCoilRegistersRequest : ModbusRequest
    {
        /// <summary>
        /// 写入起始地址
        /// </summary>
        [Description("写入地址")]
        public ushort WriteAddress { get; set; }

        /// <summary>
        /// 写入数据列表
        /// </summary>
        [Browsable(false)]
        [Description("写入数据")]
        public bool[] Data { get; set; } = [false];

        /// <summary>
        /// 构造写多个线圈寄存器请求
        /// </summary>
        public WriteMultipleCoilRegistersRequest() : base(FunctionCode.WriteMultipleCoilRegisters)
        {
        }

        /// <summary>
        /// 构造写多个线圈寄存器请求 (基于字节数组)
        /// </summary>
        /// <param name="data">原始报文数据</param>
        public WriteMultipleCoilRegistersRequest(byte[] data) : this(data, null)
        {
        }

        /// <summary>
        /// 构造写多个线圈寄存器请求 (基于字节数组和指定协议格式)
        /// </summary>
        /// <param name="data">原始报文数据</param>
        /// <param name="format">协议格式</param>
        public WriteMultipleCoilRegistersRequest(byte[] data, ModbusProtocolFormat? format) : base(data, format)
        {
            DeserializePayloadHandle();
        }

        /// <summary>
        /// 构造写多个线圈寄存器请求 (基于解包上下文)
        /// </summary>
        /// <param name="context">解包上下文</param>
        public WriteMultipleCoilRegistersRequest(STTech.BytesIO.Core.Component.UnpackContext context) : this(context, null)
        {
        }

        /// <summary>
        /// 构造写多个线圈寄存器请求 (基于解包上下文和指定协议格式)
        /// </summary>
        /// <param name="context">解包上下文</param>
        /// <param name="format">协议格式</param>
        public WriteMultipleCoilRegistersRequest(STTech.BytesIO.Core.Component.UnpackContext context, ModbusProtocolFormat? format) : base(context, format)
        {
            DeserializePayloadHandle();
        }

        /// <inheritdoc/>
        protected internal override void SerializePayloadHandle()
        {
            ushort quantity = (ushort)(Data?.Length ?? 0);
            byte byteCount = (byte)((quantity + 7) / 8);
            byte[] payload = new byte[5 + byteCount];

            BinaryPrimitives.WriteUInt16BigEndian(payload.AsSpan(0, 2), WriteAddress);
            BinaryPrimitives.WriteUInt16BigEndian(payload.AsSpan(2, 2), quantity);
            payload[4] = byteCount;

            if (Data != null)
            {
                for (int i = 0; i < quantity; i++)
                {
                    if (Data[i])
                    {
                        payload[5 + (i / 8)] |= (byte)(1 << (i % 8));
                    }
                }
            }

            Payload = payload;
        }

        /// <inheritdoc/>
        protected internal override void DeserializePayloadHandle()
        {
            if (Payload == null || Payload.Length < 5) return;
            WriteAddress = BinaryPrimitives.ReadUInt16BigEndian(Payload.AsSpan(0, 2));
            ushort quantity = BinaryPrimitives.ReadUInt16BigEndian(Payload.AsSpan(2, 2));
            byte byteCount = Payload[4];

            if (Payload.Length >= 5 + byteCount)
            {
                bool[] data = new bool[quantity];
                for (int i = 0; i < quantity; i++)
                {
                    int byteIndex = 5 + (i / 8);
                    if (byteIndex < Payload.Length)
                    {
                        data[i] = (Payload[byteIndex] & (1 << (i % 8))) != 0;
                    }
                }
                Data = data;
            }
        }
    }
}
