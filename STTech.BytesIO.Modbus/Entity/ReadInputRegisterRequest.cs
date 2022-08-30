﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace STTech.BytesIO.Modbus
{
    public class ReadInputRegisterRequest : ModbusRequest
    {
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
