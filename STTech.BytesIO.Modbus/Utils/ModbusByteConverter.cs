using System;
using System.Buffers.Binary;
using System.Text;

namespace STTech.BytesIO.Modbus
{
    /// <summary>
    /// Modbus 工业数据类型与端序转换工具
    /// </summary>
    public static class ModbusByteConverter
    {
        #region 32-bit (2 Registers, 4 Bytes)

        /// <summary>
        /// 调整 4 字节的端序到标准大端 (ABCD)
        /// </summary>
        private static void Arrange4BytesToAbcd(ReadOnlySpan<byte> src, Span<byte> dest, EndianFormat format)
        {
            switch (format)
            {
                case EndianFormat.ABCD:
                    dest[0] = src[0];
                    dest[1] = src[1];
                    dest[2] = src[2];
                    dest[3] = src[3];
                    break;
                case EndianFormat.CDAB:
                    dest[0] = src[2];
                    dest[1] = src[3];
                    dest[2] = src[0];
                    dest[3] = src[1];
                    break;
                case EndianFormat.BADC:
                    dest[0] = src[1];
                    dest[1] = src[0];
                    dest[2] = src[3];
                    dest[3] = src[2];
                    break;
                case EndianFormat.DCBA:
                    dest[0] = src[3];
                    dest[1] = src[2];
                    dest[2] = src[1];
                    dest[3] = src[0];
                    break;
            }
        }

        /// <summary>
        /// 从标准大端 (ABCD) 调整 4 字节到目标端序
        /// </summary>
        private static void Arrange4BytesFromAbcd(ReadOnlySpan<byte> abcd, Span<byte> dest, EndianFormat format)
        {
            switch (format)
            {
                case EndianFormat.ABCD:
                    dest[0] = abcd[0];
                    dest[1] = abcd[1];
                    dest[2] = abcd[2];
                    dest[3] = abcd[3];
                    break;
                case EndianFormat.CDAB:
                    dest[0] = abcd[2];
                    dest[1] = abcd[3];
                    dest[2] = abcd[0];
                    dest[3] = abcd[1];
                    break;
                case EndianFormat.BADC:
                    dest[0] = abcd[1];
                    dest[1] = abcd[0];
                    dest[2] = abcd[3];
                    dest[3] = abcd[2];
                    break;
                case EndianFormat.DCBA:
                    dest[0] = abcd[3];
                    dest[1] = abcd[2];
                    dest[2] = abcd[1];
                    dest[3] = abcd[0];
                    break;
            }
        }

        /// <summary>
        /// 从字节切片读取 32 位浮点数 (float / Single)
        /// </summary>
        public static float ToSingle(ReadOnlySpan<byte> bytes, EndianFormat format = EndianFormat.ABCD)
        {
            if (bytes.Length < 4) throw new ArgumentException("字节长度不足 4 字节", nameof(bytes));
            Span<byte> abcd = stackalloc byte[4];
            Arrange4BytesToAbcd(bytes, abcd, format);
            int intVal = BinaryPrimitives.ReadInt32BigEndian(abcd);
#if NETCOREAPP || NETSTANDARD2_1_OR_GREATER || NET5_0_OR_GREATER
            return BitConverter.Int32BitsToSingle(intVal);
#else
            byte[] temp = abcd.ToArray();
            if (!BitConverter.IsLittleEndian)
            {
                return BitConverter.ToSingle(temp, 0);
            }
            Array.Reverse(temp);
            return BitConverter.ToSingle(temp, 0);
#endif
        }

        /// <summary>
        /// 将 32 位浮点数 (float) 转换为指定端序的 4 字节
        /// </summary>
        public static byte[] GetBytes(float value, EndianFormat format = EndianFormat.ABCD)
        {
            Span<byte> abcd = stackalloc byte[4];
#if NETCOREAPP || NETSTANDARD2_1_OR_GREATER || NET5_0_OR_GREATER
            int intVal = BitConverter.SingleToInt32Bits(value);
            BinaryPrimitives.WriteInt32BigEndian(abcd, intVal);
#else
            byte[] raw = BitConverter.GetBytes(value);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(raw);
            }
            raw.CopyTo(abcd);
#endif
            byte[] result = new byte[4];
            Arrange4BytesFromAbcd(abcd, result, format);
            return result;
        }

        /// <summary>
        /// 从字节切片读取 32 位有符号整数 (int / Int32)
        /// </summary>
        public static int ToInt32(ReadOnlySpan<byte> bytes, EndianFormat format = EndianFormat.ABCD)
        {
            if (bytes.Length < 4) throw new ArgumentException("字节长度不足 4 字节", nameof(bytes));
            Span<byte> abcd = stackalloc byte[4];
            Arrange4BytesToAbcd(bytes, abcd, format);
            return BinaryPrimitives.ReadInt32BigEndian(abcd);
        }

        /// <summary>
        /// 将 32 位有符号整数 (int) 转换为指定端序的 4 字节
        /// </summary>
        public static byte[] GetBytes(int value, EndianFormat format = EndianFormat.ABCD)
        {
            Span<byte> abcd = stackalloc byte[4];
            BinaryPrimitives.WriteInt32BigEndian(abcd, value);
            byte[] result = new byte[4];
            Arrange4BytesFromAbcd(abcd, result, format);
            return result;
        }

        /// <summary>
        /// 从字节切片读取 32 位无符号整数 (uint / UInt32)
        /// </summary>
        public static uint ToUInt32(ReadOnlySpan<byte> bytes, EndianFormat format = EndianFormat.ABCD)
        {
            if (bytes.Length < 4) throw new ArgumentException("字节长度不足 4 字节", nameof(bytes));
            Span<byte> abcd = stackalloc byte[4];
            Arrange4BytesToAbcd(bytes, abcd, format);
            return BinaryPrimitives.ReadUInt32BigEndian(abcd);
        }

        /// <summary>
        /// 将 32 位无符号整数 (uint) 转换为指定端序的 4 字节
        /// </summary>
        public static byte[] GetBytes(uint value, EndianFormat format = EndianFormat.ABCD)
        {
            Span<byte> abcd = stackalloc byte[4];
            BinaryPrimitives.WriteUInt32BigEndian(abcd, value);
            byte[] result = new byte[4];
            Arrange4BytesFromAbcd(abcd, result, format);
            return result;
        }

        #endregion

        #region 16-bit (1 Register, 2 Bytes)

        /// <summary>
        /// 从字节切片读取 16 位有符号整数 (short / Int16)
        /// </summary>
        public static short ToInt16(ReadOnlySpan<byte> bytes, bool isLittleEndian = false)
        {
            if (bytes.Length < 2) throw new ArgumentException("字节长度不足 2 字节", nameof(bytes));
            return isLittleEndian ? BinaryPrimitives.ReadInt16LittleEndian(bytes) : BinaryPrimitives.ReadInt16BigEndian(bytes);
        }

        /// <summary>
        /// 将 16 位有符号整数 (short) 转换为 2 字节
        /// </summary>
        public static byte[] GetBytes(short value, bool isLittleEndian = false)
        {
            byte[] result = new byte[2];
            if (isLittleEndian)
                BinaryPrimitives.WriteInt16LittleEndian(result, value);
            else
                BinaryPrimitives.WriteInt16BigEndian(result, value);
            return result;
        }

        /// <summary>
        /// 从字节切片读取 16 位无符号整数 (ushort / UInt16)
        /// </summary>
        public static ushort ToUInt16(ReadOnlySpan<byte> bytes, bool isLittleEndian = false)
        {
            if (bytes.Length < 2) throw new ArgumentException("字节长度不足 2 字节", nameof(bytes));
            return isLittleEndian ? BinaryPrimitives.ReadUInt16LittleEndian(bytes) : BinaryPrimitives.ReadUInt16BigEndian(bytes);
        }

        /// <summary>
        /// 将 16 位无符号整数 (ushort) 转换为 2 字节
        /// </summary>
        public static byte[] GetBytes(ushort value, bool isLittleEndian = false)
        {
            byte[] result = new byte[2];
            if (isLittleEndian)
                BinaryPrimitives.WriteUInt16LittleEndian(result, value);
            else
                BinaryPrimitives.WriteUInt16BigEndian(result, value);
            return result;
        }

        #endregion

        #region 64-bit (4 Registers, 8 Bytes)

        /// <summary>
        /// 从字节切片读取 64 位双精度浮点数 (double)
        /// </summary>
        public static double ToDouble(ReadOnlySpan<byte> bytes, EndianFormat format = EndianFormat.ABCD)
        {
            if (bytes.Length < 8) throw new ArgumentException("字节长度不足 8 字节", nameof(bytes));
            long longVal = ToInt64(bytes, format);
#if NETCOREAPP || NETSTANDARD2_1_OR_GREATER || NET5_0_OR_GREATER
            return BitConverter.Int64BitsToDouble(longVal);
#else
            byte[] temp = new byte[8];
            BinaryPrimitives.WriteInt64BigEndian(temp, longVal);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(temp);
            }
            return BitConverter.ToDouble(temp, 0);
#endif
        }

        /// <summary>
        /// 将 64 位双精度浮点数 (double) 转换为 8 字节
        /// </summary>
        public static byte[] GetBytes(double value, EndianFormat format = EndianFormat.ABCD)
        {
#if NETCOREAPP || NETSTANDARD2_1_OR_GREATER || NET5_0_OR_GREATER
            long longVal = BitConverter.DoubleToInt64Bits(value);
#else
            byte[] raw = BitConverter.GetBytes(value);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(raw);
            }
            long longVal = BinaryPrimitives.ReadInt64BigEndian(raw);
#endif
            return GetBytes(longVal, format);
        }

        /// <summary>
        /// 从字节切片读取 64 位有符号整数 (long / Int64)
        /// </summary>
        public static long ToInt64(ReadOnlySpan<byte> bytes, EndianFormat format = EndianFormat.ABCD)
        {
            if (bytes.Length < 8) throw new ArgumentException("字节长度不足 8 字节", nameof(bytes));
            Span<byte> dest = stackalloc byte[8];
            // 8 字节端序：以 16 位 Word 为基本单元重排
            switch (format)
            {
                case EndianFormat.ABCD: // [0,1, 2,3, 4,5, 6,7]
                    bytes.Slice(0, 8).CopyTo(dest);
                    break;
                case EndianFormat.CDAB: // Word Swap: [6,7, 4,5, 2,3, 0,1]
                    dest[0] = bytes[6]; dest[1] = bytes[7];
                    dest[2] = bytes[4]; dest[3] = bytes[5];
                    dest[4] = bytes[2]; dest[5] = bytes[3];
                    dest[6] = bytes[0]; dest[7] = bytes[1];
                    break;
                case EndianFormat.BADC: // Byte Swap inside word
                    for (int i = 0; i < 8; i += 2)
                    {
                        dest[i] = bytes[i + 1];
                        dest[i + 1] = bytes[i];
                    }
                    break;
                case EndianFormat.DCBA: // Pure Little-Endian
                    for (int i = 0; i < 8; i++)
                    {
                        dest[i] = bytes[7 - i];
                    }
                    break;
            }
            return BinaryPrimitives.ReadInt64BigEndian(dest);
        }

        /// <summary>
        /// 将 64 位有符号整数 (long) 转换为 8 字节
        /// </summary>
        public static byte[] GetBytes(long value, EndianFormat format = EndianFormat.ABCD)
        {
            Span<byte> raw = stackalloc byte[8];
            BinaryPrimitives.WriteInt64BigEndian(raw, value);

            byte[] result = new byte[8];
            switch (format)
            {
                case EndianFormat.ABCD:
                    raw.CopyTo(result);
                    break;
                case EndianFormat.CDAB:
                    result[0] = raw[6]; result[1] = raw[7];
                    result[2] = raw[4]; result[3] = raw[5];
                    result[4] = raw[2]; result[5] = raw[3];
                    result[6] = raw[0]; result[7] = raw[1];
                    break;
                case EndianFormat.BADC:
                    for (int i = 0; i < 8; i += 2)
                    {
                        result[i] = raw[i + 1];
                        result[i + 1] = raw[i];
                    }
                    break;
                case EndianFormat.DCBA:
                    for (int i = 0; i < 8; i++)
                    {
                        result[i] = raw[7 - i];
                    }
                    break;
            }
            return result;
        }

        /// <summary>
        /// 从字节中解析 64 位无符号整数 (ulong)
        /// </summary>
        public static ulong ToUInt64(ReadOnlySpan<byte> bytes, EndianFormat format = EndianFormat.ABCD)
        {
            return unchecked((ulong)ToInt64(bytes, format));
        }

        /// <summary>
        /// 将 64 位无符号整数 (ulong) 转换为 8 字节
        /// </summary>
        public static byte[] GetBytes(ulong value, EndianFormat format = EndianFormat.ABCD)
        {
            return GetBytes(unchecked((long)value), format);
        }

        #endregion

        #region String (Registers to String)

        /// <summary>
        /// 将字节转换为 ASCII / UTF-8 字符串
        /// </summary>
        public static string ToString(ReadOnlySpan<byte> bytes, Encoding encoding = null)
        {
            encoding = encoding ?? Encoding.ASCII;
            // 找到首个 \0 终止符 (如果存在)
            int length = bytes.Length;
            for (int i = 0; i < bytes.Length; i++)
            {
                if (bytes[i] == 0)
                {
                    length = i;
                    break;
                }
            }
            if (length == 0) return string.Empty;
#if NETCOREAPP || NETSTANDARD2_1_OR_GREATER || NET5_0_OR_GREATER
            return encoding.GetString(bytes.Slice(0, length));
#else
            return encoding.GetString(bytes.Slice(0, length).ToArray());
#endif
        }

        #endregion
    }
}
