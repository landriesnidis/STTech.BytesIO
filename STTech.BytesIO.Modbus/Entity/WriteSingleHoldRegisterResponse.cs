using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace STTech.BytesIO.Modbus
{
    public class WriteSingleHoldRegisterResponse : ModbusResponse
    {
        public byte[] WriteAddress { get; }
        public IEnumerable<byte> Values { get; }

        public WriteSingleHoldRegisterResponse(IEnumerable<byte> bytes) : base(bytes)
        {
            WriteAddress = Payload.Take(2).ToArray();
            Values = Payload.Skip(2).ToArray();
        }
    }
}
