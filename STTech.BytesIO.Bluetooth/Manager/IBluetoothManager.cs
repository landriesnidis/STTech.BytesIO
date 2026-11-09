using STTech.BytesIO.Bluetooth.Entity;
using System;
using System.Collections.Generic;
using System.Runtime.Versioning;

namespace STTech.BytesIO.Bluetooth
{
    /// <summary>
    /// 蓝牙设备发现与管理中心接口
    /// </summary>
    [SupportedOSPlatform("windows10.0.19041.0")]
    public interface IBluetoothManager : IDisposable
    {
        /// <summary>
        /// 当前是否处于扫描监听状态
        /// </summary>
        bool IsScanning { get; }

        /// <summary>
        /// 发现新蓝牙设备事件 (首次发现某物理地址设备时触发)
        /// </summary>
        event EventHandler<BluetoothDeviceDiscoveredEventArgs>? OnDeviceDiscovered;

        /// <summary>
        /// 蓝牙设备信息更新事件 (已有设备的 RSSI、广播内容或连接状态变化时触发)
        /// </summary>
        event EventHandler<BluetoothDeviceDiscoveredEventArgs>? OnDeviceUpdated;

        /// <summary>
        /// 扫描状态改变事件
        /// </summary>
        event EventHandler<ScanStatusChangedEventArgs>? OnScanStatusChanged;

        /// <summary>
        /// 获取所有已发现的蓝牙设备列表快照
        /// </summary>
        IReadOnlyList<BluetoothDeviceInfo> DiscoveredDevices { get; }

        /// <summary>
        /// 根据 MAC 地址获取已发现的蓝牙设备
        /// </summary>
        /// <param name="address">MAC 地址数值</param>
        /// <returns>设备信息；若未发现则返回 null</returns>
        BluetoothDeviceInfo? GetDevice(ulong address);

        /// <summary>
        /// 根据 MAC 地址字符串获取已发现的蓝牙设备
        /// </summary>
        /// <param name="addressString">MAC 地址字符串 (如 "00:1A:7D:DA:71:13")</param>
        /// <returns>设备信息；若未发现则返回 null</returns>
        BluetoothDeviceInfo? GetDevice(string addressString);

        /// <summary>
        /// 开始扫描附近的蓝牙设备
        /// </summary>
        /// <param name="options">扫描配置参数 (若为 null 则采用默认配置)</param>
        void StartScan(BluetoothScanOptions? options = null);

        /// <summary>
        /// 停止蓝牙设备扫描
        /// </summary>
        void StopScan();

        /// <summary>
        /// 清空已发现设备的历史缓存
        /// </summary>
        void ClearDiscoveredDevices();

        /// <summary>
        /// 通过发现的设备信息创建经典蓝牙客户端
        /// </summary>
        /// <param name="device">蓝牙设备信息</param>
        /// <param name="serviceUuid">RFCOMM 服务 UUID (可选)</param>
        /// <returns>经典蓝牙客户端接口</returns>
        IBluetoothClient CreateBluetoothClient(BluetoothDeviceInfo device, Guid? serviceUuid = null);

        /// <summary>
        /// 通过 MAC 地址创建经典蓝牙客户端
        /// </summary>
        /// <param name="addressString">MAC 地址字符串 (如 "00:1A:7D:DA:71:13")</param>
        /// <param name="serviceUuid">RFCOMM 服务 UUID (可选)</param>
        /// <returns>经典蓝牙客户端接口</returns>
        IBluetoothClient CreateBluetoothClient(string addressString, Guid? serviceUuid = null);

        /// <summary>
        /// 通过 MAC 地址数值创建经典蓝牙客户端
        /// </summary>
        /// <param name="address">MAC 地址数值</param>
        /// <param name="serviceUuid">RFCOMM 服务 UUID (可选)</param>
        /// <returns>经典蓝牙客户端接口</returns>
        IBluetoothClient CreateBluetoothClient(ulong address, Guid? serviceUuid = null);

        /// <summary>
        /// 通过发现的设备信息创建 BLE 低功耗蓝牙客户端
        /// </summary>
        /// <param name="device">BLE 设备信息</param>
        /// <param name="serviceUuid">GATT 服务 UUID (可选)</param>
        /// <param name="writeUuid">写入特征值 UUID (可选)</param>
        /// <param name="notifyUuid">通知/指示特征值 UUID (可选)</param>
        /// <returns>BLE 客户端接口</returns>
        IBleClient CreateBleClient(BluetoothDeviceInfo device, Guid? serviceUuid = null, Guid? writeUuid = null, Guid? notifyUuid = null);

        /// <summary>
        /// 通过 MAC 地址创建 BLE 低功耗蓝牙客户端
        /// </summary>
        /// <param name="addressString">MAC 地址字符串 (如 "00:1A:7D:DA:71:13")</param>
        /// <param name="serviceUuid">GATT 服务 UUID (可选)</param>
        /// <param name="writeUuid">写入特征值 UUID (可选)</param>
        /// <param name="notifyUuid">通知/指示特征值 UUID (可选)</param>
        /// <returns>BLE 客户端接口</returns>
        IBleClient CreateBleClient(string addressString, Guid? serviceUuid = null, Guid? writeUuid = null, Guid? notifyUuid = null);

        /// <summary>
        /// 通过 MAC 地址数值创建 BLE 低功耗蓝牙客户端
        /// </summary>
        /// <param name="address">MAC 地址数值</param>
        /// <param name="serviceUuid">GATT 服务 UUID (可选)</param>
        /// <param name="writeUuid">写入特征值 UUID (可选)</param>
        /// <param name="notifyUuid">通知/指示特征值 UUID (可选)</param>
        /// <returns>BLE 客户端接口</returns>
        IBleClient CreateBleClient(ulong address, Guid? serviceUuid = null, Guid? writeUuid = null, Guid? notifyUuid = null);
    }
}
