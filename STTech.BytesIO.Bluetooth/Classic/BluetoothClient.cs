using STTech.BytesIO.Bluetooth.Common;
using STTech.BytesIO.Bluetooth.Entity;
using STTech.BytesIO.Core;
using System;
using System.IO;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;

#if WINDOWS
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Rfcomm;
using Windows.Devices.Enumeration;
using Windows.Networking.Sockets;
#endif

namespace STTech.BytesIO.Bluetooth
{
    /// <summary>
    /// 经典蓝牙通信客户端 (基于 RFCOMM / SPP 串行端口规范)
    /// </summary>
    [SupportedOSPlatform("windows10.0.19041.0")]
    public class BluetoothClient : BytesClient, IBluetoothClient
    {
        /// <summary>
        /// 默认 SPP (Serial Port Profile) 服务 UUID: 00001101-0000-1000-8000-00805F9B34FB
        /// </summary>
        public static readonly Guid SppServiceUuid = new Guid("00001101-0000-1000-8000-00805F9B34FB");

        private readonly object _statusLock = new object();
        private ulong _deviceAddress;
        private string _deviceAddressString = string.Empty;
        private string? _deviceName;
        private Guid _serviceUuid = SppServiceUuid;

#if WINDOWS
        private BluetoothDevice? _device;
        private RfcommDeviceService? _service;
        private StreamSocket? _socket;
        private Stream? _inStream;
        private Stream? _outStream;
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
        public Guid ServiceUuid
        {
            get => _serviceUuid;
            set
            {
                if (IsConnected)
                {
                    throw new InvalidOperationException("连接建立后不允许修改 ServiceUuid。");
                }
                _serviceUuid = value;
            }
        }

        /// <inheritdoc/>
        public BluetoothDeviceInfo? DeviceInfo { get; set; }

        /// <inheritdoc/>
        public override bool IsConnected
        {
            get
            {
#if WINDOWS
                return _isConnected && _socket != null;
#else
                return false;
#endif
            }
        }

        /// <summary>
        /// 构造经典蓝牙通信客户端 (支持无参构造以满足序列化/反序列化需求)
        /// </summary>
        public BluetoothClient()
        {
        }

        /// <summary>
        /// 构造经典蓝牙通信客户端
        /// </summary>
        /// <param name="address">蓝牙设备地址数值 (MAC)</param>
        /// <param name="serviceUuid">RFCOMM 服务 UUID (默认为 SPP 串口服务 UUID)</param>
        public BluetoothClient(ulong address, Guid? serviceUuid = null)
        {
            DeviceAddress = address;
            if (serviceUuid.HasValue)
            {
                ServiceUuid = serviceUuid.Value;
            }
        }

        /// <summary>
        /// 构造经典蓝牙通信客户端
        /// </summary>
        /// <param name="addressString">蓝牙设备地址字符串 (如 "00:1A:7D:DA:71:13")</param>
        /// <param name="serviceUuid">RFCOMM 服务 UUID (默认为 SPP 串口服务 UUID)</param>
        public BluetoothClient(string addressString, Guid? serviceUuid = null)
        {
            DeviceAddressString = addressString;
            if (serviceUuid.HasValue)
            {
                ServiceUuid = serviceUuid.Value;
            }
        }

        /// <summary>
        /// 构造经典蓝牙通信客户端
        /// </summary>
        /// <param name="deviceInfo">发现的蓝牙设备信息</param>
        /// <param name="serviceUuid">RFCOMM 服务 UUID (默认为 SPP 串口服务 UUID)</param>
        public BluetoothClient(BluetoothDeviceInfo deviceInfo, Guid? serviceUuid = null)
        {
            DeviceInfo = deviceInfo ?? throw new ArgumentNullException(nameof(deviceInfo));
            DeviceAddress = deviceInfo.Address;
            if (serviceUuid.HasValue)
            {
                ServiceUuid = serviceUuid.Value;
            }
        }

        /// <inheritdoc/>
        public Task<bool> PairAsync()
        {
            return PairAsync(null, CancellationToken.None);
        }

        /// <inheritdoc/>
        public async Task<bool> PairAsync(string? pinCode, CancellationToken cancellationToken = default)
        {
#if WINDOWS
            if (DeviceAddress == 0)
            {
                throw new InvalidOperationException("未配置有效的蓝牙设备地址 (DeviceAddress)。");
            }

            var device = DeviceInfo?.NativeDevice as BluetoothDevice;
            bool isDeviceAllocated = false;
            if (device == null)
            {
                device = await BluetoothDevice.FromBluetoothAddressAsync(DeviceAddress).AsTask(cancellationToken).ConfigureAwait(false);
                isDeviceAllocated = true;
            }

            if (device == null)
            {
                throw new IOException($"未能发现蓝牙设备: {DeviceAddressString}");
            }

            try
            {
                if (device.DeviceInformation.Pairing.IsPaired)
                {
                    return true;
                }

                var customPairing = device.DeviceInformation.Pairing.Custom;
                if (customPairing != null)
                {
                    void OnPairingRequested(DeviceInformationCustomPairing sender, DevicePairingRequestedEventArgs args)
                    {
                        switch (args.PairingKind)
                        {
                            case DevicePairingKinds.ConfirmOnly:
                                args.Accept();
                                break;
                            case DevicePairingKinds.ProvidePin:
                                args.Accept(string.IsNullOrEmpty(pinCode) ? "1234" : pinCode);
                                break;
                            case DevicePairingKinds.ConfirmPinMatch:
                                args.Accept();
                                break;
                            case DevicePairingKinds.DisplayPin:
                                args.Accept();
                                break;
                            default:
                                args.Accept();
                                break;
                        }
                    }

                    customPairing.PairingRequested += OnPairingRequested;
                    try
                    {
                        var kinds = DevicePairingKinds.ConfirmOnly | DevicePairingKinds.ProvidePin | DevicePairingKinds.ConfirmPinMatch | DevicePairingKinds.DisplayPin;
                        var result = await customPairing.PairAsync(kinds, DevicePairingProtectionLevel.None).AsTask(cancellationToken).ConfigureAwait(false);
                        return result.Status == DevicePairingResultStatus.Paired || result.Status == DevicePairingResultStatus.AlreadyPaired;
                    }
                    finally
                    {
                        customPairing.PairingRequested -= OnPairingRequested;
                    }
                }

                var res = await device.DeviceInformation.Pairing.PairAsync().AsTask(cancellationToken).ConfigureAwait(false);
                return res.Status == DevicePairingResultStatus.Paired || res.Status == DevicePairingResultStatus.AlreadyPaired;
            }
            finally
            {
                if (isDeviceAllocated && !IsConnected)
                {
                    try { device.Dispose(); } catch { }
                }
            }
#else
            await Task.CompletedTask;
            return false;
#endif
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
                var ex = new InvalidOperationException("未配置有效的蓝牙设备地址 (DeviceAddress)。");
                RaiseConnectionFailed(this, new ConnectionFailedEventArgs(ConnectErrorCode.Error, ex) { CostTime = sw.Elapsed, State = argument.State });
                return new ConnectResult(ConnectErrorCode.Error, ex) { CostTime = sw.Elapsed };
            }

            BluetoothDevice? tempDevice = null;
            RfcommDeviceService? tempService = null;
            StreamSocket? tempSocket = null;
            bool isDeviceAllocated = false;

            try
            {
                using var timeoutCts = argument.Timeout > 0 ? new CancellationTokenSource(argument.Timeout) : null;
                var ct = timeoutCts?.Token ?? CancellationToken.None;

                // 获取原生 BluetoothDevice，防御已被释放的缓存引用以支持可靠断线重连
                BluetoothDevice? device = null;
                if (DeviceInfo?.NativeDevice is BluetoothDevice cachedDevice)
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
                    tempDevice = await BluetoothDevice.FromBluetoothAddressAsync(DeviceAddress).AsTask(ct).ConfigureAwait(false);
                    device = tempDevice;
                    isDeviceAllocated = true;
                    if (device != null && DeviceInfo != null)
                    {
                        DeviceInfo.NativeDevice = device;
                    }
                }

                if (device == null)
                {
                    throw new IOException($"未能发现或连接到蓝牙设备: {DeviceAddressString} (设备可能已关机或超出通信范围)");
                }

                // 前置配对状态校验：Windows 强制要求经典蓝牙必须已完成配对
                if (device.DeviceInformation?.Pairing != null && !device.DeviceInformation.Pairing.IsPaired)
                {
                    throw new InvalidOperationException($"经典蓝牙设备 {DeviceAddressString} [{DeviceName}] 尚未在 Windows 系统中配对。RFCOMM/SPP 串口通信前必须先完成配对 (可调用 client.PairAsync() 或在 Windows 设置中配对)。");
                }

                // 查询指定的 RFCOMM 服务 (严格匹配，禁止盲目选用未知服务)
                var rfcommResult = await device.GetRfcommServicesForIdAsync(RfcommServiceId.FromUuid(ServiceUuid)).AsTask(ct).ConfigureAwait(false);
                RfcommDeviceService? selectedService = null;

                if (rfcommResult.Services != null && rfcommResult.Services.Count > 0)
                {
                    selectedService = rfcommResult.Services[0];
                    // 释放多余的服务实例
                    for (int i = 1; i < rfcommResult.Services.Count; i++)
                    {
                        try { rfcommResult.Services[i].Dispose(); } catch { }
                    }
                }

                if (selectedService == null)
                {
                    throw new IOException($"在经典蓝牙设备 {DeviceAddressString} [{DeviceName}] 上未找到指定的 RFCOMM/SPP 串口服务 ({ServiceUuid})。请确认设备是否开启并支持该串口服务。");
                }

                tempService = selectedService;
                tempSocket = new StreamSocket();
                await tempSocket.ConnectAsync(selectedService.ConnectionHostName, selectedService.ConnectionServiceName).AsTask(ct).ConfigureAwait(false);

                lock (_statusLock)
                {
                    _device = device;
                    _service = tempService;
                    _socket = tempSocket;
                    _inStream = tempSocket.InputStream.AsStreamForRead();
                    _outStream = tempSocket.OutputStream.AsStreamForWrite();
                    _isConnected = true;
                    _device.ConnectionStatusChanged += OnDeviceConnectionStatusChanged;

                    // 所有权已移交字段，清空暂存防止 finally 清理
                    tempDevice = null;
                    tempService = null;
                    tempSocket = null;
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
                // 可靠释放未交接的局部资源，杜绝 COM 与 Socket 句柄泄漏
                if (tempSocket != null)
                {
                    try { tempSocket.Dispose(); } catch { }
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
                // 只要逻辑处于连接状态，或者底层句柄未彻底释放，必须执行断开清理
                if (!_isConnected && _device == null && _socket == null)
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
            if (!IsConnected || _outStream == null)
            {
                throw new InvalidOperationException("蓝牙客户端未连接，无法发送数据。");
            }

            await _outStream.WriteAsync(args.Data, 0, args.Data.Length).ConfigureAwait(false);
            if (args.Options.FlushImmediately)
            {
                await _outStream.FlushAsync().ConfigureAwait(false);
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
            while (!cancellationToken.IsCancellationRequested && _isConnected && _inStream != null)
            {
                byte[] buffer = RentBuffer();
                int bytesRead;

                try
                {
                    bytesRead = await _inStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false);
                }
                catch
                {
                    System.Buffers.ArrayPool<byte>.Shared.Return(buffer);
                    throw;
                }

                if (bytesRead <= 0)
                {
                    System.Buffers.ArrayPool<byte>.Shared.Return(buffer);
                    break;
                }

                var context = CreateReceiveContext(buffer, 0, bytesRead);
                InvokeDataReceivedEventCallback(context);
            }
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
        private void OnDeviceConnectionStatusChanged(BluetoothDevice sender, object args)
        {
            // 修复逻辑短路：只要底层设备已变为 Disconnected，且本客户端此前处于连接中，立刻触发被动断线
            if (sender.ConnectionStatus == BluetoothConnectionStatus.Disconnected && _isConnected)
            {
                Disconnect(new DisconnectArgument(DisconnectionReasonCode.Passive));
            }
        }

        private void CleanupResources()
        {
            _isConnected = false;

            if (_inStream != null)
            {
                try { _inStream.Dispose(); } catch { }
                _inStream = null;
            }

            if (_outStream != null)
            {
                try { _outStream.Dispose(); } catch { }
                _outStream = null;
            }

            if (_socket != null)
            {
                try { _socket.Dispose(); } catch { }
                _socket = null;
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

                if (DeviceInfo != null && ReferenceEquals(DeviceInfo.NativeDevice, _device))
                {
                    DeviceInfo.NativeDevice = null;
                }
                _device = null;
            }
        }
#endif

        private static string GetPlatformNotSupportedMessage()
        {
            return "STTech.BytesIO.Bluetooth 原生经典蓝牙通信目前仅在 Windows 10/11 (windows10.0.19041.0+) 环境下受支持。" +
                   "若项目运行在 Windows 上，请确保消费工程 csproj 指定了带 '-windows10.0.19041.0' 后缀的 TargetFramework (例如 <TargetFramework>net8.0-windows10.0.19041.0</TargetFramework>)。";
        }

        /// <inheritdoc/>
        public override void Dispose()
        {
            Disconnect();
        }
    }
}
