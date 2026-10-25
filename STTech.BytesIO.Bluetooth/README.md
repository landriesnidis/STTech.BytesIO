# STTech.BytesIO.Bluetooth

[![License](https://img.shields.io/badge/license-Apache%202.0-blue.svg)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-8.0%20%7C%209.0%20%7C%2010.0-purple.svg)](https://dotnet.microsoft.com/)

`STTech.BytesIO.Bluetooth` 是基于 **STTech.BytesIO** 高性能字节流通信框架扩展的无线蓝牙通信库。严格遵循 BytesIO 统一架构设计哲学，为经典蓝牙 (Classic Bluetooth / RFCOMM / SPP) 与低功耗蓝牙 (BLE / GATT) 提供了统一的 `BytesClient` 编程模型。

---

## 核心特性

1. **统一模型**: `BluetoothClient` (经典蓝牙) 与 `BleClient` (低功耗蓝牙) 均继承自 `BytesClient`，享有协议解包器 (`Unpacker`)、心跳检测、自动重连、发送队列等全套框架能力。
2. **零拷贝与池化**: 基于 `ArrayPool<byte>.Shared` 与 `ReceiveContext` 进行数据收发缓冲，有效降低 GC 停顿与内存分配压力。
3. **双重创建模式**:
   - **发现流 (Manager-based)**: 通过 `BluetoothManager` 进行设备搜索（支持 BLE 广播与经典蓝牙配对扫描），直接由发现的 `BluetoothDeviceInfo` 构建客户端。
   - **直连流 (Direct-address)**: 对于工业固定设备或已知 MAC 的场景，支持直接通过 MAC 地址（字符串或数值）实例化客户端，无需多余扫描。
4. **跨上层协议兼容**: 无缝接入 `ModbusClient<BluetoothClient>` 或 `ModbusClient<BleClient>`，实现 Modbus RTU / ASCII over Bluetooth。
5. **清晰的平台抽象**: 现阶段原生支持 Windows 10/11 (windows10.0.19041.0+)，并使用预编译宏 (`#if WINDOWS`) 对平台进行隔离，多目标框架构建无缝兼容。

---

## 快速上手

### 1. 使用 `BluetoothManager` 扫描并创建客户端

```csharp
using STTech.BytesIO.Bluetooth;
using STTech.BytesIO.Bluetooth.Entity;

// 创建蓝牙管理器
using var manager = new BluetoothManager();

// 监听发现新设备事件
manager.OnDeviceDiscovered += (sender, e) =>
{
    var device = e.Device;
    Console.WriteLine($"发现设备: {device.Name} [{device.AddressString}] RSSI: {device.Rssi}dBm 类型: {device.Type}");

    if (device.Type == BluetoothType.Ble && device.Name.Contains("Sensor"))
    {
        // 停止扫描
        manager.StopScan();

        // 通过管理器创建 BLE 客户端
        var bleClient = manager.CreateBleClient(device);
        bleClient.OnDataReceived += (s, args) =>
        {
            Console.WriteLine($"收到 BLE 数据: {BitConverter.ToString(args.Data.ToArray())}");
        };

        // 发起连接
        var result = bleClient.Connect();
        if (result.IsSuccess)
        {
            Console.WriteLine("BLE 连接成功！");
            bleClient.Send(new byte[] { 0x01, 0x03, 0x00, 0x00, 0x00, 0x02, 0xC4, 0x0B });
        }
    }
};

// 启动扫描 (支持仅 BLE、仅经典蓝牙或 All)
manager.StartScan(new BluetoothScanOptions
{
    Mode = BluetoothScanMode.All,
    Timeout = TimeSpan.FromSeconds(10)
});
```

---

### 2. 通过已知 MAC 地址直连 BLE 客户端

```csharp
using STTech.BytesIO.Bluetooth;

// 直接通过 MAC 地址实例化 BLE 客户端 (支持自定义服务和特征值 UUID，缺省时自动探测可用透传服务)
var bleClient = new BleClient("00:1A:7D:DA:71:13")
{
    // 可选配置：指定特定服务和特征值
    // ServiceUuid = new Guid("6E400001-B5A3-F393-E0A9-E50E24DCCA9E"),
    // WriteCharacteristicUuid = new Guid("6E400002-B5A3-F393-E0A9-E50E24DCCA9E"),
    // NotifyCharacteristicUuid = new Guid("6E400003-B5A3-F393-E0A9-E50E24DCCA9E")
};

bleClient.OnDataReceived += (s, e) =>
{
    Console.WriteLine($"接收到数据: {BitConverter.ToString(e.Data.ToArray())}");
};

// 连接
var res = await bleClient.ConnectAsync();
if (res.IsSuccess)
{
    await bleClient.SendAsync(new byte[] { 0x55, 0xAA, 0x01 });
}
```

---

### 3. 经典蓝牙 (SPP / RFCOMM) 直连

```csharp
using STTech.BytesIO.Bluetooth;

// 经典蓝牙串口客户端 (默认使用 SPP 00001101-0000-1000-8000-00805F9B34FB)
var btClient = new BluetoothClient("98:D3:31:F4:2A:1B");

btClient.OnConnectedSuccessfully += (s, e) =>
{
    Console.WriteLine("经典蓝牙串口建立成功");
};

btClient.OnDataReceived += (s, e) =>
{
    Console.WriteLine($"收到串口数据: {BitConverter.ToString(e.Data.ToArray())}");
};

var connectResult = btClient.Connect();
if (connectResult.IsSuccess)
{
    btClient.Send(System.Text.Encoding.UTF8.GetBytes("AT+VERSION\r\n"));
}
```

---

### 4. 结合 Modbus 协议使用

由于客户端完全实现 `BytesClient`，可直接套用 `STTech.BytesIO.Modbus`：

```csharp
using STTech.BytesIO.Bluetooth;
using STTech.BytesIO.Modbus;

// 创建蓝牙客户端
var btClient = new BluetoothClient("00:1A:7D:DA:71:13");

// 包装为 Modbus 客户端
var modbus = new ModbusClient<BluetoothClient>(btClient, ModbusProtocolFormat.RTU);
modbus.Connect();

// 发送 Modbus 请求
var response = modbus.ReadHoldingRegisters(slaveAddress: 1, startAddress: 0, count: 10);
```

---

## 许可证
本项目采用 [Apache-2.0](LICENSE) 开源协议。
