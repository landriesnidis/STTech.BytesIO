using STTech.BytesIO.Bluetooth.Ble;
using STTech.BytesIO.Bluetooth.Common;
using STTech.BytesIO.Bluetooth.Entity;
using STTech.BytesIO.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
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
    [SupportedOSPlatform("windows10.0.19041.0")]
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
        private GattSession? _gattSession;
        private int _negotiatedPayloadSize = BlePacketSlicer.DefaultMinPayloadSize;
        private volatile bool _isConnected;

        // 工业常见已知透传服务与特征值预置表
        private static readonly HashSet<Guid> KnownIndustrialServiceUuids = new HashSet<Guid>
        {
            new Guid("6E400001-B5A3-F393-E0A9-E50E24DCCA9E"), // Nordic UART Service (NUS)
            new Guid("0000FFE0-0000-1000-8000-00805F9B34FB"), // TI / CC2540 / Telink
            new Guid("0000FFF0-0000-1000-8000-00805F9B34FB"), // 常见透传 FFF0
            new Guid("49535343-FE7D-4AE5-8FA9-9FAFD205E455"), // Microchip ISSC Transparent UART
            new Guid("0000FEE7-0000-1000-8000-00805F9B34FB"), // 常见工业网关透传
        };

        private static readonly HashSet<Guid> KnownIndustrialWriteCharacteristicUuids = new HashSet<Guid>
        {
            new Guid("6E400002-B5A3-F393-E0A9-E50E24DCCA9E"), // Nordic NUS RX (Write)
            new Guid("0000FFE1-0000-1000-8000-00805F9B34FB"), // TI FFE1 (Write/Notify)
            new Guid("0000FFF1-0000-1000-8000-00805F9B34FB"), // FFF1 (Write)
            new Guid("0000FFF2-0000-1000-8000-00805F9B34FB"), // FFF2 (Write)
            new Guid("49535343-8841-43F4-A8D4-ECBE34729BB3"), // ISSC RX
        };

        private static readonly HashSet<Guid> KnownIndustrialNotifyCharacteristicUuids = new HashSet<Guid>
        {
            new Guid("6E400003-B5A3-F393-E0A9-E50E24DCCA9E"), // Nordic NUS TX (Notify)
            new Guid("0000FFE1-0000-1000-8000-00805F9B34FB"), // TI FFE1 (Write/Notify)
            new Guid("0000FFF1-0000-1000-8000-00805F9B34FB"), // FFF1 (Notify)
            new Guid("0000FFF4-0000-1000-8000-00805F9B34FB"), // FFF4 (Notify)
            new Guid("49535343-1E4D-4BD9-BA61-23C647249616"), // ISSC TX
        };
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
        public BleWriteOption WriteOption { get; set; } = BleWriteOption.Auto;

        /// <inheritdoc/>
        public int MaxPayloadSize { get; set; } = 0;

        /// <inheritdoc/>
        public int WriteWithoutResponseIntervalMs { get; set; } = 2;

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
        /// <param name="serviceUuid">GATT 服务 UUID (为 null 时自动探测)</param>
        /// <param name="writeUuid">写入特征值 UUID (为 null 时自动探测)</param>
        /// <param name="notifyUuid">通知/指示特征值 UUID (为 null 时自动探测)</param>
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
        /// <param name="serviceUuid">GATT 服务 UUID (为 null 时自动探测)</param>
        /// <param name="writeUuid">写入特征值 UUID (为 null 时自动探测)</param>
        /// <param name="notifyUuid">通知/指示特征值 UUID (为 null 时自动探测)</param>
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
        /// <param name="serviceUuid">GATT 服务 UUID (为 null 时自动探测)</param>
        /// <param name="writeUuid">写入特征值 UUID (为 null 时自动探测)</param>
        /// <param name="notifyUuid">通知/指示特征值 UUID (为 null 时自动探测)</param>
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

            BluetoothLEDevice? tempDevice = null;
            GattDeviceService? tempService = null;
            GattSession? tempSession = null;
            GattCharacteristic? hookedNotifyChar = null;
            bool isDeviceAllocated = false;

            try
            {
                using var timeoutCts = argument.Timeout > 0 ? new CancellationTokenSource(argument.Timeout) : null;
                var ct = timeoutCts?.Token ?? CancellationToken.None;

                // 获取原生 BluetoothLEDevice，防御已被释放的缓存引用以支持可靠断线重连
                BluetoothLEDevice? device = null;
                if (DeviceInfo?.NativeDevice is BluetoothLEDevice cachedDevice)
                {
                    try
                    {
                        _ = cachedDevice.ConnectionStatus;
                        device = cachedDevice;
                    }
                    catch (ObjectDisposedException)
                    {
                        DeviceInfo.NativeDevice = null;
                    }
                }

                if (device == null)
                {
                    tempDevice = await BluetoothLEDevice.FromBluetoothAddressAsync(DeviceAddress).AsTask(ct).ConfigureAwait(false);
                    device = tempDevice;
                    isDeviceAllocated = true;
                    if (device != null && DeviceInfo != null)
                    {
                        DeviceInfo.NativeDevice = device;
                    }
                }

                if (device == null)
                {
                    throw new IOException($"未能发现或连接到 BLE 设备: {DeviceAddressString} (设备可能已断开广播或超出通信范围)");
                }

                GattDeviceService? targetService = null;
                GattCharacteristic? writeChar = null;
                GattCharacteristic? notifyChar = null;

                // 解析 GATT 服务与特征值
                if (ServiceUuid.HasValue)
                {
                    var servicesResult = await device.GetGattServicesForUuidAsync(ServiceUuid.Value).AsTask(ct).ConfigureAwait(false);
                    if (servicesResult.Status == GattCommunicationStatus.Success && servicesResult.Services.Count > 0)
                    {
                        targetService = servicesResult.Services[0];
                        // 释放可能查询出的其余多余服务实例
                        for (int i = 1; i < servicesResult.Services.Count; i++)
                        {
                            try { servicesResult.Services[i].Dispose(); } catch { }
                        }
                    }

                    if (targetService == null)
                    {
                        throw new IOException($"在 BLE 设备 {DeviceAddressString} 上未找到指定的 GATT 服务: {ServiceUuid.Value}");
                    }

                    tempService = targetService;

                    // 检索特征值
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

                    // 自动匹配该服务下未显式指定的特征值
                    if (writeChar == null || notifyChar == null)
                    {
                        var charList = await targetService.GetCharacteristicsAsync().AsTask(ct).ConfigureAwait(false);
                        if (charList.Status == GattCommunicationStatus.Success)
                        {
                            var resolved = ResolveOptimalCharacteristics(charList.Characteristics);
                            writeChar ??= resolved.Write;
                            notifyChar ??= resolved.Notify;
                        }
                    }
                }
                else
                {
                    // 自动探测服务：同服务内聚匹配 Write 与 Notify，坚决防止跨服务串扰，且严格释放未选服务
                    var allServices = await device.GetGattServicesAsync(BluetoothCacheMode.Uncached).AsTask(ct).ConfigureAwait(false);
                    if (allServices.Status == GattCommunicationStatus.Success && allServices.Services.Count > 0)
                    {
                        var candidates = new List<(GattDeviceService Service, GattCharacteristic Write, GattCharacteristic Notify, int Priority)>();

                        foreach (var svc in allServices.Services)
                        {
                            var chars = await svc.GetCharacteristicsAsync(BluetoothCacheMode.Uncached).AsTask(ct).ConfigureAwait(false);
                            if (chars.Status == GattCommunicationStatus.Success && chars.Characteristics.Count > 0)
                            {
                                var (svcWrite, svcNotify, priority) = ResolveOptimalCharacteristics(chars.Characteristics, svc.Uuid);

                                // 必须在同一服务内聚匹配
                                if (svcWrite != null && svcNotify != null)
                                {
                                    candidates.Add((svc, svcWrite, svcNotify, priority));
                                    continue;
                                }
                            }

                            // 未选中的服务立即 Dispose，避免原生句柄泄漏
                            try { svc.Dispose(); } catch { }
                        }

                        if (candidates.Count > 0)
                        {
                            // 按优先级降序排序 (知名透传服务 > 自定义透传 > 普通服务)
                            var selected = candidates.OrderByDescending(c => c.Priority).First();

                            targetService = selected.Service;
                            writeChar = selected.Write;
                            notifyChar = selected.Notify;
                            tempService = targetService;

                            // 释放其余未选中的候选服务
                            foreach (var cand in candidates)
                            {
                                if (!ReferenceEquals(cand.Service, targetService))
                                {
                                    try { cand.Service.Dispose(); } catch { }
                                }
                            }
                        }
                    }
                }

                if (targetService == null)
                {
                    throw new IOException($"在 BLE 设备 {DeviceAddressString} 上未找到可用的通信 GATT 服务。");
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
                hookedNotifyChar = notifyChar;

                var cccdValue = notifyChar.CharacteristicProperties.HasFlag(GattCharacteristicProperties.Notify)
                    ? GattClientCharacteristicConfigurationDescriptorValue.Notify
                    : GattClientCharacteristicConfigurationDescriptorValue.Indicate;

                var cccdResult = await notifyChar.WriteClientCharacteristicConfigurationDescriptorWithResultAsync(cccdValue).AsTask(ct).ConfigureAwait(false);
                if (cccdResult.Status != GattCommunicationStatus.Success)
                {
                    throw new IOException($"订阅 BLE 特征值通知失败: {cccdResult.Status}");
                }

                // 建立 GattSession 实现 Windows 平台连接保活与 MTU 动态同步
                try
                {
                    tempSession = await GattSession.FromDeviceIdAsync(device.BluetoothDeviceId).AsTask(ct).ConfigureAwait(false);
                    if (tempSession != null)
                    {
                        tempSession.MaintainConnection = true; // 防止 Windows 空闲休眠切断物理链路
                        UpdatePayloadSizeFromPdu(tempSession.MaxPduSize);
                        tempSession.MaxPduSizeChanged += OnGattSessionMaxPduSizeChanged;
                    }
                }
                catch
                {
                    // 兼容旧硬件驱动或不支持 GattSession 的系统环境
                    _negotiatedPayloadSize = BlePacketSlicer.DefaultMinPayloadSize;
                }

                lock (_statusLock)
                {
                    _device = device;
                    _service = targetService;
                    _writeCharacteristic = writeChar;
                    _notifyCharacteristic = notifyChar;
                    _gattSession = tempSession;
                    _isConnected = true;
                    _device.ConnectionStatusChanged += OnDeviceConnectionStatusChanged;

                    // 所有权已完整转移，清空局部暂存引用以防 finally 清理
                    tempDevice = null;
                    tempService = null;
                    tempSession = null;
                    hookedNotifyChar = null;
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
            finally
            {
                // 可靠释放未交接的局部资源，杜绝 COM 句柄泄漏
                if (hookedNotifyChar != null)
                {
                    try { hookedNotifyChar.ValueChanged -= OnCharacteristicValueChanged; } catch { }
                }
                if (tempSession != null)
                {
                    try { tempSession.Dispose(); } catch { }
                }
                if (tempService != null)
                {
                    try { tempService.Dispose(); } catch { }
                }
                if (isDeviceAllocated && tempDevice != null)
                {
                    try { tempDevice.Dispose(); } catch { }
                    if (DeviceInfo != null && ReferenceEquals(DeviceInfo.NativeDevice, tempDevice))
                    {
                        DeviceInfo.NativeDevice = null;
                    }
                }
            }
#else
            sw.Stop();
            var ex = new PlatformNotSupportedException(GetPlatformNotSupportedMessage());
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

#if WINDOWS
            lock (_statusLock)
            {
                // 只要此前处于已连接状态，或者底层原生资源尚未释放，就必须执行断开清理逻辑
                if (!_isConnected && _device == null && _service == null)
                {
                    return new DisconnectResult(DisconnectErrorCode.NoConnection);
                }

                try
                {
                    CancelReceiveDataTask();
                    CleanupResources();
                    RaiseDisconnected(this, new DisconnectedEventArgs(argument.ReasonCode, argument.Exception));
                    return new DisconnectResult();
                }
                catch (Exception ex)
                {
                    return new DisconnectResult(DisconnectErrorCode.Error, ex);
                }
            }
#else
            return new DisconnectResult(DisconnectErrorCode.NoConnection);
#endif
        }

        /// <inheritdoc/>
        protected override async Task SendHandlerAsync(SendArgs args)
        {
#if WINDOWS
            if (!IsConnected || _writeCharacteristic == null)
            {
                throw new InvalidOperationException("BLE 客户端未连接或写入特征值未就绪，无法发送数据。");
            }

            int maxChunk = MaxPayloadSize > 0 ? MaxPayloadSize : _negotiatedPayloadSize;
            if (maxChunk <= 0)
            {
                maxChunk = BlePacketSlicer.DefaultMinPayloadSize;
            }

            // 自适应协商特征值写入方式，杜绝 ArgumentException 崩溃
            var gattOption = ResolveGattWriteOption(_writeCharacteristic);

            var slices = BlePacketSlicer.Slice(args.Data, maxChunk).ToList();
            for (int i = 0; i < slices.Count; i++)
            {
                var slice = slices[i];
                // 零拷贝优化：直接通过 AsBuffer 将原数组切片映射为 WinRT IBuffer，消除 DataWriter 与中间 byte[] 堆分配
                var buffer = slice.Array!.AsBuffer(slice.Offset, slice.Count);

                var result = await _writeCharacteristic.WriteValueWithResultAsync(buffer, gattOption).AsTask().ConfigureAwait(false);
                if (result.Status != GattCommunicationStatus.Success)
                {
                    throw new IOException($"BLE 特征值写入失败: {result.Status}, 协议错误代码: {result.ProtocolError}");
                }

                // 仅在实际采用无响应写入时应用分包流控微延迟
                if (gattOption == GattWriteOption.WriteWithoutResponse && WriteWithoutResponseIntervalMs > 0 && i < slices.Count - 1)
                {
                    await Task.Delay(WriteWithoutResponseIntervalMs).ConfigureAwait(false);
                }
            }

            RaiseDataSent(this, new DataSentEventArgs(args.Data));
#else
            await Task.FromException(new PlatformNotSupportedException(GetPlatformNotSupportedMessage())).ConfigureAwait(false);
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
        private GattWriteOption ResolveGattWriteOption(GattCharacteristic characteristic)
        {
            var props = characteristic.CharacteristicProperties;
            bool supportsNoResp = props.HasFlag(GattCharacteristicProperties.WriteWithoutResponse);
            bool supportsWithResp = props.HasFlag(GattCharacteristicProperties.Write);

            var effective = ResolveEffectiveWriteOption(WriteOption, supportsNoResp, supportsWithResp);
            return effective == BleWriteOption.WriteWithoutResponse
                ? GattWriteOption.WriteWithoutResponse
                : GattWriteOption.WriteWithResponse;
        }

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
            // 只要底层设备已变为 Disconnected，且本客户端此前处于连接中，立刻触发被动断线
            if (sender.ConnectionStatus == BluetoothConnectionStatus.Disconnected && _isConnected)
            {
                Disconnect(new DisconnectArgument(DisconnectionReasonCode.Passive));
            }
        }

        private void OnGattSessionMaxPduSizeChanged(GattSession sender, object args)
        {
            UpdatePayloadSizeFromPdu(sender.MaxPduSize);
        }

        private void UpdatePayloadSizeFromPdu(ushort maxPduSize)
        {
            if (maxPduSize > 3)
            {
                _negotiatedPayloadSize = maxPduSize - 3;
            }
            else
            {
                _negotiatedPayloadSize = BlePacketSlicer.DefaultMinPayloadSize;
            }
        }

        private void CleanupResources()
        {
            _isConnected = false;

            if (_notifyCharacteristic != null)
            {
                try
                {
                    _notifyCharacteristic.ValueChanged -= OnCharacteristicValueChanged;

                    // 仅当物理链路仍处于连接状态时才尝试注销空中 CCCD；若物理已断开，坚决不发送空中报文，防止死等阻塞
                    if (_device != null && _device.ConnectionStatus == BluetoothConnectionStatus.Connected)
                    {
                        try
                        {
                            var unregisterOp = _notifyCharacteristic.WriteClientCharacteristicConfigurationDescriptorWithResultAsync(
                                GattClientCharacteristicConfigurationDescriptorValue.None);
                            var unregisterWait = unregisterOp.AsTask();
                            unregisterWait.Wait(200);
                        }
                        catch { }
                    }
                }
                catch { }
                _notifyCharacteristic = null;
            }

            _writeCharacteristic = null;

            if (_gattSession != null)
            {
                try
                {
                    _gattSession.MaxPduSizeChanged -= OnGattSessionMaxPduSizeChanged;
                    _gattSession.MaintainConnection = false;
                    _gattSession.Dispose();
                }
                catch { }
                _gattSession = null;
            }

            if (_service != null)
            {
                try { _service.Dispose(); } catch { }
                _service = null;
            }

            if (_device != null)
            {
                try { _device.ConnectionStatusChanged -= OnDeviceConnectionStatusChanged; } catch { }
                try { _device.Dispose(); } catch { }

                // 同步解耦 DeviceInfo 缓存的原生对象，防止重连时拿到被 Dispose 的实例
                if (DeviceInfo != null && ReferenceEquals(DeviceInfo.NativeDevice, _device))
                {
                    DeviceInfo.NativeDevice = null;
                }
                _device = null;
            }
        }

        private static (GattCharacteristic? Write, GattCharacteristic? Notify, int Priority) ResolveOptimalCharacteristics(
            IReadOnlyList<GattCharacteristic> characteristics, Guid? serviceUuid = null)
        {
            GattCharacteristic? writeChar = null;
            GattCharacteristic? notifyChar = null;
            int priority = 0;

            if (serviceUuid.HasValue)
            {
                if (KnownIndustrialServiceUuids.Contains(serviceUuid.Value))
                {
                    priority += 100;
                }
                else if (!IsBluetoothSigReservedService(serviceUuid.Value))
                {
                    priority += 50;
                }
            }

            // 1. 优先在知名工业 UUID 中匹配
            foreach (var c in characteristics)
            {
                if (writeChar == null && KnownIndustrialWriteCharacteristicUuids.Contains(c.Uuid) &&
                    (c.CharacteristicProperties.HasFlag(GattCharacteristicProperties.Write) || c.CharacteristicProperties.HasFlag(GattCharacteristicProperties.WriteWithoutResponse)))
                {
                    writeChar = c;
                    priority += 20;
                }
                if (notifyChar == null && KnownIndustrialNotifyCharacteristicUuids.Contains(c.Uuid) &&
                    (c.CharacteristicProperties.HasFlag(GattCharacteristicProperties.Notify) || c.CharacteristicProperties.HasFlag(GattCharacteristicProperties.Indicate)))
                {
                    notifyChar = c;
                    priority += 20;
                }
            }

            // 2. 其次优先选择具备全双工能力 (Write + Notify) 的透传特征值 (如 TI FFE1)
            if (writeChar == null || notifyChar == null)
            {
                foreach (var c in characteristics)
                {
                    bool canWrite = c.CharacteristicProperties.HasFlag(GattCharacteristicProperties.Write) || c.CharacteristicProperties.HasFlag(GattCharacteristicProperties.WriteWithoutResponse);
                    bool canNotify = c.CharacteristicProperties.HasFlag(GattCharacteristicProperties.Notify) || c.CharacteristicProperties.HasFlag(GattCharacteristicProperties.Indicate);
                    if (canWrite && canNotify)
                    {
                        writeChar ??= c;
                        notifyChar ??= c;
                        priority += 10;
                        break;
                    }
                }
            }

            // 3. 最后补充普通特征值
            if (writeChar == null || notifyChar == null)
            {
                foreach (var c in characteristics)
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

            return (writeChar, notifyChar, priority);
        }
#endif

        /// <summary>
        /// 自适应确定实际使用的特征值写入方式
        /// </summary>
        internal static BleWriteOption ResolveEffectiveWriteOption(BleWriteOption preference, bool supportsWithoutResponse, bool supportsWithResponse)
        {
            switch (preference)
            {
                case BleWriteOption.Auto:
                    // 自适应模式：优先采用高吞吐率的无响应写入，若不支持则降级为带响应写入
                    if (supportsWithoutResponse) return BleWriteOption.WriteWithoutResponse;
                    if (supportsWithResponse) return BleWriteOption.WriteWithResponse;
                    return BleWriteOption.WriteWithoutResponse;

                case BleWriteOption.WriteWithoutResponse:
                    // 优先无响应写入；若硬件仅支持带响应则自动降级，避免 ArgumentException 异常
                    if (supportsWithoutResponse) return BleWriteOption.WriteWithoutResponse;
                    if (supportsWithResponse) return BleWriteOption.WriteWithResponse;
                    return BleWriteOption.WriteWithoutResponse;

                case BleWriteOption.WriteWithResponse:
                    // 优先带响应写入；若硬件仅支持无响应则自动回退
                    if (supportsWithResponse) return BleWriteOption.WriteWithResponse;
                    if (supportsWithoutResponse) return BleWriteOption.WriteWithoutResponse;
                    return BleWriteOption.WriteWithResponse;

                default:
                    return supportsWithoutResponse ? BleWriteOption.WriteWithoutResponse : BleWriteOption.WriteWithResponse;
            }
        }

        /// <summary>
        /// 判断是否为 Bluetooth SIG 官方保留服务 (0x1800 ~ 0x18FF)
        /// </summary>
        internal static bool IsBluetoothSigReservedService(Guid uuid)
        {
            var bytes = uuid.ToByteArray();
            // Bluetooth SIG Base UUID: 0000xxxx-0000-1000-8000-00805F9B34FB
            // 注意: .NET 中 Guid.ToByteArray() 的第三段 (short _c) 为小端序存储:
            // 0x1000 在 bytes[6..7] 中 low byte在前、high byte在后，即 bytes[6] == 0x00, bytes[7] == 0x10
            if (bytes[4] == 0x00 && bytes[5] == 0x00 && bytes[6] == 0x00 && bytes[7] == 0x10 &&
                bytes[8] == 0x80 && bytes[9] == 0x00 && bytes[10] == 0x00 && bytes[11] == 0x80 &&
                bytes[12] == 0x5F && bytes[13] == 0x9B && bytes[14] == 0x34 && bytes[15] == 0xFB)
            {
                ushort shortId = BitConverter.ToUInt16(bytes, 0);
                // 0x1800 ~ 0x18FF 为标准保留 Profile (GAP, GATT, Battery, Device Info 等)
                return shortId >= 0x1800 && shortId <= 0x18FF;
            }
            return false;
        }

        private static string GetPlatformNotSupportedMessage()
        {
            return "STTech.BytesIO.Bluetooth 原生 BLE 通信目前仅在 Windows 10/11 (windows10.0.19041.0+) 环境下受支持。" +
                   "若项目运行在 Windows 上，请确保消费工程 csproj 指定了带 '-windows10.0.19041.0' 后缀的 TargetFramework (例如 <TargetFramework>net8.0-windows10.0.19041.0</TargetFramework>)。";
        }

        /// <inheritdoc/>
        public override void Dispose()
        {
            Disconnect();
        }
    }
}
