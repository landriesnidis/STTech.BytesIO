using System;
using System.Buffers;

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
        public ModbusForwardRequest(STTech.BytesIO.Core.Component.UnpackContext context) 
        {
            UnpackContext = context;
            RawBytes = context.Data.ToArray();
            
            ProtocolFormat = RawBytes[0] == (byte)':' ? ModbusProtocolFormat.ASCII : ModbusProtocolFormat.RTU;
            if (ProtocolFormat == ModbusProtocolFormat.ASCII)
            {
                if (RawBytes.Length >= 3)
                {
                    SlaveId = byte.Parse(System.Text.Encoding.ASCII.GetString(RawBytes, 1, 2));
                }
            }
            else
            {
                if (RawBytes.Length >= 1)
                {
                    SlaveId = RawBytes[0];
                }
            }
        }

        protected internal override void SerializePayloadHandle()
        {
            // 透传模式下无需序列化
        }
    }
}
