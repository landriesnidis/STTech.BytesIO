# STTech.BytesIO.Modbus

**STTech.BytesIO.Modbus** 是一个基于 `STTech.BytesIO` 核心架构实现的全方位 Modbus 协议库。由于 Modbus 是基于帧协议的通信标准，它在 `BytesIO` 的基础上进行了封装与抽象，为开发者提供了更高级别、统一且易用的接口来处理 Modbus RTU、TCP 及 ASCII 通信。

---

## ✨ 特性 (Features)

- **全面协议支持**: 无缝覆盖工业界主流的 **Modbus RTU**, **Modbus TCP**, 和 **Modbus ASCII**。
- **标准功能码全支持**: 
  - `0x01` 读线圈 (ReadCoilRegister)
  - `0x02` 读离散输入 (ReadDiscreteInputRegister)
  - `0x03` 读保持寄存器 (ReadHoldingRegister)
  - `0x04` 读输入寄存器 (ReadInputRegister)
  - `0x05` 写单个线圈 (WriteSingleCoilRegister)
  - `0x06` 写单个保持寄存器 (WriteSingleHoldingRegister)
  - `0x0F` 写多个线圈 (WriteMultipleCoilRegisters)
  - `0x10` 写多个保持寄存器 (WriteMultipleHoldingRegisters)
- **灵活的传输层底座**: 可以根据需要无缝切换集成的 TCP 客户端 (`TcpClient`) 或串口客户端 (`SerialClient`) 作为底层传输通道，复用 `BytesIO` 强大的断线重连、零拷贝机制。
- **高性能解包与校验**: 内置 Modbus 专用解包器 (`ModbusUnpacker`)，自动进行 CRC/LRC 校验与粘包/半包处理，确保数据帧的完整性与准确性。
- **虚拟设备支持**: 包含 `VirtualModbusDevice`，方便开发者进行本地联调与模拟测试。

---

## 🛠️ 安装 (Installation)

通过 NuGet 包管理器安装：

```bash
dotnet add package STTech.BytesIO.Modbus
```

---

## 📖 快速上手 (Quick Start)

### 1. Modbus TCP 客户端使用示例

使用 `ModbusTcpClient` 进行基于网络的通信：

```csharp
using STTech.BytesIO.Modbus;

// 1. 创建 Modbus TCP 客户端
var client = new ModbusTcpClient { Host = "127.0.0.1", Port = 502 };

// 2. 异步连接
var connectResult = await client.ConnectAsync();
if (connectResult.IsSuccess) 
{
    // 3. 读取保持寄存器 (功能码 0x03)
    // 参数: 寄存器起始地址 0, 读取长度 10
    var response = await client.ReadHoldingRegisterAsync(0, 10);
    
    if (response.IsSuccess) 
    {
        // 成功获取到设备返回的原始字节数据
        byte[] rawData = response.Data; 
        Console.WriteLine($"读取成功: {BitConverter.ToString(rawData)}");
    }
    else 
    {
        Console.WriteLine($"读取失败: 错误码 {response.ErrorCode}");
    }
}
```

### 2. Modbus RTU (串口) 客户端使用示例

使用 `ModbusSerialClient` 进行基于串口的通信：

```csharp
using STTech.BytesIO.Modbus;

// 1. 创建 Modbus RTU 串口客户端
var serialClient = new ModbusSerialClient { PortName = "COM1", BaudRate = 9600 };

// 2. 异步连接
await serialClient.ConnectAsync();

// 3. 写入单个线圈 (功能码 0x05)
// 参数: 站号(如果支持多设备的话，可通过封装的方法或传入), 寄存器地址 10, 值 true
var writeResponse = await serialClient.WriteSingleCoilRegisterAsync(10, true);

if (writeResponse.IsSuccess) {
    Console.WriteLine("线圈写入成功！");
}
```

### 3. Modbus 服务端 (虚拟设备) 示例

```csharp
using STTech.BytesIO.Modbus;

// 创建 Modbus TCP 服务端并启动
var server = new ModbusTcpServer { Port = 502 };
server.Start();

Console.WriteLine("Modbus TCP 服务端已启动...");
```

---

## 🔗 返回主页

返回主项目文档: [STTech.BytesIO 主页](../../README.md)

---
*Powered by KarFans Industrial Co., LTD.*
