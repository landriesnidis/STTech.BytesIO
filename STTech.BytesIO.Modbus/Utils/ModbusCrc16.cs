using System;

namespace STTech.BytesIO.Modbus
{
    /// <summary>
    /// Modbus RTU CRC16 高性能查表法校验工具
    /// </summary>
    public static class ModbusCrc16
    {
        private static readonly ushort[] Table = new ushort[256];

        static ModbusCrc16()
        {
            ushort poly = 0xA001;
            for (ushort i = 0; i < 256; i++)
            {
                ushort cur = i;
                for (byte j = 0; j < 8; j++)
                {
                    if ((cur & 1) != 0)
                        cur = (ushort)((cur >> 1) ^ poly);
                    else
                        cur >>= 1;
                }
                Table[i] = cur;
            }
        }

        /// <summary>
        /// 基于 256 表计算数据的 CRC16 校验码 (零堆分配)
        /// </summary>
        /// <param name="data">待计算的数据切片</param>
        /// <returns>CRC16 校验码 (低字节在低位，高字节在高位)</returns>
        public static ushort Compute(ReadOnlySpan<byte> data)
        {
            ushort crc = 0xFFFF;
            for (int i = 0; i < data.Length; i++)
            {
                crc = (ushort)((crc >> 8) ^ Table[(crc ^ data[i]) & 0xFF]);
            }
            return crc;
        }

        /// <summary>
        /// 校验 RTU 报文末尾的 CRC16 校验码是否匹配
        /// </summary>
        /// <param name="frame">包含末尾 2 字节 CRC 的完整 RTU 报文</param>
        /// <returns>若校验匹配返回 true，否则返回 false</returns>
        public static bool Check(ReadOnlySpan<byte> frame)
        {
            if (frame.Length < 3) return false;
            int dataLen = frame.Length - 2;
            ushort expectedCrc = (ushort)(frame[dataLen] | (frame[dataLen + 1] << 8));
            ushort actualCrc = Compute(frame.Slice(0, dataLen));
            return expectedCrc == actualCrc;
        }
    }
}
