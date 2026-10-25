using System;
using System.Collections.Generic;

namespace STTech.BytesIO.Bluetooth.Entity
{
    /// <summary>
    /// 蓝牙扫描选项参数
    /// </summary>
    public class BluetoothScanOptions
    {
        /// <summary>
        /// 扫描模式 (默认为扫描所有类型)
        /// </summary>
        public BluetoothScanMode Mode { get; set; } = BluetoothScanMode.All;

        /// <summary>
        /// 扫描超时时间 (为 null 表示持续扫描直至显式调用 StopScan)
        /// </summary>
        public TimeSpan? Timeout { get; set; }

        /// <summary>
        /// RSSI 信号阈值过滤 (低于该阈值的设备将被过滤忽略，单位: dBm)
        /// </summary>
        public short? RssiFilter { get; set; }

        /// <summary>
        /// 服务 UUID 过滤器 (仅上报包含指定服务的设备)
        /// </summary>
        public IList<Guid> ServiceUuids { get; set; } = new List<Guid>();
    }
}
