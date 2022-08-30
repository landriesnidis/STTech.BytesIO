using STTech.BytesIO.Core.Entity;
using System.Collections.Generic;

namespace STTech.BytesIO.Modbus
{
    public abstract class ModbusRequest : IRequest
    {
        public byte SlaveId { get; set; }
        public FunctionCode FunctionCode { get; set; }

        protected IEnumerable<byte> Payload { get; set; }
        public byte[] Crc => new byte[2];

        public virtual byte[] GetBytes()
        {
            List<byte> bytes = new List<byte>();
            bytes.Add(SlaveId);
            bytes.Add((byte)FunctionCode);
            bytes.AddRange(Payload);
            bytes.AddRange(Crc);

            return bytes.ToArray();
        }
    }

}
