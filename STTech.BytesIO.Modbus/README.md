# STTech.BytesIO.Modbus

[![NuGet](https://img.shields.io/nuget/v/STTech.BytesIO.Modbus.svg?style=flat-square)](https://www.nuget.org/packages/STTech.BytesIO.Modbus)
[![License](https://img.shields.io/badge/license-Apache%202.0-blue.svg)](LICENSE)

**STTech.BytesIO.Modbus** 是基于 `STTech.BytesIO` 核心架构实现的工业级 Modbus 协议通信库。支持 **Modbus TCP**、**Modbus RTU** 与 **Modbus ASCII** 协议，提供强类型、直观易用的 API 来操作标准 PLC 与工业传感器寄存器。

---

## 🌟 核心特性

- 🎯 **主流协议全覆盖**：无缝支持 Modbus RTU（串口）、Modbus TCP（网络以太网）及 Modbus ASCII。
- 📋 **标准功能码全面支持**：
  - `0x01` 读线圈 (ReadCoilRegister)
  - `0x02` 读离散输入 (ReadDiscreteInputRegister)
  - `0x03` 读保持寄存器 (ReadHoldingRegister)
  - `0x04` 读输入寄存器 (ReadInputRegister)
  - `0x05` 写单个线圈 (WriteSingleCoilRegister)
  - `0x06` 写单个保持寄存器 (WriteSingleHoldingRegister)
  - `0x0F` 写多个线圈 (WriteMultipleCoilRegisters)
  - `0x10` 写多个保持寄存器 (WriteMultipleHoldingRegisters)
- ⚡ **稳健的底层通信**：复用 `BytesIO` 强大的自动重连、心跳保活、零拷贝缓冲与 CRC/LRC 自动校验。
- 🧪 **虚拟设备支持**：内置 `VirtualModbusDevice`，方便开发者进行离线联调与单元测试。

---

## 📦 安装

```bash
dotnet add package STTech.BytesIO.Modbus
```

---

## 🚀 快速上手

### Modbus TCP 客户端读写示例

```csharp
using System;
using System.Threading.Tasks;
using STTech.BytesIO.Modbus;

// 1. 创建 Modbus TCP 客户端
var client = new ModbusTcpClient
{
    Host = "127.0.0.1",
    Port = 502
};

// 2. 异步连接
var connectResult = await client.ConnectAsync();
if (connectResult.IsSuccess)
{
    // 3. 读取站号为 1 的保持寄存器（从地址 0 开始，读取 10 个寄存器）
    var reply = await client.ReadHoldingRegistersAsync(slaveId: 1, startAddress: 0, count: 10);
    if (reply.IsSuccess)
    {
        short[] values = reply.Data;
        Console.WriteLine($"读取成功，寄存器0值: {values[0]}");
    }

    // 4. 向站号为 1 的保持寄存器写入单个值
    await client.WriteSingleHoldingRegisterAsync(slaveId: 1, registerAddress: 0, value: 1234);
}
```

---

## 📄 开源许可证

本项目基于 [Apache-2.0](https://www.apache.org/licenses/LICENSE-2.0) 协议开源。