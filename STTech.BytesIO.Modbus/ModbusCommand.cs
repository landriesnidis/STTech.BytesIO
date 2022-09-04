using System;
using System.ComponentModel;

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
        [Description("读线圈寄存器")]
        ReadCoilRegister = 0x01,

        /// <summary>
        /// 读离散输入寄存器
        /// </summary>
        [Description("读离散输入寄存器")] 
        ReadDiscreteInputRegister = 0x02,


        /// <summary>
        /// 读保持寄存器
        /// </summary>
        [Description("读保持寄存器")] 
        ReadHoldRegister = 0x03,

        /// <summary>
        ///  读输入寄存器
        /// </summary>
        [Description("读输入寄存器")] 
        ReadInputRegister = 0x04,

        /// <summary>
        /// 写单个线圈寄存器
        /// </summary>
        [Description("写单个线圈寄存器")] 
        WriteSingleCoilRegister = 0x05,

        /// <summary>
        /// 写单个保持寄存器
        /// </summary>
        [Description("写单个保持寄存器")] 
        WriteSingleHoldRegister = 0x06,

        /// <summary>
        /// 写多个线圈寄存器
        /// </summary>
        [Description("写多个线圈寄存器")] 
        WriteMultipleCoilRegisters = 0x0F,

        /// <summary>
        /// 写多个保持寄存器
        /// </summary>
        [Description("写多个保持寄存器")] 
        WriteMultipleHoldRegisters = 0x10,
    }


}
