using STTech.BytesIO.Core;
using STTech.BytesIO.Core.Component;
using System;
using System.Collections.Generic;
using System.Linq;

namespace STTech.BytesIO.Modbus
{
    public class ModbusUnpacker : Unpacker<ModbusResponse>
    {
        public ModbusUnpacker(BytesClient client, Func<IEnumerable<byte>, int> calculatePacketLengthHandler = null) : base(client, calculatePacketLengthHandler)
        {

        }

        const int slaveIdLen = 1;
        const int functionCodeLen = 1;
        const int crcLen = 2;
        const int fixedHead = slaveIdLen + functionCodeLen;
        protected override int CalculatePacketLength(IEnumerable<byte> bytes)
        {
            if (bytes.Count() < 2)
            {
                return 0;
            }


            switch ((FunctionCode)Convert.ToInt16(bytes.Skip(1).Take(1)))
            {
                case FunctionCode.ReadCoilRegister:
                case FunctionCode.ReadDiscreteInputRegister:
                case FunctionCode.ReadHoldRegister:
                case FunctionCode.ReadInputRegister:
                    return fixedHead + 1 + Convert.ToInt16(bytes.Skip(fixedHead).Take(1)) + crcLen;

                case FunctionCode.WriteSingleCoilRegister:
                case FunctionCode.WriteSingleHoldRegister:
                case FunctionCode.WriteMultipleCoilRegisters:
                case FunctionCode.WriteMultipleHoldRegisters:
                    return fixedHead + 4 + crcLen;

                default:
                    return 0;
            }
        }
    }
}
