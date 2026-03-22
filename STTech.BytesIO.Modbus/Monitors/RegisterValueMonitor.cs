using STTech.BytesIO.Core;
using STTech.CodePlus.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace STTech.BytesIO.Modbus.Monitors
{
    /// <summary>
    /// 寄存器数值监视器
    /// </summary>
    public abstract class RegisterValueMonitor<T> : ValuesMonitor<T>, IModbusRegister<T> where T : struct
    {
        /// <summary>
        /// 连续寄存器起始地址
        /// </summary>
        public ushort StartAddress { get; }

        /// <summary>
        /// 连续寄存器的总长度
        /// </summary>
        public ushort Length { get; }

        /// <summary>
        /// 构造寄存器数值监视器
        /// </summary>
        /// <param name="startAddress">起始地址</param>
        /// <param name="length">寄存器长度</param>
        public RegisterValueMonitor(ushort startAddress, ushort length) : base(new T[length])
        {
            StartAddress = startAddress;
            Length = length;
        }

        public abstract ModbusRegisterType RegisterType { get; }

        /// <summary>
        /// 判定寄存器区间是否被此连续块包含
        /// </summary>
        /// <param name="address">待检查的起始地址</param>
        /// <param name="length">待检查的长度</param>
        /// <returns>如果包含则返回 true</returns>
        public bool Contains(ushort address, ushort length)
        {
            return address >= StartAddress && (address + length) <= (StartAddress + Length);
        }
    }

    /// <summary>
    /// 数值类型为UInt16的寄存器数值监视器
    /// </summary>
    public abstract class UInt16RegisterValueMonitor : RegisterValueMonitor<ushort>
    {
        /// <summary>
        /// 构造数值类型为 UInt16 的寄存器数值监视器
        /// </summary>
        /// <param name="startAddress">起始地址</param>
        /// <param name="length">寄存器长度</param>
        public UInt16RegisterValueMonitor(ushort startAddress, ushort length) : base(startAddress, length)
        {
        }
    }

    /// <summary>
    /// 数值类型为布尔值的寄存器数值监视器
    /// </summary>
    public abstract class BooleanRegisterValueMonitor : RegisterValueMonitor<bool>
    {
        /// <summary>
        /// 构造数值类型为布尔值的寄存器数值监视器
        /// </summary>
        /// <param name="startAddress">起始地址</param>
        /// <param name="length">寄存器长度</param>
        public BooleanRegisterValueMonitor(ushort startAddress, ushort length) : base(startAddress, length)
        {
        }
    }

    /// <summary>
    /// 离散输入寄存器数值监视器
    /// </summary>
    public class DiscreteInputRegisterValueMonitor : BooleanRegisterValueMonitor
    {
        public override ModbusRegisterType RegisterType => ModbusRegisterType.DiscreteInput;

        /// <summary>
        /// 构造离散输入寄存器数值监视器
        /// </summary>
        /// <param name="startAddress">起始地址</param>
        /// <param name="length">寄存器长度</param>
        public DiscreteInputRegisterValueMonitor(ushort startAddress, ushort length) : base(startAddress, length)
        {
        }
    }

    /// <summary>
    /// 线圈寄存器数值监视器
    /// </summary>
    public class CoilRegisterValueMonitor : BooleanRegisterValueMonitor
    {
        public override ModbusRegisterType RegisterType => ModbusRegisterType.Coil;

        /// <summary>
        /// 构造线圈寄存器数值监视器
        /// </summary>
        /// <param name="startAddress">起始地址</param>
        /// <param name="length">寄存器长度</param>
        public CoilRegisterValueMonitor(ushort startAddress, ushort length) : base(startAddress, length)
        {
        }
    }

    /// <summary>
    /// 保持寄存器数值监视器
    /// </summary>
    public class HoldingRegisterValueMonitor : UInt16RegisterValueMonitor
    {
        public override ModbusRegisterType RegisterType => ModbusRegisterType.Holding;

        /// <summary>
        /// 构造保持寄存器数值监视器
        /// </summary>
        /// <param name="startAddress">起始地址</param>
        /// <param name="length">寄存器长度</param>
        public HoldingRegisterValueMonitor(ushort startAddress, ushort length) : base(startAddress, length)
        {
        }
    }

    /// <summary>
    /// 输入寄存器数值监视器
    /// </summary>
    public class InputRegisterValueMonitor : UInt16RegisterValueMonitor
    {
        public override ModbusRegisterType RegisterType => ModbusRegisterType.Input;

        /// <summary>
        /// 构造输入寄存器数值监视器
        /// </summary>
        /// <param name="startAddress">起始地址</param>
        /// <param name="length">寄存器长度</param>
        public InputRegisterValueMonitor(ushort startAddress, ushort length) : base(startAddress, length)
        {
        }
    }
}