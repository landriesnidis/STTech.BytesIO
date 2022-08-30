using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace STTech.BytesIO.Modbus
{
    public class WriteMultipleHoldRegistersRequest:ModbusRequest
    {
        public ushort WriteAddress { get; set; }
        public ushort WriteLength { get; set; }

        public byte[] Data { get; set; }

        public override byte[] GetBytes()
        {
            List<byte> bytes = new List<byte>();
            bytes.AddRange(BitConverter.GetBytes(WriteAddress).Reverse());
            bytes.AddRange(BitConverter.GetBytes(WriteLength).Reverse());
            bytes.AddRange((byte[])Data.Reverse());
            Payload = bytes.ToArray();
            return base.GetBytes();
        }
    }
}
