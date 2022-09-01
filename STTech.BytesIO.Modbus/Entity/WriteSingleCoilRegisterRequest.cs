using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace STTech.BytesIO.Modbus
{
    public class WriteSingleCoilRegisterRequest : ModbusRequest
    {
        public ushort WriteAddress { get; set; }
        public bool Data { get; set; }

        public WriteSingleCoilRegisterRequest() : base(FunctionCode.WriteSingleCoilRegister)
        {

        }
        public override byte[] GetBytes()
        {
            var temp = Data ? CoilOn : CoilOff;
            var value = BitConverter.GetBytes(temp).Reverse();
            List<byte> bytes = new List<byte>();
            bytes.AddRange(BitConverter.GetBytes(WriteAddress).Reverse());
            bytes.AddRange(value);
            Payload = bytes.ToArray();
            return base.GetBytes();
        }
    }
}
