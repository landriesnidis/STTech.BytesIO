using STTech.BytesIO.Core.Component;
using STTech.BytesIO.Tcp;

namespace STTech.BytesIO.Modbus
{
    public class ModbusTcpClient : TcpClient, IModbusClient
    {
        public Unpacker<ModbusResponse> Unpacker { get; }

        public ModbusTcpClient()
        {
            Unpacker = new ModbusUnpacker(this);
        }
    }
}
