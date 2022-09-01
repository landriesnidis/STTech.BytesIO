using STTech.BytesIO.Core.Entity;
using System.Collections.Generic;
using System.Linq;

namespace STTech.BytesIO.Modbus
{
    public class ModbusResponse : Response
    {
        public byte SlaveId { get; }
        public FunctionCode FunctionCode { get; }
        public IEnumerable<byte> Crc { get; }

        protected IEnumerable<byte> Payload { get; }

        public ModbusResponse(IEnumerable<byte> bytes) : base(bytes)
        {
            SlaveId = bytes.ElementAt(0);
            FunctionCode = (FunctionCode)bytes.ElementAt(1);
            var arr = bytes.Skip(2);
            var payloadLen = arr.Count() - 2;
            Payload = arr.Take(payloadLen);
            Crc = bytes.Skip(payloadLen);
        }
    }

}
