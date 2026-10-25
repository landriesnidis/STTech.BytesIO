using System;
using System.Collections.Generic;

namespace STTech.BytesIO.Bluetooth.Entity
{
    /// <summary>
    /// 蓝牙设备信息
    /// </summary>
    public class BluetoothDeviceInfo
    {
        /// <summary>
        /// 蓝牙设备地址数值 (48位 MAC 地址)
        /// </summary>
        public ulong Address { get; set; }

        /// <summary>
        /// 格式化后的 MAC 地址字符串 (例如: "00:1A:7D:DA:71:13")
        /// </summary>
        public string AddressString { get; set; } = string.Empty;

        /// <summary>
        /// 蓝牙设备友好名称
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 系统设备标识 ID
        /// </summary>
        public string DeviceId { get; set; } = string.Empty;

        /// <summary>
        /// 蓝牙类型 (经典蓝牙 / BLE)
        /// </summary>
        public BluetoothType Type { get; set; }

        /// <summary>
        /// 是否已在操作系统中配对
        /// </summary>
        public bool IsPaired { get; set; }

        /// <summary>
        /// 是否处于物理连接状态
        /// </summary>
        public bool IsConnected { get; set; }

        /// <summary>
        /// 信号强度指示 (RSSI, 单位 dBm, 主要适用于 BLE)
        /// </summary>
        public short Rssi { get; set; }

        /// <summary>
        /// 发现或广播的服务 UUID 列表
        /// </summary>
        public List<Guid> ServiceUuids { get; set; } = new List<Guid>();

        /// <summary>
        /// 最近一次发现或广播的时间
        /// </summary>
        public DateTime LastSeen { get; set; } = DateTime.Now;

        /// <summary>
        /// 平台原生设备对象引用 (内部缓存，不对外作为公共序列化契约)
        /// </summary>
        internal object? NativeDevice { get; set; }

        /// <inheritdoc/>
        public override string ToString()
        {
            var typeTag = Type == BluetoothType.Ble ? "BLE" : "Classic";
            var name = string.IsNullOrWhiteSpace(Name) ? "Unknown" : Name;
            var rssiStr = Rssi != 0 ? $" ({Rssi} dBm)" : string.Empty;
            return $"[{typeTag}] {name} - {AddressString}{rssiStr}";
        }
    }
}
