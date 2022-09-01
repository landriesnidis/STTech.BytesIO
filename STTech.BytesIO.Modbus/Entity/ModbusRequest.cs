using STTech.BytesIO.Core.Entity;
using System;
using System.Collections.Generic;
using System.Linq;

namespace STTech.BytesIO.Modbus
{
    public abstract class ModbusRequest : IRequest
    {
        public byte SlaveId { get; set; }
        public FunctionCode FunctionCode { get; set; }

        protected ModbusRequest(FunctionCode functionCode)
        {
            FunctionCode = functionCode;
        }

        protected IEnumerable<byte> Payload { get; set; }

        public const ushort CoilOn = 0xFF00;
        public const ushort CoilOff = 0x0000;

        public virtual byte[] GetBytes()
        {
            List<byte> bytes = new List<byte>();
            bytes.Add(SlaveId);
            bytes.Add((byte)FunctionCode);
            bytes.AddRange(Payload);

            return CRC16(bytes.ToArray());
        }

        public static byte[] CRC16(byte[] value, ushort poly = 0xA001, ushort crcInit = 0xFFFF)
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
