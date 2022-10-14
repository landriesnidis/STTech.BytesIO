using STTech.BytesIO.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace STTech.BytesIO.Modbus
{
    public abstract class ModbusRequest : IRequest
    {
        /// <summary>
        /// 从机地址
        /// </summary>
        [Description("从机地址")]
        public byte SlaveId { get; set; }

        /// <summary>
        /// 功能码
        /// </summary>
        [Description("功能码")]
        protected FunctionCode FunctionCode { get; set; }

        protected ModbusRequest(FunctionCode functionCode)
        {
            FunctionCode = functionCode;
        }

        /// <summary>
        /// 有效荷载
        /// </summary>
        protected IEnumerable<byte> Payload { get; set; }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <returns></returns>
        public virtual byte[] GetBytes()
        {
            List<byte> bytes = new List<byte>();
            bytes.Add(SlaveId);
            bytes.Add((byte)FunctionCode);
            bytes.AddRange(Payload);

            return CRC16(bytes.ToArray());
        }

        private static byte[] CRC16(byte[] value, ushort poly = 0xA001, ushort crcInit = 0xFFFF)
        {
            if (value == null || !value.Any())
                throw new ArgumentException("");

            //运算
            ushort crc = crcInit;
            for (int i = 0; i < value.Length; i++)
            {
                crc = (ushort)(crc ^ (value[i]));
                for (int j = 0; j < 8; j++)
                {
                    crc = (crc & 1) != 0 ? (ushort)((crc >> 1) ^ poly) : (ushort)(crc >> 1);
                }
            }
            byte hi = (byte)((crc & 0xFF00) >> 8);
            byte lo = (byte)(crc & 0x00FF);
            List<byte> buffer = new List<byte>();
            buffer.AddRange(value);
            buffer.Add(lo);
            buffer.Add(hi);
            return buffer.ToArray();
        }
    }

}
