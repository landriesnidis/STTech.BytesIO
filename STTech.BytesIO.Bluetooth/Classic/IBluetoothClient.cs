using STTech.BytesIO.Bluetooth.Entity;
using STTech.BytesIO.Core;
using System;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;

namespace STTech.BytesIO.Bluetooth
{
    /// <summary>
    /// 经典蓝牙通信客户端接口 (基于 RFCOMM / SPP 串行端口规范)
    /// </summary>
    [SupportedOSPlatform("windows10.0.19041.0")]
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

        /// <summary>
        /// 异步发起系统配对 (在工业触屏或 Kiosk 模式下免切桌面完成配对)
        /// </summary>
        /// <returns>配对是否成功</returns>
        Task<bool> PairAsync();

        /// <summary>
        /// 异步发起系统配对，支持传入指定 PIN 码 (如针对需固定 PIN 码的经典蓝牙设备如 HC-05/06) 与取消令牌
        /// </summary>
        /// <param name="pinCode">配对 PIN 码 (可选)</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>配对是否成功</returns>
        Task<bool> PairAsync(string? pinCode, CancellationToken cancellationToken = default);
    }
}
