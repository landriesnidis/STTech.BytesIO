using System;
using System.Buffers;
using System.Buffers.Binary;

namespace STTech.BytesIO.Modbus.Entity
{
    /// <summary>
    /// Modbus透明转发请求 (包含原始报文数据，不进行深层解析)
    /// </summary>
    public class ModbusForwardRequest : ModbusRequest
    {
        /// <summary>
        /// 原始报文内容
        /// </summary>
        public byte[] RawBytes { get; }

        /// <summary>
        /// 构造 Modbus 转发请求对象
        /// </summary>
        /// <param name="context">解包上下文</param>
        public ModbusForwardRequest(STTech.BytesIO.Core.Component.UnpackContext context) : this(context, null)
        {
        }

        /// <summary>
        /// 构造 Modbus 转发请求对象 (带指定协议格式)
        /// </summary>
        /// <param name="context">解包上下文</param>
        /// <param name="format">协议格式</param>
        public ModbusForwardRequest(STTech.BytesIO.Core.Component.UnpackContext context, ModbusProtocolFormat? format)
        {
            UnpackContext = context;
            RawBytes = context.Data.ToArray();

            ModbusProtocolFormat fmt = format ?? (RawBytes.Length > 0 && RawBytes[0] == (byte)':' ? ModbusProtocolFormat.ASCII : ModbusProtocolFormat.RTU);
            ProtocolFormat = fmt;

            switch (fmt)
            {
                case ModbusProtocolFormat.MBAP:
                    if (RawBytes.Length >= 8)
                    {
                        TransactionId = BinaryPrimitives.ReadUInt16BigEndian(RawBytes.AsSpan(0, 2));
                        ProtocolId = BinaryPrimitives.ReadUInt16BigEndian(RawBytes.AsSpan(2, 2));
                        SlaveId = RawBytes[6];
                        FunctionCode = (FunctionCode)RawBytes[7];
                    }
                    break;

                case ModbusProtocolFormat.ASCII:
                    if (RawBytes.Length >= 3)
                    {
                        SlaveId = Convert.ToByte(System.Text.Encoding.ASCII.GetString(RawBytes, 1, 2), 16);
                        if (RawBytes.Length >= 5)
                        {
                            FunctionCode = (FunctionCode)Convert.ToByte(System.Text.Encoding.ASCII.GetString(RawBytes, 3, 2), 16);
                        }
                    }
                    break;

                case ModbusProtocolFormat.RTU:
                default:
                    if (RawBytes.Length >= 1)
                    {
                        SlaveId = RawBytes[0];
                    }
                    if (RawBytes.Length >= 2)
                    {
                        FunctionCode = (FunctionCode)RawBytes[1];
                    }
                    break;
            }
        }

        protected internal override void SerializePayloadHandle()
        {
            // 透传模式下无需序列化
        }
    }
}
