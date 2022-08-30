using STTech.BytesIO.Core.Component;
using STTech.BytesIO.Modbus;
using System.Text;

namespace STTech.BytesIO.Modbus
{
    public interface IModbusClient : IUnpackerSupport<ModbusResponse>
    {
    }
}
