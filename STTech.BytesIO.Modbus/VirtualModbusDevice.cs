using STTech.BytesIO.Core;
using STTech.BytesIO.Modbus.Monitors;
using System;
using System.Collections.Generic;
using System.Linq;

namespace STTech.BytesIO.Modbus
{
    /// <summary>
    /// 虚拟 Modbus 设备
    /// 支持动态挂载连续寄存器监视器，并自动答复 Modbus TCP/RTU 服务端接收的读写请求。
    /// 可以触发寄存器的数值变更事件。
    /// </summary>
    public class VirtualModbusDevice
    {
        private readonly ModbusServer _server;
        private readonly List<IModbusRegister> _registers = new List<IModbusRegister>();

        /// <summary>
        /// 获取或设置 Modbus 服务端
        /// </summary>
        public ModbusServer Server => _server;

        /// <summary>
        /// 构造虚拟 Modbus 设备
        /// </summary>
        /// <param name="server"></param>
        public VirtualModbusDevice(ModbusServer server)
        {
            _server = server ?? throw new ArgumentNullException(nameof(server));

            _server.ReadCoilRegisterRequested += Server_ReadCoilRegisterRequested;
            _server.ReadDiscreteInputRegisterRequested += Server_ReadDiscreteInputRegisterRequested;
            _server.ReadHoldingRegisterRequested += Server_ReadHoldingRegisterRequested;
            _server.ReadInputRegisterRequested += Server_ReadInputRegisterRequested;

            _server.WriteSingleCoilRegisterRequested += Server_WriteSingleCoilRegisterRequested;
            _server.WriteMultipleCoilRegistersRequested += Server_WriteMultipleCoilRegistersRequested;
            _server.WriteSingleHoldingRegisterRequested += Server_WriteSingleHoldingRegisterRequested;
            _server.WriteMultipleHoldingRegistersRequested += Server_WriteMultipleHoldingRegistersRequested;
        }

        /// <summary>
        /// 挂载寄存器内存区 (可以使用例如 HoldingRegisterValueMonitor)
        /// </summary>
        public void Mount(IModbusRegister register)
        {
            if (register == null) throw new ArgumentNullException(nameof(register));
            if (!_registers.Contains(register)) _registers.Add(register);
        }

        /// <summary>
        /// 卸载寄存器内存区
        /// </summary>
        public void Unmount(IModbusRegister register)
        {
            _registers.Remove(register);
        }

        private T GetRegister<T>(ModbusRegisterType type, ushort address, ushort length) where T : class, IModbusRegister
        {
            var reg = _registers.FirstOrDefault(r => r.RegisterType == type && r.Contains(address, length));
            return reg as T;
        }

        private ushort ParseBigEndianUInt16(byte[] data, int offset)
        {
            return (ushort)((data[offset] << 8) | data[offset + 1]);
        }
        
        private byte[] GetBytesBigEndian(ushort value)
        {
            return new byte[] { (byte)(value >> 8), (byte)(value & 0xFF) };
        }

        private void Server_ReadCoilRegisterRequested(object sender, ModbusReadRequestedEventArgs<bool[]> e)
        {
            var reg = GetRegister<IModbusRegister<bool>>(ModbusRegisterType.Coil, e.Request.StartAddress, e.Request.Length);
            if (reg != null)
            {
                var offset = e.Request.StartAddress - reg.StartAddress;
                e.ResponseData = new bool[e.Request.Length];
                Array.Copy(reg.Source, offset, e.ResponseData, 0, e.Request.Length);
            }
            else
            {
                e.IsError = true;
                e.ErrorCode = ModbusErrorCode.IllegalDataAddress;
            }
        }

        private void Server_ReadDiscreteInputRegisterRequested(object sender, ModbusReadRequestedEventArgs<bool[]> e)
        {
            var reg = GetRegister<IModbusRegister<bool>>(ModbusRegisterType.DiscreteInput, e.Request.StartAddress, e.Request.Length);
            if (reg != null)
            {
                var offset = e.Request.StartAddress - reg.StartAddress;
                e.ResponseData = new bool[e.Request.Length];
                Array.Copy(reg.Source, offset, e.ResponseData, 0, e.Request.Length);
            }
            else
            {
                e.IsError = true;
                e.ErrorCode = ModbusErrorCode.IllegalDataAddress;
            }
        }

        private void Server_ReadHoldingRegisterRequested(object sender, ModbusReadRequestedEventArgs<byte[]> e)
        {
            var reg = GetRegister<IModbusRegister<ushort>>(ModbusRegisterType.Holding, e.Request.StartAddress, e.Request.Length);
            if (reg != null)
            {
                var offset = e.Request.StartAddress - reg.StartAddress;
                e.ResponseData = new byte[e.Request.Length * 2];
                for (int i = 0; i < e.Request.Length; i++)
                {
                    var valBytes = GetBytesBigEndian(reg.Source[offset + i]);
                    e.ResponseData[i * 2] = valBytes[0];
                    e.ResponseData[i * 2 + 1] = valBytes[1];
                }
            }
            else
            {
                e.IsError = true;
                e.ErrorCode = ModbusErrorCode.IllegalDataAddress;
            }
        }

        private void Server_ReadInputRegisterRequested(object sender, ModbusReadRequestedEventArgs<byte[]> e)
        {
            var reg = GetRegister<IModbusRegister<ushort>>(ModbusRegisterType.Input, e.Request.StartAddress, e.Request.Length);
            if (reg != null)
            {
                var offset = e.Request.StartAddress - reg.StartAddress;
                e.ResponseData = new byte[e.Request.Length * 2];
                for (int i = 0; i < e.Request.Length; i++)
                {
                    var valBytes = GetBytesBigEndian(reg.Source[offset + i]);
                    e.ResponseData[i * 2] = valBytes[0];
                    e.ResponseData[i * 2 + 1] = valBytes[1];
                }
            }
            else
            {
                e.IsError = true;
                e.ErrorCode = ModbusErrorCode.IllegalDataAddress;
            }
        }

        private void Server_WriteSingleCoilRegisterRequested(object sender, ModbusWriteRequestedEventArgs<WriteSingleCoilRegisterRequest> e)
        {
            var reg = GetRegister<IModbusRegister<bool>>(ModbusRegisterType.Coil, e.Request.WriteAddress, 1);
            if (reg != null)
            {
                var offset = e.Request.WriteAddress - reg.StartAddress;
                reg.Source[offset] = e.Request.Data;
                reg.Notify();
            }
            else
            {
                e.IsError = true;
                e.ErrorCode = ModbusErrorCode.IllegalDataAddress;
            }
        }

        private void Server_WriteMultipleCoilRegistersRequested(object sender, ModbusWriteRequestedEventArgs<WriteMultipleCoilRegistersRequest> e)
        {
            ushort length = (ushort)e.Request.Data.Length;
            var reg = GetRegister<IModbusRegister<bool>>(ModbusRegisterType.Coil, e.Request.WriteAddress, length);
            if (reg != null)
            {
                var offset = e.Request.WriteAddress - reg.StartAddress;
                for (int i = 0; i < length; i++)
                {
                    reg.Source[offset + i] = e.Request.Data[i];
                }
                reg.Notify();
            }
            else
            {
                e.IsError = true;
                e.ErrorCode = ModbusErrorCode.IllegalDataAddress;
            }
        }

        private void Server_WriteSingleHoldingRegisterRequested(object sender, ModbusWriteRequestedEventArgs<WriteSingleHoldingRegisterRequest> e)
        {
            var reg = GetRegister<IModbusRegister<ushort>>(ModbusRegisterType.Holding, e.Request.WriteAddress, 1);
            if (reg != null)
            {
                var offset = e.Request.WriteAddress - reg.StartAddress;
                reg.Source[offset] = ParseBigEndianUInt16(e.Request.Data, 0);
                reg.Notify();
            }
            else
            {
                e.IsError = true;
                e.ErrorCode = ModbusErrorCode.IllegalDataAddress;
            }
        }

        private void Server_WriteMultipleHoldingRegistersRequested(object sender, ModbusWriteRequestedEventArgs<WriteMultipleHoldingRegistersRequest> e)
        {
            ushort length = (ushort)(e.Request.Data.Length / 2);
            var reg = GetRegister<IModbusRegister<ushort>>(ModbusRegisterType.Holding, e.Request.WriteAddress, length);
            if (reg != null)
            {
                var offset = e.Request.WriteAddress - reg.StartAddress;
                for (int i = 0; i < length; i++)
                {
                    reg.Source[offset + i] = ParseBigEndianUInt16(e.Request.Data, i * 2);
                }
                reg.Notify();
            }
            else
            {
                e.IsError = true;
                e.ErrorCode = ModbusErrorCode.IllegalDataAddress;
            }
        }
    }
}
