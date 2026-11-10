using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;

namespace STTech.BytesIO.Modbus
{
    /// <summary>
    /// 读输入寄存器响应
    /// </summary>
    public class ReadInputRegisterResponse : ModbusResponse
    {
        /// <summary>
        /// 数据字节长度
        /// </summary>
        public byte Length { get; protected set; }

        /// <summary>
        /// 寄存器数据
        /// </summary>
        public byte[] Values { get; protected set; }

        /// <summary>
        /// 初始化 <see cref="ReadInputRegisterResponse"/> 类的新实例。
        /// </summary>
        public ReadInputRegisterResponse() { }

        /// <summary>
        /// 构造读输入寄存器响应
        /// </summary>
        /// <param name="slaveId">从站地址</param>
        /// <param name="format">协议格式</param>
        /// <param name="values">寄存器数据</param>
        public ReadInputRegisterResponse(ushort slaveId, ModbusProtocolFormat format, byte[] values)
            : base(slaveId, FunctionCode.ReadInputRegister, format)
        {
            Values = values;
            Length = (byte)(values?.Length ?? 0);
        }

        /// <inheritdoc/>
        protected internal override void SerializePayloadHandle()
        {
            int valLen = Values?.Length ?? 0;
            byte[] bytes = new byte[1 + valLen];
            bytes[0] = Length;
            if (valLen > 0)
            {
                Buffer.BlockCopy(Values, 0, bytes, 1, valLen);
            }
            Payload = bytes;
        }

        /// <summary>
        /// 获取由原始数据转换的 ushort 数组 (零多余闭包分配)
        /// </summary>
        /// <returns>ushort 数组</returns>
        public ushort[] GetUInt16Array()
        {
            if (Values == null || Values.Length < 2) return Array.Empty<ushort>();
            int count = Values.Length / 2;
            ushort[] result = new ushort[count];
            for (int i = 0; i < count; i++)
            {
                result[i] = BinaryPrimitives.ReadUInt16BigEndian(Values.AsSpan(i * 2, 2));
            }
            return result;
        }

        /// <summary>
        /// 构造读输入寄存器响应 (基于字节数组)
        /// </summary>
        /// <param name="bytes">原始报文数据</param>
        public ReadInputRegisterResponse(byte[] bytes) : this(bytes, ModbusProtocolFormat.RTU)
        {
        }

        /// <summary>
        /// 构造读输入寄存器响应 (基于字节数组和指定协议格式)
        /// </summary>
        /// <param name="bytes">原始报文数据</param>
        /// <param name="format">协议格式</param>
        public ReadInputRegisterResponse(byte[] bytes, ModbusProtocolFormat format) : base(bytes, format)
        {
            InitValuesFromPayload();
        }

        /// <summary>
        /// 构造读输入寄存器响应 (基于解包上下文)
        /// </summary>
        /// <param name="context">解包上下文</param>
        public ReadInputRegisterResponse(STTech.BytesIO.Core.Component.UnpackContext context) : this(context, ModbusProtocolFormat.RTU)
        {
        }

        /// <summary>
        /// 构造读输入寄存器响应 (基于解包上下文和指定协议格式)
        /// </summary>
        /// <param name="context">解包上下文</param>
        /// <param name="format">协议格式</param>
        public ReadInputRegisterResponse(STTech.BytesIO.Core.Component.UnpackContext context, ModbusProtocolFormat format) : base(context, format)
        {
            InitValuesFromPayload();
        }

        private void InitValuesFromPayload()
        {
            if (IsSuccess && Payload != null && Payload.Length > 0)
            {
                Length = Payload[0];
                if (Payload.Length > 1)
                {
                    int copyLen = Math.Min((int)Length, Payload.Length - 1);
                    Values = new byte[copyLen];
                    Buffer.BlockCopy(Payload, 1, Values, 0, copyLen);
                }
            }
        }
    }
}
