using System.ComponentModel;

namespace STTech.BytesIO.Modbus
{
    /// <summary>
    /// 工业端序 / 字节序格式
    /// </summary>
    public enum EndianFormat
    {
        /// <summary>
        /// 大端模式 (Big-Endian)，西门子 S7 PLC 等设备默认格式。
        /// 例如 32 位数据 ABCD: 高字在前，高字节在前。
        /// </summary>
        [Description("大端模式 ABCD")]
        ABCD,

        /// <summary>
        /// 大端字交换模式 (Big-Endian Word Swap)，施耐德 / Modicon 等设备默认格式。
        /// 例如 32 位数据 CDAB: 低字在前，但字内高字节在前。
        /// </summary>
        [Description("大端字交换 CDAB")]
        CDAB,

        /// <summary>
        /// 字节交换模式 (Byte Swap)。
        /// 例如 32 位数据 BADC: 高字在前，但字内低字节在前。
        /// </summary>
        [Description("字节交换 BADC")]
        BADC,

        /// <summary>
        /// 小端模式 (Little-Endian)，标准 x86 内存排列。
        /// 例如 32 位数据 DCBA: 低字节在前，低字在前。
        /// </summary>
        [Description("小端模式 DCBA")]
        DCBA,
    }
}
