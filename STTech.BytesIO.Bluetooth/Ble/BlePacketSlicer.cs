using System;
using System.Collections.Generic;

namespace STTech.BytesIO.Bluetooth.Ble
{
    /// <summary>
    /// BLE 数据包分包切片工具 (用于处理超长数据分帧与流控)
    /// </summary>
    public static class BlePacketSlicer
    {
        /// <summary>
        /// 默认 BLE 最小有效负载大小 (ATT MTU 23 - 3 字节 ATT 头部 = 20 字节)
        /// </summary>
        public const int DefaultMinPayloadSize = 20;

        /// <summary>
        /// 将数据按最大有效负载大小进行切片分包
        /// </summary>
        /// <param name="data">待发送的原始字节数组</param>
        /// <param name="maxPayloadSize">单包最大有效载荷 (为 0 或负数时自动使用 20 字节保底)</param>
        /// <returns>分包切片集合</returns>
        public static IEnumerable<ArraySegment<byte>> Slice(byte[] data, int maxPayloadSize)
        {
            if (data == null || data.Length == 0)
            {
                yield break;
            }

            int chunkSize = maxPayloadSize > 0 ? maxPayloadSize : DefaultMinPayloadSize;
            int offset = 0;
            while (offset < data.Length)
            {
                int count = Math.Min(chunkSize, data.Length - offset);
                yield return new ArraySegment<byte>(data, offset, count);
                offset += count;
            }
        }
    }
}
