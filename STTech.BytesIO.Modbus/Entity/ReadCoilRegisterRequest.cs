using System;
using System.Collections.Generic;
using System.Linq;

namespace STTech.BytesIO.Modbus
{
    public class ReadCoilRegisterRequest : ModbusRequest
    {
        public ReadCoilRegisterRequest() : base(FunctionCode.ReadCoilRegister)
        {
        }

        public ushort StartAddress { get; set; }
        public ushort Length { get; set; }

        public override byte[] GetBytes()
        {
            List<byte> bytes = new List<byte>();
            bytes.AddRange(BitConverter.GetBytes(StartAddress).Reverse());
            bytes.AddRange(BitConverter.GetBytes(Length).Reverse());
            Payload = bytes.ToArray();

            return base.GetBytes();
        }
    }

}
