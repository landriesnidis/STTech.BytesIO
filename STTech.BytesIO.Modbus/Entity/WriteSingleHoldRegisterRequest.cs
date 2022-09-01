using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace STTech.BytesIO.Modbus
{
    public class WriteSingleHoldRegisterRequest : ModbusRequest
    {
        public ushort WriteAddress { get; set; }
        public byte[] Data { get; set; }

        public WriteSingleHoldRegisterRequest() : base(FunctionCode.WriteSingleHoldRegister)
        {

        }

        public override byte[] GetBytes()

        {
            List<byte> bytes = new List<byte>();
            bytes.AddRange(BitConverter.GetBytes(WriteAddress).Reverse());
            bytes.AddRange(Data.Reverse());
            Payload = bytes.ToArray();
            return base.GetBytes();
        }
    }
}
