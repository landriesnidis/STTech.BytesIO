using STTech.BytesIO.Bluetooth.Entity;
using STTech.BytesIO.Core;
using System;
using System.Runtime.Versioning;

namespace STTech.BytesIO.Bluetooth
{
    /// <summary>
    /// 低功耗蓝牙通信客户端接口 (基于 BLE / GATT 服务与特征值通信)
    /// </summary>
    [SupportedOSPlatform("windows10.0.19041.0")]
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
        /// 写入选项 (自适应 / 带响应 / 无响应)
        /// </summary>
        BleWriteOption WriteOption { get; set; }

        /// <summary>
        /// 单次写入的最大有效负载大小（字节数，默认根据底层 MTU 协商动态确定，最小为 20 字节；设为大于 0 的数值可手动限定分包大小）
        /// </summary>
        int MaxPayloadSize { get; set; }

        /// <summary>
        /// 在使用 WriteWithoutResponse 发送长数据连续分包时的微延迟时间（毫秒，默认 2ms，防止从机接收缓冲区溢出丢包；0 表示不等待）
        /// </summary>
        int WriteWithoutResponseIntervalMs { get; set; }

        /// <summary>
        /// 关联的设备发现信息
        /// </summary>
        BluetoothDeviceInfo? DeviceInfo { get; set; }
    }
}
