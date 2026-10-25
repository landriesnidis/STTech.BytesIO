using STTech.BytesIO.Bluetooth.Common;
using STTech.BytesIO.Bluetooth.Entity;
using STTech.BytesIO.Core;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

#if WINDOWS
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Rfcomm;
using Windows.Networking.Sockets;
#endif

namespace STTech.BytesIO.Bluetooth
{
    /// <summary>
    /// 经典蓝牙通信客户端 (基于 RFCOMM / SPP 串行端口规范)
    /// </summary>
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

            try
            {
                using var timeoutCts = argument.Timeout > 0 ? new CancellationTokenSource(argument.Timeout) : null;
                var ct = timeoutCts?.Token ?? CancellationToken.None;

                // 获取蓝牙设备对象
                var device = DeviceInfo?.NativeDevice as BluetoothDevice;
                if (device == null)
                {
                    device = await BluetoothDevice.FromBluetoothAddressAsync(DeviceAddress).AsTask(ct).ConfigureAwait(false);
                }

                if (device == null)
                {
                    throw new IOException($"未能发现或连接到蓝牙设备: {DeviceAddressString} (设备可能已关机或超出距离)");
                }

                // 查询 RFCOMM 服务
                var rfcommResult = await device.GetRfcommServicesForIdAsync(RfcommServiceId.FromUuid(ServiceUuid)).AsTask(ct).ConfigureAwait(false);
                RfcommDeviceService? selectedService = null;

                if (rfcommResult.Services != null && rfcommResult.Services.Count > 0)
                {
                    selectedService = rfcommResult.Services[0];
                }
                else
                {
                    // 容错: 遍历设备上的所有 RFCOMM 服务，选用首个可用服务
                    var allServices = await device.GetRfcommServicesAsync().AsTask(ct).ConfigureAwait(false);
                    if (allServices.Services != null && allServices.Services.Count > 0)
                    {
                        selectedService = allServices.Services[0];
                    }
                }

                if (selectedService == null)
                {
                    throw new IOException($"在蓝牙设备 {DeviceAddressString} 上未找到可用的 RFCOMM/SPP 串口服务。");
                }

                var socket = new StreamSocket();
                await socket.ConnectAsync(selectedService.ConnectionHostName, selectedService.ConnectionServiceName).AsTask(ct).ConfigureAwait(false);

                lock (_statusLock)
                {
                    _device = device;
                    _service = selectedService;
                    _socket = socket;
                    _inStream = socket.InputStream.AsStreamForRead();
                    _outStream = socket.OutputStream.AsStreamForWrite();
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
            var ex = new PlatformNotSupportedException("STTech.BytesIO.Bluetooth 当前仅在 Windows 10/11 (windows10.0.19041.0+) 平台上支持原生经典蓝牙。");
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
            await Task.FromException(new PlatformNotSupportedException("STTech.BytesIO.Bluetooth 当前仅在 Windows 平台上可用。")).ConfigureAwait(false);
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
            if (sender.ConnectionStatus == BluetoothConnectionStatus.Disconnected && IsConnected)
            {
                Disconnect(new DisconnectArgument(DisconnectionReasonCode.Passive));
            }
        }

        private void CleanupResources()
        {
            _isConnected = false;

            if (_device != null)
            {
                try { _device.ConnectionStatusChanged -= OnDeviceConnectionStatusChanged; } catch { }
                try { _device.Dispose(); } catch { }
                _device = null;
            }

            if (_service != null)
            {
                try { _service.Dispose(); } catch { }
                _service = null;
            }

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
        }
#endif

        /// <inheritdoc/>
        public override void Dispose()
        {
            Disconnect();
        }
    }
}
