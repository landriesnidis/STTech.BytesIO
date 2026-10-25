using System;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace STTech.BytesIO.Bluetooth.Common
{
    /// <summary>
    /// 蓝牙 MAC 地址辅助工具类
    /// </summary>
    public static class BluetoothAddressHelper
    {
        /// <summary>
        /// 格式化数值类型的 MAC 地址为字符串 (例如 "00:1A:7D:DA:71:13")
        /// </summary>
        /// <param name="address">蓝牙地址数值 (48位)</param>
        /// <param name="separator">分隔符，默认为冒号 ":"</param>
        /// <returns>格式化后的 MAC 字符串</returns>
        public static string Format(ulong address, string separator = ":")
        {
            var hex = address.ToString("X12");
            var sb = new StringBuilder(17);
            for (int i = 0; i < 12; i += 2)
            {
                if (i > 0 && !string.IsNullOrEmpty(separator))
                {
                    sb.Append(separator);
                }
                sb.Append(hex.Substring(i, 2));
            }
            return sb.ToString();
        }

        /// <summary>
        /// 将 MAC 地址字符串解析为 ulong 数值
        /// </summary>
        /// <param name="addressString">MAC 字符串 (支持 "00:1A:7D:DA:71:13"、"00-1A-7D-DA-71-13" 或无分隔符形式)</param>
        /// <returns>解析后的蓝牙地址数值</returns>
        public static ulong Parse(string addressString)
        {
            if (TryParse(addressString, out var address))
            {
                return address;
            }
            throw new FormatException($"无效的蓝牙 MAC 地址格式: '{addressString}'。有效格式示例: '00:1A:7D:DA:71:13' 或 '00-1A-7D-DA-71-13'");
        }

        /// <summary>
        /// 尝试将 MAC 地址字符串解析为 ulong 数值
        /// </summary>
        /// <param name="addressString">MAC 字符串</param>
        /// <param name="address">解析成功的数值</param>
        /// <returns>是否解析成功</returns>
        public static bool TryParse(string? addressString, out ulong address)
        {
            address = 0;
            if (string.IsNullOrWhiteSpace(addressString))
            {
                return false;
            }

            var clean = Regex.Replace(addressString!.Trim(), "[:\\-\\s]", "");
            if (clean.Length == 0 || clean.Length > 12)
            {
                return false;
            }

            return ulong.TryParse(clean, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out address);
        }
    }
}
