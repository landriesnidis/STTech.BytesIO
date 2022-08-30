using STTech.BytesIO.Core.Component;
using STTech.BytesIO.Serial;

namespace STTech.BytesIO.Modbus
{
    public class ModbusRtuClient : SerialClient, IModbusClient
    {
        public Unpacker<ModbusResponse> Unpacker { get; }
        public ModbusRtuClient()
        {
            Unpacker = new Unpacker<ModbusResponse>(this);
        }
    }
}
