using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace STTech.BytesIO.Modbus
{
    public class WriteSingleCoilRegisterRequest : ModbusRequest
    {
        [Description("写入地址")]
        public ushort WriteAddress { get; set; }

        [Description("写入数据")]
        public bool Data { get; set; }

        private const ushort CoilOn = 0xFF00;
        private const ushort CoilOff = 0x0000;

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
