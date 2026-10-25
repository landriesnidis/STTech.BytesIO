using STTech.BytesIO.Bluetooth.Entity;
using STTech.BytesIO.Core;
using System;

namespace STTech.BytesIO.Bluetooth
{
    /// <summary>
    /// 经典蓝牙通信客户端接口 (基于 RFCOMM / SPP 串行端口规范)
    /// </summary>
    public interface IBluetoothClient : IBytesClient
    {
        /// <summary>
        /// 目标蓝牙设备地址数值 (MAC)
        /// </summary>
        ulong DeviceAddress { get; set; }

        /// <summary>
        /// 目标蓝牙设备地址字符串 (例如 "00:1A:7D:DA:71:13")
        /// </summary>
        string DeviceAddressString { get; set; }

        /// <summary>
        /// 目标蓝牙设备名称
        /// </summary>
        string DeviceName { get; set; }

        /// <summary>
        /// RFCOMM 服务 UUID (默认为 SPP 串口服务 UUID: 00001101-0000-1000-8000-00805F9B34FB)
        /// </summary>
        Guid ServiceUuid { get; set; }

        /// <summary>
        /// 关联的设备发现信息
        /// </summary>
        BluetoothDeviceInfo? DeviceInfo { get; set; }
    }
}
