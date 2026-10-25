using STTech.BytesIO.Bluetooth.Common;
using STTech.BytesIO.Bluetooth.Entity;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
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
        public IReadOnlyCollection<BluetoothDeviceInfo> DiscoveredDevices => _deviceCache.Values.ToList().AsReadOnly();

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

                    _bleWatcher.Received += OnBleWatcherReceived;
                    _bleWatcher.Start();
                }

                if (options.Mode.HasFlag(BluetoothScanMode.Classic))
                {
                    var selector = BluetoothDevice.GetDeviceSelector();
                    _classicWatcher = DeviceInformation.CreateWatcher(selector, null, DeviceInformationKind.Device);
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

        /// <inheritdoc/>
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

        /// <inheritdoc/>
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

        /// <inheritdoc/>
        public BluetoothClient CreateBluetoothClient(BluetoothDeviceInfo device, Guid? serviceUuid = null)
        {
            if (device == null) throw new ArgumentNullException(nameof(device));
            return new BluetoothClient(device, serviceUuid);
        }

        /// <inheritdoc/>
        public BluetoothClient CreateBluetoothClient(string addressString, Guid? serviceUuid = null)
        {
            if (BluetoothAddressHelper.TryParse(addressString, out var addr) && _deviceCache.TryGetValue(addr, out var cached))
            {
                return new BluetoothClient(cached, serviceUuid);
            }
            return new BluetoothClient(addressString, serviceUuid);
        }

        /// <inheritdoc/>
        public BluetoothClient CreateBluetoothClient(ulong address, Guid? serviceUuid = null)
        {
            if (_deviceCache.TryGetValue(address, out var cached))
            {
                return new BluetoothClient(cached, serviceUuid);
            }
            return new BluetoothClient(address, serviceUuid);
        }

        /// <inheritdoc/>
        public BleClient CreateBleClient(BluetoothDeviceInfo device, Guid? serviceUuid = null, Guid? writeUuid = null, Guid? notifyUuid = null)
        {
            if (device == null) throw new ArgumentNullException(nameof(device));
            return new BleClient(device, serviceUuid, writeUuid, notifyUuid);
        }

        /// <inheritdoc/>
        public BleClient CreateBleClient(string addressString, Guid? serviceUuid = null, Guid? writeUuid = null, Guid? notifyUuid = null)
        {
            if (BluetoothAddressHelper.TryParse(addressString, out var addr) && _deviceCache.TryGetValue(addr, out var cached))
            {
                return new BleClient(cached, serviceUuid, writeUuid, notifyUuid);
            }
            return new BleClient(addressString, serviceUuid, writeUuid, notifyUuid);
        }

        /// <inheritdoc/>
        public BleClient CreateBleClient(ulong address, Guid? serviceUuid = null, Guid? writeUuid = null, Guid? notifyUuid = null)
        {
            if (_deviceCache.TryGetValue(address, out var cached))
            {
                return new BleClient(cached, serviceUuid, writeUuid, notifyUuid);
            }
            return new BleClient(address, serviceUuid, writeUuid, notifyUuid);
        }

#if WINDOWS
        private void OnBleWatcherReceived(BluetoothLEAdvertisementWatcher sender, BluetoothLEAdvertisementReceivedEventArgs args)
        {
            var address = args.BluetoothAddress;
            var isNew = !_deviceCache.ContainsKey(address);

            var info = _deviceCache.GetOrAdd(address, addr => new BluetoothDeviceInfo
            {
                Address = addr,
                AddressString = BluetoothAddressHelper.Format(addr),
                Type = BluetoothType.Ble
            });

            lock (info)
            {
                info.Rssi = args.RawSignalStrengthInDBm;
                info.LastSeen = DateTime.Now;

                if (!string.IsNullOrWhiteSpace(args.Advertisement.LocalName))
                {
                    info.Name = args.Advertisement.LocalName;
                }

                if (args.Advertisement.ServiceUuids != null)
                {
                    foreach (var uuid in args.Advertisement.ServiceUuids)
                    {
                        if (!info.ServiceUuids.Contains(uuid))
                        {
                            info.ServiceUuids.Add(uuid);
                        }
                    }
                }
            }

            if (isNew)
            {
                RaiseDeviceDiscovered(info, true);
            }
            else
            {
                RaiseDeviceUpdated(info);
            }
        }

        private async void OnClassicDeviceAdded(DeviceWatcher sender, DeviceInformation args)
        {
            try
            {
                var bth = await BluetoothDevice.FromIdAsync(args.Id).AsTask().ConfigureAwait(false);
                if (bth == null) return;

                var address = bth.BluetoothAddress;
                var isNew = !_deviceCache.ContainsKey(address);

                var info = _deviceCache.GetOrAdd(address, addr => new BluetoothDeviceInfo
                {
                    Address = addr,
                    AddressString = BluetoothAddressHelper.Format(addr),
                    Type = BluetoothType.Classic
                });

                lock (info)
                {
                    info.DeviceId = args.Id;
                    info.Name = bth.Name ?? args.Name ?? string.Empty;
                    info.IsPaired = args.Pairing.IsPaired;
                    info.IsConnected = bth.ConnectionStatus == BluetoothConnectionStatus.Connected;
                    info.NativeDevice = bth;
                    info.LastSeen = DateTime.Now;
                }

                if (isNew)
                {
                    RaiseDeviceDiscovered(info, true);
                }
                else
                {
                    RaiseDeviceUpdated(info);
                }
            }
            catch { }
        }

        private void OnClassicDeviceUpdated(DeviceWatcher sender, DeviceInformationUpdate args)
        {
            var match = _deviceCache.Values.FirstOrDefault(d => d.DeviceId == args.Id);
            if (match != null)
            {
                match.LastSeen = DateTime.Now;
                RaiseDeviceUpdated(match);
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
            _deviceCache.Clear();
        }
    }
}
