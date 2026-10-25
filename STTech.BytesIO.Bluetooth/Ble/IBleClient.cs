using STTech.BytesIO.Bluetooth.Entity;
using STTech.BytesIO.Core;
using System;

namespace STTech.BytesIO.Bluetooth
{
    /// <summary>
    /// 低功耗蓝牙通信客户端接口 (基于 BLE / GATT 服务与特征值通信)
    /// </summary>
    public interface IBleClient : IBytesClient
    {
        /// <summary>
        /// 目标 BLE 设备地址数值 (MAC)
        /// </summary>
        ulong DeviceAddress { get; set; }

        /// <summary>
        /// 目标 BLE 设备地址字符串 (例如 "00:1A:7D:DA:71:13")
        /// </summary>
        string DeviceAddressString { get; set; }

        /// <summary>
        /// 目标 BLE 设备名称
        /// </summary>
        string DeviceName { get; set; }

        /// <summary>
        /// 通信使用的 GATT 服务 UUID (为 null 时自动探测可用传输服务)
        /// </summary>
        Guid? ServiceUuid { get; set; }

        /// <summary>
        /// 数据写入特征值 UUID (为 null 时自动探测具备可写属性的特征值)
        /// </summary>
        Guid? WriteCharacteristicUuid { get; set; }

        /// <summary>
        /// 数据通知/指示特征值 UUID (为 null 时自动探测具备 Notify/Indicate 属性的特征值)
        /// </summary>
        Guid? NotifyCharacteristicUuid { get; set; }

        /// <summary>
        /// 写入选项 (带响应 / 无响应)
        /// </summary>
        BleWriteOption WriteOption { get; set; }

        /// <summary>
        /// 关联的设备发现信息
        /// </summary>
        BluetoothDeviceInfo? DeviceInfo { get; set; }
    }
}
