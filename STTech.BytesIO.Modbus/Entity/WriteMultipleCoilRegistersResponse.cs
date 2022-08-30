using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace STTech.BytesIO.Modbus
{
    public class WriteMultipleCoilRegistersResponse : ModbusResponse
    {
        public byte[] WriteAddress { get; }
        public IEnumerable<byte> WriteLength { get; }

        public WriteMultipleCoilRegistersResponse(IEnumerable<byte> bytes) : base(bytes)
        {
            WriteAddress = Payload.Take(2).ToArray();
            WriteLength = Payload.Skip(2).ToArray();
        }
    }
}
