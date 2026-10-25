using System;

namespace STTech.BytesIO.Bluetooth.Entity
{
    /// <summary>
    /// 发现蓝牙设备事件参数
    /// </summary>
    public class BluetoothDeviceDiscoveredEventArgs : EventArgs
    {
        /// <summary>
        /// 发现或更新的蓝牙设备信息
        /// </summary>
        public BluetoothDeviceInfo Device { get; }

        /// <summary>
        /// 是否为本次扫描过程中首次发现的新设备 (false 表示已存在设备的信号强度或名称更新)
        /// </summary>
        public bool IsNewDevice { get; }

        /// <summary>
        /// 构造发现蓝牙设备事件参数
        /// </summary>
        /// <param name="device">蓝牙设备信息</param>
        /// <param name="isNewDevice">是否为新发现设备</param>
        public BluetoothDeviceDiscoveredEventArgs(BluetoothDeviceInfo device, bool isNewDevice)
        {
            Device = device;
            IsNewDevice = isNewDevice;
        }
    }
}
