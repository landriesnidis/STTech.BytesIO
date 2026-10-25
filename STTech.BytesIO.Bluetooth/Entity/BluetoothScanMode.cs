using System;

namespace STTech.BytesIO.Bluetooth.Entity
{
    /// <summary>
    /// 蓝牙扫描发现模式
    /// </summary>
    [Flags]
    public enum BluetoothScanMode
    {
        /// <summary>
        /// 仅扫描 BLE 设备
        /// </summary>
        Ble = 1,

        /// <summary>
        /// 仅扫描经典蓝牙设备
        /// </summary>
        Classic = 2,

        /// <summary>
        /// 同时扫描所有类型蓝牙设备 (BLE 与经典蓝牙)
        /// </summary>
        All = Ble | Classic
    }
}
