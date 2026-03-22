using System;

namespace STTech.BytesIO.Modbus.Monitors
{
    /// <summary>
    /// Modbus 寄存器类型
    /// </summary>
    public enum ModbusRegisterType
    {
        /// <summary>
        /// 线圈寄存器 (可读可写)
        /// </summary>
        Coil,

        /// <summary>
        /// 离散输入寄存器 (只读)
        /// </summary>
        DiscreteInput,

        /// <summary>
        /// 输入寄存器 (只读)
        /// </summary>
        Input,

        /// <summary>
        /// 保持寄存器 (可读可写)
        /// </summary>
        Holding
    }

    /// <summary>
    /// Modbus 寄存器接口
    /// </summary>
    public interface IModbusRegister
    {
        /// <summary>
        /// 寄存器类型
        /// </summary>
        ModbusRegisterType RegisterType { get; }

        /// <summary>
        /// 连续寄存器起始地址
        /// </summary>
        ushort StartAddress { get; }

        /// <summary>
        /// 连续寄存器的总长度
        /// </summary>
        ushort Length { get; }

        /// <summary>
        /// 被分配至指定的起始地址的范围计算（判定寄存器区间是否被此连续快包含）
        /// </summary>
        bool Contains(ushort address, ushort length);
    }

    /// <summary>
    /// 强类型的 Modbus 寄存器接口
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IModbusRegister<T> : IModbusRegister where T : struct
    {
        /// <summary>
        /// 数据源
        /// </summary>
        T[] Source { get; }

        /// <summary>
        /// 触发变更事件
        /// </summary>
        void Notify();
    }
}
