using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace STTech.BytesIO.Modbus
{
    public class ReadInputRegisterResponse : ModbusResponse
    {
        public byte Length { get; }
        public IEnumerable<byte> Values { get; }

        public ReadInputRegisterResponse(IEnumerable<byte> bytes) : base(bytes)
        {
            Length = Payload.ElementAt(0);
            Values = Payload.Skip(1);
        }
    }
}
