using STTech.BytesIO.Core;
using STTech.BytesIO.Core.Component;
using System;
using System.Collections.Generic;
using System.Linq;

namespace STTech.BytesIO.Modbus
{
    /// <summary>
    /// Modbus协议解包器
    /// </summary>
    public class ModbusUnpacker : Unpacker<ModbusResponse>
    {
        public ModbusUnpacker(BytesClient client) : base(client, CalculatePacketLengthHandler)
        {

        }

        const int slaveIdLen = 1;
        const int functionCodeLen = 1;
        const int crcLen = 2;
        const int fixedHead = slaveIdLen + functionCodeLen;
        private static int CalculatePacketLengthHandler(IEnumerable<byte> bytes)
        {
            if (bytes.Count() < 2)
            {
                return 0;
            }


            switch ((FunctionCode)(short)(bytes.Skip(1).First()))
            {
                case FunctionCode.ReadCoilRegister:
                case FunctionCode.ReadDiscreteInputRegister:
                case FunctionCode.ReadHoldRegister:
                case FunctionCode.ReadInputRegister:
                    return fixedHead + 1 + (short)bytes.Skip(fixedHead).First() + crcLen;

                case FunctionCode.WriteSingleCoilRegister:
                case FunctionCode.WriteSingleHoldRegister:
                case FunctionCode.WriteMultipleCoilRegisters:
                case FunctionCode.WriteMultipleHoldRegisters:
                    return fixedHead + 4 + crcLen;

                default:
                    return bytes.Count();
            }
        }
    }
}
