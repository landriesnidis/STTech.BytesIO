using System;

namespace STTech.BytesIO.Modbus
{
    /// <summary>
    /// Modbus命令
    /// </summary>
    public enum FunctionCode : byte
    {
        /// <summary>
        /// 读线圈寄存器
        /// </summary>
        ReadCoilRegister = 0x01,

        /// <summary>
        /// 读离散输入寄存器
        /// </summary>
        ReadDiscreteInputRegister = 0x02,


        /// <summary>
        /// 读保持寄存器
        /// </summary>
        ReadHoldRegister = 0x03,

        /// <summary>
        ///  读输入寄存器
        /// </summary>
        ReadInputRegister = 0x04,

        /// <summary>
        /// 写单个线圈寄存器
        /// </summary>
        WriteSingleCoilRegister = 0x05,

        /// <summary>
        /// 写单个保持寄存器
        /// </summary>
        WriteSingleHoldRegister = 0x06,

        /// <summary>
        /// 写多个线圈寄存器
        /// </summary>
        WriteMultipleCoilRegisters = 0x0F,

        /// <summary>
        /// 写多个保持寄存器
        /// </summary>
        WriteMultipleHoldRegisters = 0x10,
    }


}
