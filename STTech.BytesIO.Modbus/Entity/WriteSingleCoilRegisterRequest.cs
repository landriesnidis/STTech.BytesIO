using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace STTech.BytesIO.Modbus
{
    public class WriteSingleCoilRegisterRequest:ModbusRequest
    {
        public ushort WriteAddress { get; set; }
        public ushort Data { get; set; }

        public override byte[] GetBytes()
        {
            List<byte> bytes = new List<byte>();
            bytes.AddRange(BitConverter.GetBytes(WriteAddress).Reverse());
            bytes.AddRange(BitConverter.GetBytes(Data).Reverse());
            Payload = bytes.ToArray();
            return base.GetBytes();
        }
    }
}
