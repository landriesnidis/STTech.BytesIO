using STTech.BytesIO.Bluetooth.Common;
using STTech.BytesIO.Bluetooth.Entity;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;

#if WINDOWS
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Devices.Enumeration;
#endif

namespace STTech.BytesIO.Bluetooth
{
    /// <summary>
    /// 蓝牙设备管理器 (负责设备发现、状态监控与客户端工厂构建)
    /// </summary>
    [SupportedOSPlatform("windows10.0.19041.0")]
    public class BluetoothManager : IBluetoothManager
    {
        private readonly object _lock = new object();
        private readonly ConcurrentDictionary<ulong, BluetoothDeviceInfo> _deviceCache = new ConcurrentDictionary<ulong, BluetoothDeviceInfo>();
        private CancellationTokenSource? _scanTimeoutCts;
        private volatile bool _isScanning;

#if WINDOWS
        private BluetoothLEAdvertisementWatcher? _bleWatcher;
        private DeviceWatcher? _classicWatcher;
#endif

        /// <inheritdoc/>
        public bool IsScanning => _isScanning;

        /// <inheritdoc/>
        public IReadOnlyList<BluetoothDeviceInfo> DiscoveredDevices => _deviceCache.Values.ToList().AsReadOnly();

        /// <inheritdoc/>
        public event EventHandler<BluetoothDeviceDiscoveredEventArgs>? OnDeviceDiscovered;

        /// <inheritdoc/>
        public event EventHandler<BluetoothDeviceDiscoveredEventArgs>? OnDeviceUpdated;

        /// <inheritdoc/>
        public event EventHandler<ScanStatusChangedEventArgs>? OnScanStatusChanged;

        /// <summary>
        /// 构造蓝牙设备管理器
        /// </summary>
        public BluetoothManager()
        {
        }

        /// <inheritdoc/>
        public BluetoothDeviceInfo? GetDevice(ulong address)
        {
            return _deviceCache.TryGetValue(address, out var info) ? info : null;
        }

        /// <inheritdoc/>
        public BluetoothDeviceInfo? GetDevice(string addressString)
        {
            if (BluetoothAddressHelper.TryParse(addressString, out var address))
            {
                return GetDevice(address);
            }
            return null;
        }

        /// <inheritdoc/>
        public void ClearDiscoveredDevices()
        {
            foreach (var device in _deviceCache.Values)
            {
                try
                {
                    (device.NativeDevice as IDisposable)?.Dispose();
                    device.NativeDevice = null;
                }
                catch { }
            }

            _deviceCache.Clear();
        }

        /// <inheritdoc/>
        public void StartScan(BluetoothScanOptions? options = null)
        {
            options ??= new BluetoothScanOptions();

            lock (_lock)
            {
                if (_isScanning)
                {
                    StopScan();
                }

#if WINDOWS
                _scanTimeoutCts?.Cancel();
                _scanTimeoutCts?.Dispose();
                _scanTimeoutCts = options.Timeout.HasValue ? new CancellationTokenSource(options.Timeout.Value) : null;

                if (options.Mode.HasFlag(BluetoothScanMode.Ble))
                {
                    _bleWatcher = new BluetoothLEAdvertisementWatcher
                    {
                        ScanningMode = BluetoothLEScanningMode.Active
                    };

                    if (options.RssiFilter.HasValue)
                    {
                        _bleWatcher.SignalStrengthFilter.InRangeThresholdInDBm = options.RssiFilter.Value;
                    }

                    // 生效服务 UUID 过滤参数
                    if (options.ServiceUuids != null && options.ServiceUuids.Count > 0)
                    {
                        foreach (var uuid in options.ServiceUuids)
                        {
                            _bleWatcher.AdvertisementFilter.Advertisement.ServiceUuids.Add(uuid);
                        }
                    }

                    _bleWatcher.Received += OnBleWatcherReceived;
                    _bleWatcher.Start();
                }

                if (options.Mode.HasFlag(BluetoothScanMode.Classic))
                {
                    string selector;
                    DeviceInformationKind deviceKind;
                    if (options.ClassicScanUnpairedDevices)
                    {
                        // 采用 AEP 协议选择器，驱动系统硬件主动执行空中 Inquiry 发现未配对及已配对经典蓝牙设备
                        selector = "System.Devices.Aep.ProtocolId:=\"{E0CBF06C-CD8B-4647-BB8A-263B43F0F974}\"";
                        deviceKind = DeviceInformationKind.AssociationEndpoint;
                    }
                    else
                    {
                        // 仅枚举系统中已绑定的已配对设备
                        selector = BluetoothDevice.GetDeviceSelector();
                        deviceKind = DeviceInformationKind.Device;
                    }

                    _classicWatcher = DeviceInformation.CreateWatcher(
                        selector,
                        new[] { "System.Devices.Aep.DeviceAddress", "System.Devices.Aep.IsPaired", "System.Devices.Aep.IsConnected" },
                        deviceKind);
                    _classicWatcher.Added += OnClassicDeviceAdded;
                    _classicWatcher.Updated += OnClassicDeviceUpdated;
                    _classicWatcher.Start();
                }

                _isScanning = true;
                RaiseScanStatusChanged(true);

                if (_scanTimeoutCts != null)
                {
                    _ = Task.Delay(options.Timeout!.Value, _scanTimeoutCts.Token).ContinueWith(t =>
                    {
                        if (!t.IsCanceled)
                        {
                            StopScan();
                        }
                    }, TaskScheduler.Default);
                }
#else
                throw new PlatformNotSupportedException("STTech.BytesIO.Bluetooth 当前仅支持 Windows 平台 (windows10.0.19041.0+)。");
#endif
            }
        }

        /// <inheritdoc/>
        public void StopScan()
        {
            lock (_lock)
            {
                if (!_isScanning)
                {
                    return;
                }

                _scanTimeoutCts?.Cancel();
                _scanTimeoutCts?.Dispose();
                _scanTimeoutCts = null;

#if WINDOWS
                if (_bleWatcher != null)
                {
                    try
                    {
                        _bleWatcher.Received -= OnBleWatcherReceived;
                        _bleWatcher.Stop();
                    }
                    catch { }
                    _bleWatcher = null;
                }

                if (_classicWatcher != null)
                {
                    try
                    {
                        _classicWatcher.Added -= OnClassicDeviceAdded;
                        _classicWatcher.Updated -= OnClassicDeviceUpdated;
                        _classicWatcher.Stop();
                    }
                    catch { }
                    _classicWatcher = null;
                }
#endif

                _isScanning = false;
                RaiseScanStatusChanged(false);
            }
        }

        /// <summary>
        /// 查询系统中所有已配对的经典蓝牙设备快照
        /// </summary>
        public async Task<IReadOnlyList<BluetoothDeviceInfo>> FindPairedClassicDevicesAsync()
        {
#if WINDOWS
            var selector = BluetoothDevice.GetDeviceSelectorFromPairingState(true);
            var devices = await DeviceInformation.FindAllAsync(selector).AsTask().ConfigureAwait(false);
            var result = new List<BluetoothDeviceInfo>();

            foreach (var dev in devices)
            {
                try
                {
                    var bth = await BluetoothDevice.FromIdAsync(dev.Id).AsTask().ConfigureAwait(false);
                    if (bth != null)
                    {
                        var info = new BluetoothDeviceInfo
                        {
                            Address = bth.BluetoothAddress,
                            AddressString = BluetoothAddressHelper.Format(bth.BluetoothAddress),
                            Name = bth.Name ?? dev.Name ?? string.Empty,
                            DeviceId = dev.Id,
                            Type = BluetoothType.Classic,
                            IsPaired = true,
                            IsConnected = bth.ConnectionStatus == BluetoothConnectionStatus.Connected,
                            NativeDevice = bth
                        };
                        result.Add(info);
                        _deviceCache.AddOrUpdate(info.Address, info, (_, existing) => info);
                    }
                }
                catch { }
            }

            return result.AsReadOnly();
#else
            await Task.CompletedTask;
            return Array.Empty<BluetoothDeviceInfo>();
#endif
        }

        /// <summary>
        /// 查询系统中所有已配对的 BLE 低功耗蓝牙设备快照
        /// </summary>
        public async Task<IReadOnlyList<BluetoothDeviceInfo>> FindPairedBleDevicesAsync()
        {
#if WINDOWS
            var selector = BluetoothLEDevice.GetDeviceSelectorFromPairingState(true);
            var devices = await DeviceInformation.FindAllAsync(selector).AsTask().ConfigureAwait(false);
            var result = new List<BluetoothDeviceInfo>();

            foreach (var dev in devices)
            {
                try
                {
                    var bth = await BluetoothLEDevice.FromIdAsync(dev.Id).AsTask().ConfigureAwait(false);
                    if (bth != null)
                    {
                        var info = new BluetoothDeviceInfo
                        {
                            Address = bth.BluetoothAddress,
                            AddressString = BluetoothAddressHelper.Format(bth.BluetoothAddress),
                            Name = bth.Name ?? dev.Name ?? string.Empty,
                            DeviceId = dev.Id,
                            Type = BluetoothType.Ble,
                            IsPaired = true,
                            IsConnected = bth.ConnectionStatus == BluetoothConnectionStatus.Connected,
                            NativeDevice = bth
                        };
                        result.Add(info);
                        _deviceCache.AddOrUpdate(info.Address, info, (_, existing) => info);
                    }
                }
                catch { }
            }

            return result.AsReadOnly();
#else
            await Task.CompletedTask;
            return Array.Empty<BluetoothDeviceInfo>();
#endif
        }

        /// <summary>
        /// 通过发现的设备信息创建经典蓝牙客户端
        /// </summary>
        public BluetoothClient CreateBluetoothClient(BluetoothDeviceInfo device, Guid? serviceUuid = null)
        {
            if (device == null) throw new ArgumentNullException(nameof(device));
            if (device.Address != 0)
            {
                _deviceCache.TryAdd(device.Address, device);
            }
            return new BluetoothClient(device, serviceUuid);
        }

        /// <summary>
        /// 通过 MAC 地址创建经典蓝牙客户端
        /// </summary>
        public BluetoothClient CreateBluetoothClient(string addressString, Guid? serviceUuid = null)
        {
            if (BluetoothAddressHelper.TryParse(addressString, out var addr) && _deviceCache.TryGetValue(addr, out var cached))
            {
                return new BluetoothClient(cached, serviceUuid);
            }
            return new BluetoothClient(addressString, serviceUuid);
        }

        /// <summary>
        /// 通过 MAC 地址数值创建经典蓝牙客户端
        /// </summary>
        public BluetoothClient CreateBluetoothClient(ulong address, Guid? serviceUuid = null)
        {
            if (_deviceCache.TryGetValue(address, out var cached))
            {
                return new BluetoothClient(cached, serviceUuid);
            }
            return new BluetoothClient(address, serviceUuid);
        }

        /// <summary>
        /// 通过发现的设备信息创建 BLE 低功耗蓝牙客户端
        /// </summary>
        public BleClient CreateBleClient(BluetoothDeviceInfo device, Guid? serviceUuid = null, Guid? writeUuid = null, Guid? notifyUuid = null)
        {
            if (device == null) throw new ArgumentNullException(nameof(device));
            if (device.Address != 0)
            {
                _deviceCache.TryAdd(device.Address, device);
            }
            return new BleClient(device, serviceUuid, writeUuid, notifyUuid);
        }

        /// <summary>
        /// 通过 MAC 地址创建 BLE 低功耗蓝牙客户端
        /// </summary>
        public BleClient CreateBleClient(string addressString, Guid? serviceUuid = null, Guid? writeUuid = null, Guid? notifyUuid = null)
        {
            if (BluetoothAddressHelper.TryParse(addressString, out var addr) && _deviceCache.TryGetValue(addr, out var cached))
            {
                return new BleClient(cached, serviceUuid, writeUuid, notifyUuid);
            }
            return new BleClient(addressString, serviceUuid, writeUuid, notifyUuid);
        }

        /// <summary>
        /// 通过 MAC 地址数值创建 BLE 低功耗蓝牙客户端
        /// </summary>
        public BleClient CreateBleClient(ulong address, Guid? serviceUuid = null, Guid? writeUuid = null, Guid? notifyUuid = null)
        {
            if (_deviceCache.TryGetValue(address, out var cached))
            {
                return new BleClient(cached, serviceUuid, writeUuid, notifyUuid);
            }
            return new BleClient(address, serviceUuid, writeUuid, notifyUuid);
        }

        // 显式实现 IBluetoothManager 接口方法，满足依赖倒置原则
        IBluetoothClient IBluetoothManager.CreateBluetoothClient(BluetoothDeviceInfo device, Guid? serviceUuid) => CreateBluetoothClient(device, serviceUuid);
        IBluetoothClient IBluetoothManager.CreateBluetoothClient(string addressString, Guid? serviceUuid) => CreateBluetoothClient(addressString, serviceUuid);
        IBluetoothClient IBluetoothManager.CreateBluetoothClient(ulong address, Guid? serviceUuid) => CreateBluetoothClient(address, serviceUuid);

        IBleClient IBluetoothManager.CreateBleClient(BluetoothDeviceInfo device, Guid? serviceUuid, Guid? writeUuid, Guid? notifyUuid) => CreateBleClient(device, serviceUuid, writeUuid, notifyUuid);
        IBleClient IBluetoothManager.CreateBleClient(string addressString, Guid? serviceUuid, Guid? writeUuid, Guid? notifyUuid) => CreateBleClient(addressString, serviceUuid, writeUuid, notifyUuid);
        IBleClient IBluetoothManager.CreateBleClient(ulong address, Guid? serviceUuid, Guid? writeUuid, Guid? notifyUuid) => CreateBleClient(address, serviceUuid, writeUuid, notifyUuid);

#if WINDOWS
        private void OnBleWatcherReceived(BluetoothLEAdvertisementWatcher sender, BluetoothLEAdvertisementReceivedEventArgs args)
        {
            var address = args.BluetoothAddress;
            var newEntry = new BluetoothDeviceInfo
            {
                Address = address,
                AddressString = BluetoothAddressHelper.Format(address),
                Type = BluetoothType.Ble
            };

            // 使用 TryAdd 保证原子性判定，消除并发竞态导致的重复 isNew 事件
            bool isNew = _deviceCache.TryAdd(address, newEntry);
            var info = isNew ? newEntry : _deviceCache[address];

            lock (info)
            {
                info.Rssi = args.RawSignalStrengthInDBm;
                info.LastSeen = DateTime.Now;

                // 优先更新设备名称（主广播包或扫描响应包带回的名称）
                if (!string.IsNullOrWhiteSpace(args.Advertisement.LocalName))
                {
                    info.Name = args.Advertisement.LocalName;
                }

                if (args.Advertisement.ServiceUuids != null)
                {
                    foreach (var uuid in args.Advertisement.ServiceUuids)
                    {
                        info.AddServiceUuid(uuid);
                    }
                }
            }

            try
            {
                if (isNew)
                {
                    RaiseDeviceDiscovered(info, true);
                }
                else
                {
                    RaiseDeviceUpdated(info);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[BluetoothManager] 派发 BLE 设备事件异常: {ex}");
            }
        }

        private async void OnClassicDeviceAdded(DeviceWatcher sender, DeviceInformation args)
        {
            try
            {
                BluetoothDevice? bth = null;
                try
                {
                    bth = await BluetoothDevice.FromIdAsync(args.Id).AsTask().ConfigureAwait(false);
                }
                catch { }

                ulong address = 0;
                if (bth != null)
                {
                    address = bth.BluetoothAddress;
                }
                else if (args.Properties.TryGetValue("System.Devices.Aep.DeviceAddress", out var addrObj) && addrObj is string addrStr)
                {
                    BluetoothAddressHelper.TryParse(addrStr, out address);
                }

                if (address == 0) return;

                var newEntry = new BluetoothDeviceInfo
                {
                    Address = address,
                    AddressString = BluetoothAddressHelper.Format(address),
                    Type = BluetoothType.Classic
                };

                bool isNew = _deviceCache.TryAdd(address, newEntry);
                var info = isNew ? newEntry : _deviceCache[address];

                lock (info)
                {
                    info.DeviceId = args.Id;
                    info.Name = bth?.Name ?? args.Name ?? string.Empty;
                    info.IsPaired = args.Pairing?.IsPaired ?? (bth?.DeviceInformation?.Pairing?.IsPaired ?? false);
                    info.IsConnected = bth?.ConnectionStatus == BluetoothConnectionStatus.Connected;
                    if (bth != null)
                    {
                        info.NativeDevice = bth;
                    }
                    info.LastSeen = DateTime.Now;
                }

                try
                {
                    if (isNew)
                    {
                        RaiseDeviceDiscovered(info, true);
                    }
                    else
                    {
                        RaiseDeviceUpdated(info);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[BluetoothManager] 派发经典蓝牙事件异常: {ex}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[BluetoothManager] 解析经典蓝牙设备异常: {ex}");
            }
        }

        private void OnClassicDeviceUpdated(DeviceWatcher sender, DeviceInformationUpdate args)
        {
            try
            {
                var match = _deviceCache.Values.FirstOrDefault(d => d.DeviceId == args.Id);
                if (match != null)
                {
                    match.LastSeen = DateTime.Now;
                    try
                    {
                        RaiseDeviceUpdated(match);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[BluetoothManager] 派发经典蓝牙更新事件异常: {ex}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[BluetoothManager] 经典蓝牙设备更新异常: {ex}");
            }
        }
#endif

        /// <summary>
        /// 触发发现新设备事件
        /// </summary>
        /// <param name="device">蓝牙设备信息</param>
        /// <param name="isNewDevice">是否为首次发现的新设备</param>
        protected virtual void RaiseDeviceDiscovered(BluetoothDeviceInfo device, bool isNewDevice)
        {
            OnDeviceDiscovered?.Invoke(this, new BluetoothDeviceDiscoveredEventArgs(device, isNewDevice));
        }

        /// <summary>
        /// 触发设备信息更新事件
        /// </summary>
        /// <param name="device">蓝牙设备信息</param>
        protected virtual void RaiseDeviceUpdated(BluetoothDeviceInfo device)
        {
            OnDeviceUpdated?.Invoke(this, new BluetoothDeviceDiscoveredEventArgs(device, false));
        }

        /// <summary>
        /// 触发扫描状态改变事件
        /// </summary>
        /// <param name="isScanning">当前是否扫描中</param>
        protected virtual void RaiseScanStatusChanged(bool isScanning)
        {
            OnScanStatusChanged?.Invoke(this, new ScanStatusChangedEventArgs(isScanning));
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            StopScan();

            foreach (var device in _deviceCache.Values)
            {
                try
                {
                    (device.NativeDevice as IDisposable)?.Dispose();
                    device.NativeDevice = null;
                }
                catch { }
            }

            _deviceCache.Clear();
        }
    }
}
