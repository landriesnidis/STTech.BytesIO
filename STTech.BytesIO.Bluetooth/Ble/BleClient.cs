using STTech.BytesIO.Bluetooth.Common;
using STTech.BytesIO.Bluetooth.Entity;
using STTech.BytesIO.Core;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

#if WINDOWS
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Storage.Streams;
#endif

namespace STTech.BytesIO.Bluetooth
{
    /// <summary>
    /// 低功耗蓝牙通信客户端 (基于 BLE / GATT 服务与特征值通信)
    /// </summary>
    public class BleClient : BytesClient, IBleClient
    {
        private readonly object _statusLock = new object();
        private ulong _deviceAddress;
        private string _deviceAddressString = string.Empty;
        private string? _deviceName;

#if WINDOWS
        private BluetoothLEDevice? _device;
        private GattDeviceService? _service;
        private GattCharacteristic? _writeCharacteristic;
        private GattCharacteristic? _notifyCharacteristic;
        private volatile bool _isConnected;
#endif

        /// <inheritdoc/>
        public ulong DeviceAddress
        {
            get => _deviceAddress;
            set
            {
                if (IsConnected)
                {
                    throw new InvalidOperationException("连接建立后不允许修改 DeviceAddress。");
                }
                _deviceAddress = value;
                _deviceAddressString = BluetoothAddressHelper.Format(value);
            }
        }

        /// <inheritdoc/>
        public string DeviceAddressString
        {
            get => _deviceAddressString;
            set
            {
                if (IsConnected)
                {
                    throw new InvalidOperationException("连接建立后不允许修改 DeviceAddressString。");
                }
                _deviceAddressString = value ?? string.Empty;
                if (BluetoothAddressHelper.TryParse(_deviceAddressString, out var addr))
                {
                    _deviceAddress = addr;
                }
            }
        }

        /// <inheritdoc/>
        public string DeviceName
        {
            get => _deviceName ?? DeviceInfo?.Name ?? string.Empty;
            set => _deviceName = value;
        }

        /// <inheritdoc/>
        public Guid? ServiceUuid { get; set; }

        /// <inheritdoc/>
        public Guid? WriteCharacteristicUuid { get; set; }

        /// <inheritdoc/>
        public Guid? NotifyCharacteristicUuid { get; set; }

        /// <inheritdoc/>
        public BleWriteOption WriteOption { get; set; } = BleWriteOption.WriteWithoutResponse;

        /// <inheritdoc/>
        public BluetoothDeviceInfo? DeviceInfo { get; set; }

        /// <inheritdoc/>
        public override bool IsConnected
        {
            get
            {
#if WINDOWS
                return _isConnected && _device != null && _device.ConnectionStatus == BluetoothConnectionStatus.Connected;
#else
                return false;
#endif
            }
        }

        /// <summary>
        /// 构造低功耗蓝牙通信客户端
        /// </summary>
        public BleClient()
        {
        }

        /// <summary>
        /// 构造低功耗蓝牙通信客户端
        /// </summary>
        /// <param name="address">BLE 设备地址数值 (MAC)</param>
        /// <param name="serviceUuid">GATT 服务 UUID (可选，为 null 时自动探测)</param>
        /// <param name="writeUuid">写入特征值 UUID (可选，为 null 时自动探测)</param>
        /// <param name="notifyUuid">通知/指示特征值 UUID (可选，为 null 时自动探测)</param>
        public BleClient(ulong address, Guid? serviceUuid = null, Guid? writeUuid = null, Guid? notifyUuid = null)
        {
            DeviceAddress = address;
            ServiceUuid = serviceUuid;
            WriteCharacteristicUuid = writeUuid;
            NotifyCharacteristicUuid = notifyUuid;
        }

        /// <summary>
        /// 构造低功耗蓝牙通信客户端
        /// </summary>
        /// <param name="addressString">BLE 设备地址字符串 (如 "00:1A:7D:DA:71:13")</param>
        /// <param name="serviceUuid">GATT 服务 UUID (可选，为 null 时自动探测)</param>
        /// <param name="writeUuid">写入特征值 UUID (可选，为 null 时自动探测)</param>
        /// <param name="notifyUuid">通知/指示特征值 UUID (可选，为 null 时自动探测)</param>
        public BleClient(string addressString, Guid? serviceUuid = null, Guid? writeUuid = null, Guid? notifyUuid = null)
        {
            DeviceAddressString = addressString;
            ServiceUuid = serviceUuid;
            WriteCharacteristicUuid = writeUuid;
            NotifyCharacteristicUuid = notifyUuid;
        }

        /// <summary>
        /// 构造低功耗蓝牙通信客户端
        /// </summary>
        /// <param name="deviceInfo">发现的 BLE 设备信息</param>
        /// <param name="serviceUuid">GATT 服务 UUID (可选，为 null 时自动探测)</param>
        /// <param name="writeUuid">写入特征值 UUID (可选，为 null 时自动探测)</param>
        /// <param name="notifyUuid">通知/指示特征值 UUID (可选，为 null 时自动探测)</param>
        public BleClient(BluetoothDeviceInfo deviceInfo, Guid? serviceUuid = null, Guid? writeUuid = null, Guid? notifyUuid = null)
        {
            DeviceInfo = deviceInfo ?? throw new ArgumentNullException(nameof(deviceInfo));
            DeviceAddress = deviceInfo.Address;
            ServiceUuid = serviceUuid;
            WriteCharacteristicUuid = writeUuid;
            NotifyCharacteristicUuid = notifyUuid;
        }

        /// <inheritdoc/>
        public override ConnectResult Connect(ConnectArgument? argument = null)
        {
            return ConnectAsync(argument).GetAwaiter().GetResult();
        }

        /// <inheritdoc/>
        public override async Task<ConnectResult> ConnectAsync(ConnectArgument? argument = null)
        {
            argument ??= new ConnectArgument();
            var sw = System.Diagnostics.Stopwatch.StartNew();

            if (IsConnected)
            {
                sw.Stop();
                return new ConnectResult(ConnectErrorCode.IsConnected) { CostTime = sw.Elapsed };
            }

#if WINDOWS
            if (DeviceAddress == 0)
            {
                sw.Stop();
                var ex = new InvalidOperationException("未配置有效的 BLE 设备地址 (DeviceAddress)。");
                RaiseConnectionFailed(this, new ConnectionFailedEventArgs(ConnectErrorCode.Error, ex) { CostTime = sw.Elapsed, State = argument.State });
                return new ConnectResult(ConnectErrorCode.Error, ex) { CostTime = sw.Elapsed };
            }

            try
            {
                using var timeoutCts = argument.Timeout > 0 ? new CancellationTokenSource(argument.Timeout) : null;
                var ct = timeoutCts?.Token ?? CancellationToken.None;

                // 获取原生 BluetoothLEDevice
                var device = DeviceInfo?.NativeDevice as BluetoothLEDevice;
                if (device == null)
                {
                    device = await BluetoothLEDevice.FromBluetoothAddressAsync(DeviceAddress).AsTask(ct).ConfigureAwait(false);
                }

                if (device == null)
                {
                    throw new IOException($"未能发现或连接到 BLE 设备: {DeviceAddressString} (设备可能已断开广播或超出通信范围)");
                }

                GattDeviceService? targetService = null;
                GattCharacteristic? writeChar = null;
                GattCharacteristic? notifyChar = null;

                // 解析 GATT 服务
                if (ServiceUuid.HasValue)
                {
                    var servicesResult = await device.GetGattServicesForUuidAsync(ServiceUuid.Value).AsTask(ct).ConfigureAwait(false);
                    if (servicesResult.Status == GattCommunicationStatus.Success && servicesResult.Services.Count > 0)
                    {
                        targetService = servicesResult.Services[0];
                    }
                }
                else
                {
                    // 自动探测服务
                    var allServices = await device.GetGattServicesAsync().AsTask(ct).ConfigureAwait(false);
                    if (allServices.Status == GattCommunicationStatus.Success && allServices.Services.Count > 0)
                    {
                        // 优先查找包含传输特征值的自定义服务
                        foreach (var svc in allServices.Services)
                        {
                            var chars = await svc.GetCharacteristicsAsync().AsTask(ct).ConfigureAwait(false);
                            if (chars.Status == GattCommunicationStatus.Success && chars.Characteristics.Count > 0)
                            {
                                foreach (var c in chars.Characteristics)
                                {
                                    if (writeChar == null && (c.CharacteristicProperties.HasFlag(GattCharacteristicProperties.Write) || c.CharacteristicProperties.HasFlag(GattCharacteristicProperties.WriteWithoutResponse)))
                                    {
                                        writeChar = c;
                                    }
                                    if (notifyChar == null && (c.CharacteristicProperties.HasFlag(GattCharacteristicProperties.Notify) || c.CharacteristicProperties.HasFlag(GattCharacteristicProperties.Indicate)))
                                    {
                                        notifyChar = c;
                                    }
                                }

                                if (writeChar != null && notifyChar != null)
                                {
                                    targetService = svc;
                                    break;
                                }
                            }
                        }
                    }
                }

                if (targetService == null)
                {
                    throw new IOException($"在 BLE 设备 {DeviceAddressString} 上未找到可用的通信 GATT 服务。");
                }

                // 若此前指定了服务 UUID，继续解析该服务下的特征值
                if (writeChar == null || notifyChar == null)
                {
                    if (WriteCharacteristicUuid.HasValue)
                    {
                        var wcRes = await targetService.GetCharacteristicsForUuidAsync(WriteCharacteristicUuid.Value).AsTask(ct).ConfigureAwait(false);
                        if (wcRes.Status == GattCommunicationStatus.Success && wcRes.Characteristics.Count > 0)
                        {
                            writeChar = wcRes.Characteristics[0];
                        }
                    }

                    if (NotifyCharacteristicUuid.HasValue)
                    {
                        var ncRes = await targetService.GetCharacteristicsForUuidAsync(NotifyCharacteristicUuid.Value).AsTask(ct).ConfigureAwait(false);
                        if (ncRes.Status == GattCommunicationStatus.Success && ncRes.Characteristics.Count > 0)
                        {
                            notifyChar = ncRes.Characteristics[0];
                        }
                    }

                    // 自动寻找未指定的特征值
                    var charList = await targetService.GetCharacteristicsAsync().AsTask(ct).ConfigureAwait(false);
                    if (charList.Status == GattCommunicationStatus.Success)
                    {
                        foreach (var c in charList.Characteristics)
                        {
                            if (writeChar == null && (c.CharacteristicProperties.HasFlag(GattCharacteristicProperties.Write) || c.CharacteristicProperties.HasFlag(GattCharacteristicProperties.WriteWithoutResponse)))
                            {
                                writeChar = c;
                            }
                            if (notifyChar == null && (c.CharacteristicProperties.HasFlag(GattCharacteristicProperties.Notify) || c.CharacteristicProperties.HasFlag(GattCharacteristicProperties.Indicate)))
                            {
                                notifyChar = c;
                            }
                        }
                    }
                }

                if (writeChar == null)
                {
                    throw new IOException($"在 BLE 服务 {targetService.Uuid} 下未找到可写特征值。");
                }
                if (notifyChar == null)
                {
                    throw new IOException($"在 BLE 服务 {targetService.Uuid} 下未找到具备通知/指示 (Notify/Indicate) 功能的特征值。");
                }

                // 订阅数据接收通知
                notifyChar.ValueChanged += OnCharacteristicValueChanged;
                var cccdValue = notifyChar.CharacteristicProperties.HasFlag(GattCharacteristicProperties.Notify)
                    ? GattClientCharacteristicConfigurationDescriptorValue.Notify
                    : GattClientCharacteristicConfigurationDescriptorValue.Indicate;

                var cccdResult = await notifyChar.WriteClientCharacteristicConfigurationDescriptorWithResultAsync(cccdValue).AsTask(ct).ConfigureAwait(false);
                if (cccdResult.Status != GattCommunicationStatus.Success)
                {
                    throw new IOException($"订阅 BLE 特征值通知失败: {cccdResult.Status}");
                }

                lock (_statusLock)
                {
                    _device = device;
                    _service = targetService;
                    _writeCharacteristic = writeChar;
                    _notifyCharacteristic = notifyChar;
                    _isConnected = true;
                    _device.ConnectionStatusChanged += OnDeviceConnectionStatusChanged;
                }

                GenerateNewConnectionId();
                sw.Stop();

                RaiseConnectedSuccessfully(this, new ConnectedSuccessfullyEventArgs { CostTime = sw.Elapsed, State = argument.State });
                StartReceiveDataTask();

                return new ConnectResult { CostTime = sw.Elapsed };
            }
            catch (Exception ex)
            {
                sw.Stop();
                CleanupResources();
                var failedArgs = new ConnectionFailedEventArgs(ConnectErrorCode.Error, ex) { CostTime = sw.Elapsed, State = argument.State };
                RaiseConnectionFailed(this, failedArgs);
                return new ConnectResult(ConnectErrorCode.Error, ex) { CostTime = sw.Elapsed };
            }
#else
            sw.Stop();
            var ex = new PlatformNotSupportedException("STTech.BytesIO.Bluetooth 当前仅在 Windows 10/11 (windows10.0.19041.0+) 平台上支持原生 BLE 通信。");
            RaiseConnectionFailed(this, new ConnectionFailedEventArgs(ConnectErrorCode.Error, ex) { CostTime = sw.Elapsed, State = argument.State });
            return new ConnectResult(ConnectErrorCode.Error, ex) { CostTime = sw.Elapsed };
#endif
        }

        /// <inheritdoc/>
        public override DisconnectResult Disconnect(DisconnectArgument? argument = null)
        {
            argument ??= new DisconnectArgument();

            if (argument.GracefulShutdown)
            {
                WaitAndDrainSendQueue(3000);
            }

            lock (_statusLock)
            {
                if (!IsConnected)
                {
                    return new DisconnectResult(DisconnectErrorCode.NoConnection);
                }

                try
                {
                    CancelReceiveDataTask();
#if WINDOWS
                    CleanupResources();
#endif
                    RaiseDisconnected(this, new DisconnectedEventArgs(argument.ReasonCode, argument.Exception));
                    return new DisconnectResult();
                }
                catch (Exception ex)
                {
                    return new DisconnectResult(DisconnectErrorCode.Error, ex);
                }
            }
        }

        /// <inheritdoc/>
        protected override async Task SendHandlerAsync(SendArgs args)
        {
#if WINDOWS
            if (!IsConnected || _writeCharacteristic == null)
            {
                throw new InvalidOperationException("BLE 客户端未连接或写入特征值未就绪，无法发送数据。");
            }

            var writer = new DataWriter();
            writer.WriteBytes(args.Data);
            var buffer = writer.DetachBuffer();

            var gattOption = WriteOption == BleWriteOption.WriteWithoutResponse
                ? GattWriteOption.WriteWithoutResponse
                : GattWriteOption.WriteWithResponse;

            var result = await _writeCharacteristic.WriteValueWithResultAsync(buffer, gattOption).AsTask().ConfigureAwait(false);
            if (result.Status != GattCommunicationStatus.Success)
            {
                throw new IOException($"BLE 特征值写入失败: {result.Status}, 协议错误代码: {result.ProtocolError}");
            }

            RaiseDataSent(this, new DataSentEventArgs(args.Data));
#else
            await Task.FromException(new PlatformNotSupportedException("STTech.BytesIO.Bluetooth 当前仅在 Windows 平台上可用。")).ConfigureAwait(false);
#endif
        }

        /// <inheritdoc/>
        protected override async Task ReceiveDataHandleAsync(CancellationToken cancellationToken)
        {
#if WINDOWS
            // BLE 接收为基于事件的主动推送，通过长延时挂起维持接收任务生命周期
            await Task.Delay(Timeout.Infinite, cancellationToken).ConfigureAwait(false);
#else
            await Task.CompletedTask;
#endif
        }

        /// <inheritdoc/>
        protected override void ReceiveDataCompletedHandle()
        {
            Disconnect(new DisconnectArgument(DisconnectionReasonCode.Passive));
        }

#if WINDOWS
        private void OnCharacteristicValueChanged(GattCharacteristic sender, GattValueChangedEventArgs args)
        {
            var rawBuffer = args.CharacteristicValue;
            int length = (int)rawBuffer.Length;
            if (length <= 0) return;

            byte[] rented = RentBuffer();
            if (length <= rented.Length)
            {
                rawBuffer.CopyTo(0, rented, 0, length);
                var context = CreateReceiveContext(rented, 0, length);
                InvokeDataReceivedEventCallback(context);
            }
            else
            {
                System.Buffers.ArrayPool<byte>.Shared.Return(rented);
                byte[] exact = rawBuffer.ToArray();
                var context = new ReceiveContext(exact);
                InvokeDataReceivedEventCallback(context);
            }
        }

        private void OnDeviceConnectionStatusChanged(BluetoothLEDevice sender, object args)
        {
            if (sender.ConnectionStatus == BluetoothConnectionStatus.Disconnected && IsConnected)
            {
                Disconnect(new DisconnectArgument(DisconnectionReasonCode.Passive));
            }
        }

        private void CleanupResources()
        {
            _isConnected = false;

            if (_notifyCharacteristic != null)
            {
                try { _notifyCharacteristic.ValueChanged -= OnCharacteristicValueChanged; } catch { }
                _notifyCharacteristic = null;
            }

            _writeCharacteristic = null;

            if (_service != null)
            {
                try { _service.Dispose(); } catch { }
                _service = null;
            }

            if (_device != null)
            {
                try { _device.ConnectionStatusChanged -= OnDeviceConnectionStatusChanged; } catch { }
                try { _device.Dispose(); } catch { }
                _device = null;
            }
        }
#endif

        /// <inheritdoc/>
        public override void Dispose()
        {
            Disconnect();
        }
    }
}
