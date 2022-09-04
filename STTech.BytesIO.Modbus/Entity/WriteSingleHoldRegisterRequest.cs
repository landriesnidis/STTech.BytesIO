using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace STTech.BytesIO.Modbus
{
    public class WriteSingleHoldRegisterRequest : ModbusRequest
    {
        [Description("写入地址")]
        public ushort WriteAddress { get; set; }

        [Description("写入数据")]
        public byte[] Data { get; set; } = new byte[0];

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
