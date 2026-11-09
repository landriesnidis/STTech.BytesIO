using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;

namespace STTech.BytesIO.Modbus
{
    /// <summary>
    /// 写入寄存器响应
    /// </summary>
    public class WriteRegisterResponse : ModbusResponse
    {
        /// <summary>
        /// 写入地址
        /// </summary>
        public ushort WriteAddress { get; protected set; }

        /// <summary>
        /// 写入的数据
        /// </summary>
        public byte[] Values { get; protected set; }

        public WriteRegisterResponse() { }

        /// <summary>
        /// 构造写入寄存器响应
        /// </summary>
        /// <param name="slaveId">从站地址</param>
        /// <param name="functionCode">功能码</param>
        /// <param name="format">协议格式</param>
        /// <param name="writeAddress">写入地址</param>
        /// <param name="values">写入的数据</param>
        public WriteRegisterResponse(ushort slaveId, FunctionCode functionCode, ModbusProtocolFormat format, ushort writeAddress, byte[] values) 
            : base(slaveId, functionCode, format)
        {
            WriteAddress = writeAddress;
            Values = values;
        }

        protected internal override void SerializePayloadHandle()
        {
            byte[] bytes = new byte[2 + (Values?.Length ?? 0)];
            BinaryPrimitives.WriteUInt16BigEndian(bytes.AsSpan(0, 2), WriteAddress);
            if (Values != null && Values.Length > 0)
            {
                Buffer.BlockCopy(Values, 0, bytes, 2, Values.Length);
            }
            Payload = bytes;
        }

        /// <summary>
        /// 构造写入寄存器响应 (基于字节数组)
        /// </summary>
        /// <param name="bytes">原始报文数据</param>
        public WriteRegisterResponse(byte[] bytes) : this(bytes, ModbusProtocolFormat.RTU)
        {
        }

        /// <summary>
        /// 构造写入寄存器响应 (基于字节数组和指定协议格式)
        /// </summary>
        /// <param name="bytes">原始报文数据</param>
        /// <param name="format">协议格式</param>
        public WriteRegisterResponse(byte[] bytes, ModbusProtocolFormat format) : base(bytes, format)
        {
            InitValuesFromPayload();
        }

        /// <summary>
        /// 构造写入寄存器响应 (基于解包上下文)
        /// </summary>
        /// <param name="context">解包上下文</param>
        public WriteRegisterResponse(STTech.BytesIO.Core.Component.UnpackContext context) : this(context, ModbusProtocolFormat.RTU)
        {
        }

        /// <summary>
        /// 构造写入寄存器响应 (基于解包上下文和指定协议格式)
        /// </summary>
        /// <param name="context">解包上下文</param>
        /// <param name="format">协议格式</param>
        public WriteRegisterResponse(STTech.BytesIO.Core.Component.UnpackContext context, ModbusProtocolFormat format) : base(context, format)
        {
            InitValuesFromPayload();
        }

        private void InitValuesFromPayload()
        {
            if (IsSuccess && Payload != null && Payload.Length >= 2)
            {
                WriteAddress = BinaryPrimitives.ReadUInt16BigEndian(Payload.AsSpan(0, 2));
                if (Payload.Length > 2)
                {
                    Values = Payload.AsSpan(2).ToArray();
                }
            }
        }

        /// <summary>
        /// 获取由转数据转换的ushort值 (零堆分配)
        /// </summary>
        /// <returns></returns>
        public ushort GetUInt16()
        {
            if (Values == null || Values.Length < 2) return 0;
            return BinaryPrimitives.ReadUInt16BigEndian(Values.AsSpan(0, 2));
        }
    }
}
