using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace STTech.BytesIO.Modbus
{
    public class WriteMultipleHoldRegistersRequest : ModbusRequest
    {
        [Description("写入地址")]
        public ushort WriteAddress { get; set; }

        [Description("写入长度")]
        public ushort WriteLength { get; set; } = 1;

        [Description("写入数据")]
        public byte[] Data { get; set; } = new byte[0];

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
