# STTech.BytesIO.Bluetooth

[![License](https://img.shields.io/badge/license-Apache%202.0-blue.svg)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-8.0%20%7C%209.0%20%7C%2010.0-purple.svg)](https://dotnet.microsoft.com/)

`STTech.BytesIO.Bluetooth` 是基于 **STTech.BytesIO** 高性能字节流通信框架扩展的工业级无线蓝牙通信库。严格遵循 BytesIO 统一架构设计哲学，为经典蓝牙 (Classic Bluetooth / RFCOMM / SPP) 与低功耗蓝牙 (BLE / GATT) 提供了统一的 `BytesClient` 编程模型。

> [!IMPORTANT]
> **目标框架 (TargetFramework) 配置指南**：
> 本库原生支持 Windows 10/11 的 WinRT 蓝牙协议栈。若您的项目运行在 Windows 上，请确保消费工程 `.csproj` 中指定带操作系统版本的 TargetFramework，例如：
> ```xml
> <TargetFramework>net8.0-windows10.0.19041.0</TargetFramework>
> <!-- 或 net9.0-windows10.0.19041.0 / net10.0-windows10.0.19041.0 -->
> ```
> 避免使用纯 `net8.0`（未指定操作系统平台），否则 NuGet 资产解析器会匹配到跨平台空桩程序集并抛出不支持异常。

---

## 核心特性

1. **统一模型**: `BluetoothClient` (经典蓝牙) 与 `BleClient` (低功耗蓝牙) 均继承自 `BytesClient`，享有协议解包器 (`Unpacker`)、心跳检测、自动重连、发送队列等全套框架能力。面向接口解耦，完全满足控制反转与依赖倒置设计原则 (`IBluetoothClient`、`IBleClient`、`IBluetoothManager`)。
2. **自适应 MTU 与自动分包切片**:
   - `BleClient` 自动协商底层 ATT MTU (`GattSession.MaxPduSize`)；
   - 发送任意长度长数据（如 Modbus 报文、JSON 固件帧）时，内置发送泵自适应分包切片发送，免除物理 ATT MTU 限制导致的协议报错与通信中断。
3. **特征值写入自适应协商 (`BleWriteOption.Auto`)**:
   - 默认启用 `Auto` 模式，动态探测硬件特征值权限（无响应写入 / 有响应写入），优先采用无响应写入以获得高吞吐率，若硬件仅开放带响应写入则自动降级，根除 `ArgumentException` 硬件写入崩溃。
4. **工业级连接保活与资源安全**:
   - 自动启用 `GattSession.MaintainConnection = true`，防止 Windows 电源管理在空闲（20~30s）时切断 BLE 链路；
   - 断开连接时智能识别链路状态，仅在物理连接活跃时注销 CCCD，杜绝被动断线时的无效等待；
   - 采用严格的异常全生命周期所有权交接机制，连接失败或超时均即时 Dispose 原生 COM 资源，防止物理句柄泄漏与设备忙死锁。
5. **经典蓝牙主动配对与 PIN 码支持**:
   - 支持通过 `client.PairAsync("1234")` 进行交互式或无感 PIN 码配对，免去人工切换 Windows 桌面的繁琐流程。
6. **双重创建模式**:
   - **发现流 (Manager-based)**: 通过 `BluetoothManager` 进行设备搜索（支持 BLE 广播与经典蓝牙空中未配对及已配对扫描，内置原子更新与扫描响应合并），直接由发现的 `BluetoothDeviceInfo` 构建客户端。
   - **直连流 (Direct-address)**: 对于工业固定设备或已知 MAC 的场景，支持直接通过 MAC 地址实例化客户端，免去多余扫描。
7. **跨上层协议兼容**: 无缝接入 `ModbusClient<BluetoothClient>` 或 `ModbusClient<BleClient>`，实现 Modbus RTU / ASCII over Bluetooth。

---

## 快速上手

### 1. 使用 `BluetoothManager` 扫描并创建客户端

> [!TIP]
> **扫描提示**：BLE 设备往往将设备名放在随后的扫描响应包（Scan Response）中，建议监听 `OnDeviceDiscovered` 与 `OnDeviceUpdated`。

```csharp
using STTech.BytesIO.Bluetooth;
using STTech.BytesIO.Bluetooth.Entity;

// 创建蓝牙管理器
using var manager = new BluetoothManager();

void HandleDevice(BluetoothDeviceInfo device)
{
    Console.WriteLine($"发现设备: {device.Name} [{device.AddressString}] RSSI: {device.Rssi}dBm 类型: {device.Type}");

    // 匹配目标设备（名称或 MAC 地址）
    if (device.Type == BluetoothType.Ble && (device.Name.Contains("Sensor") || device.AddressString == "00:1A:7D:DA:71:13"))
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
            // 可直接发送超长数据，内部自动分包
            bleClient.Send(new byte[] { 0x01, 0x03, 0x00, 0x00, 0x00, 0x02, 0xC4, 0x0B });
        }
    }
}

// 同时监听初次发现与扫描响应更新
manager.OnDeviceDiscovered += (sender, e) => HandleDevice(e.Device);
manager.OnDeviceUpdated += (sender, e) => HandleDevice(e.Device);

// 启动扫描 (支持通过 ServiceUuids 进行硬件级 UUID 过滤)
manager.StartScan(new BluetoothScanOptions
{
    Mode = BluetoothScanMode.Ble,
    RssiFilter = -80,
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
    // 可选配置：指定特定服务和特征值 UUID (未指定时会自动跳过 SIG 保留服务探测工业透传服务)
    // ServiceUuid = new Guid("6E400001-B5A3-F393-E0A9-E50E24DCCA9E"),
    // WriteCharacteristicUuid = new Guid("6E400002-B5A3-F393-E0A9-E50E24DCCA9E"),
    // NotifyCharacteristicUuid = new Guid("6E400003-B5A3-F393-E0A9-E50E24DCCA9E")

    // 可选配置：手动限定单包大小 (字节，默认 0 表示根据 MTU 协商动态计算有效载荷)
    // MaxPayloadSize = 244,
    // WriteWithoutResponseIntervalMs = 2 // 连续无响应分包写入时的微延迟 (ms)
};

bleClient.OnDataReceived += (s, e) =>
{
    Console.WriteLine($"接收到数据: {BitConverter.ToString(e.Data.ToArray())}");
};

// 连接
var res = await bleClient.ConnectAsync();
if (res.IsSuccess)
{
    // 发送任意长度数据，内部自动安全切片
    await bleClient.SendAsync(new byte[] { 0x55, 0xAA, 0x01, 0x02, 0x03 });
}
```

---

### 3. 经典蓝牙 (SPP / RFCOMM) 直连

> [!NOTE]
> 经典蓝牙 (SPP) 建立串口通信前，请确保该蓝牙设备已在 Windows“蓝牙和其他设备”设置中**已完成配对**。

```csharp
using STTech.BytesIO.Bluetooth;

// 经典蓝牙串口客户端 (默认使用标准 SPP 00001101-0000-1000-8000-00805F9B34FB)
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
