using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace STTech.BytesIO.Modbus
{
    public class WriteMultipleHoldRegistersRequest : ModbusRequest
    {
        public ushort WriteAddress { get; set; }
        public ushort WriteLength { get; set; }

        public byte[] Data { get; set; }


        public WriteMultipleHoldRegistersRequest() : base(FunctionCode.WriteMultipleHoldRegisters)
        {

        }
        public override byte[] GetBytes()
        {
            List<byte> bytes = new List<byte>();
            bytes.AddRange(BitConverter.GetBytes(WriteAddress).Reverse());
            bytes.AddRange(BitConverter.GetBytes(WriteLength).Reverse());
            bytes.Add((byte)Data.Length);
            bytes.AddRange(Data.Reverse());
            Payload = bytes.ToArray();
            return base.GetBytes();
        }
    }
}
