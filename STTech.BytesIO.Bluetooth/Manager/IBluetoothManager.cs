using STTech.BytesIO.Bluetooth.Entity;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace STTech.BytesIO.Bluetooth
{
    /// <summary>
    /// 蓝牙设备管理器接口 (负责设备发现、状态监控与客户端工厂构建)
    /// </summary>
    public interface IBluetoothManager : IDisposable
    {
        /// <summary>
        /// 当前是否处于扫描发现状态
        /// </summary>
        bool IsScanning { get; }

        /// <summary>
        /// 当前已发现的全部蓝牙设备集合 (缓存)
        /// </summary>
        IReadOnlyCollection<BluetoothDeviceInfo> DiscoveredDevices { get; }

        /// <summary>
        /// 发现新设备事件 (本次扫描过程中首次检测到的设备)
        /// </summary>
        event EventHandler<BluetoothDeviceDiscoveredEventArgs> OnDeviceDiscovered;

        /// <summary>
        /// 设备信息更新事件 (已发现设备的信号强度 RSSI、名称或服务更新)
        /// </summary>
        event EventHandler<BluetoothDeviceDiscoveredEventArgs> OnDeviceUpdated;

        /// <summary>
        /// 扫描状态改变事件
        /// </summary>
        event EventHandler<ScanStatusChangedEventArgs> OnScanStatusChanged;

        /// <summary>
        /// 开始扫描附近的蓝牙设备
        /// </summary>
        /// <param name="options">扫描参数配置 (为 null 时使用默认配置扫描所有类型)</param>
        void StartScan(BluetoothScanOptions? options = null);

        /// <summary>
        /// 停止当前的蓝牙设备扫描
        /// </summary>
        void StopScan();

        /// <summary>
        /// 查询当前操作系统中已配对的经典蓝牙设备列表
        /// </summary>
        /// <returns>已配对经典蓝牙设备列表</returns>
        Task<IReadOnlyList<BluetoothDeviceInfo>> FindPairedClassicDevicesAsync();

        /// <summary>
        /// 查询当前操作系统中已配对的 BLE 低功耗蓝牙设备列表
        /// </summary>
        /// <returns>已配对 BLE 设备列表</returns>
        Task<IReadOnlyList<BluetoothDeviceInfo>> FindPairedBleDevicesAsync();

        /// <summary>
        /// 通过发现的设备信息创建经典蓝牙客户端
        /// </summary>
        /// <param name="device">蓝牙设备信息</param>
        /// <param name="serviceUuid">RFCOMM 服务 UUID (可选)</param>
        /// <returns>经典蓝牙客户端</returns>
        BluetoothClient CreateBluetoothClient(BluetoothDeviceInfo device, Guid? serviceUuid = null);

        /// <summary>
        /// 通过 MAC 地址创建经典蓝牙客户端
        /// </summary>
        /// <param name="addressString">MAC 地址字符串 (如 "00:1A:7D:DA:71:13")</param>
        /// <param name="serviceUuid">RFCOMM 服务 UUID (可选)</param>
        /// <returns>经典蓝牙客户端</returns>
        BluetoothClient CreateBluetoothClient(string addressString, Guid? serviceUuid = null);

        /// <summary>
        /// 通过 MAC 地址数值创建经典蓝牙客户端
        /// </summary>
        /// <param name="address">MAC 地址数值</param>
        /// <param name="serviceUuid">RFCOMM 服务 UUID (可选)</param>
        /// <returns>经典蓝牙客户端</returns>
        BluetoothClient CreateBluetoothClient(ulong address, Guid? serviceUuid = null);

        /// <summary>
        /// 通过发现的设备信息创建 BLE 低功耗蓝牙客户端
        /// </summary>
        /// <param name="device">BLE 设备信息</param>
        /// <param name="serviceUuid">GATT 服务 UUID (可选)</param>
        /// <param name="writeUuid">写入特征值 UUID (可选)</param>
        /// <param name="notifyUuid">通知/指示特征值 UUID (可选)</param>
        /// <returns>BLE 客户端</returns>
        BleClient CreateBleClient(BluetoothDeviceInfo device, Guid? serviceUuid = null, Guid? writeUuid = null, Guid? notifyUuid = null);

        /// <summary>
        /// 通过 MAC 地址创建 BLE 低功耗蓝牙客户端
        /// </summary>
        /// <param name="addressString">MAC 地址字符串 (如 "00:1A:7D:DA:71:13")</param>
        /// <param name="serviceUuid">GATT 服务 UUID (可选)</param>
        /// <param name="writeUuid">写入特征值 UUID (可选)</param>
        /// <param name="notifyUuid">通知/指示特征值 UUID (可选)</param>
        /// <returns>BLE 客户端</returns>
        BleClient CreateBleClient(string addressString, Guid? serviceUuid = null, Guid? writeUuid = null, Guid? notifyUuid = null);

        /// <summary>
        /// 通过 MAC 地址数值创建 BLE 低功耗蓝牙客户端
        /// </summary>
        /// <param name="address">MAC 地址数值</param>
        /// <param name="serviceUuid">GATT 服务 UUID (可选)</param>
        /// <param name="writeUuid">写入特征值 UUID (可选)</param>
        /// <param name="notifyUuid">通知/指示特征值 UUID (可选)</param>
        /// <returns>BLE 客户端</returns>
        BleClient CreateBleClient(ulong address, Guid? serviceUuid = null, Guid? writeUuid = null, Guid? notifyUuid = null);
    }
}
