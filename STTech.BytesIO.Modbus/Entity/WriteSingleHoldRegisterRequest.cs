using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace STTech.BytesIO.Modbus
{
    public class WriteSingleHoldRegisterRequest:ModbusRequest
    {
        public byte[] WriteAddress { get; set; }
        public ushort Data { get; set; }

        public override byte[] GetBytes()
        {
            List<byte> bytes = new List<byte>();
            bytes.AddRange(WriteAddress.Reverse());
            bytes.AddRange(BitConverter.GetBytes(Data).Reverse());
            Payload = bytes.ToArray();
            return base.GetBytes();
        }
    }
}
