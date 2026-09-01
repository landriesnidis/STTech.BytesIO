# STTech.BytesIO

[![NuGet](https://img.shields.io/nuget/v/STTech.BytesIO.svg?style=flat-square)](https://www.nuget.org/packages/STTech.BytesIO)
[![License](https://img.shields.io/badge/license-Apache%202.0-blue.svg)](LICENSE)

**STTech.BytesIO** 是一个高性能、轻量级、面向工业自动化与高并发场景的 .NET 字节流通信框架。它为开发者提供了高度统一的编程模型，用于处理 **TCP (Socket/SSL)**、**串口 (SerialPort)** 以及 **进程间通信 (IPC/NamedPipe)** 等底层通信。

---

## 🌟 核心特性

- 🎯 **统一 API 架构**：无论底层是 TCP、串口还是 IPC，客户端统一继承自 `BytesClient`，服务端继承自 `BytesServer`，拥有完全一致的 `ConnectAsync()`、`SendAsync()`、`OnDataReceived` 交互体验。
- ⚡ **零拷贝高性能处理**：深度集成 `System.IO.Pipelines`，极大减少内存分配与 GC 压力，轻松应对高频吞吐场景。
- 📦 **协议解包器 (Unpacker)**：内置支持定长、分隔符、头部带长度字段等多种常用解包器，彻底告别粘包、分包问题。
- 🔄 **工业级可靠性**：内置自动重连机制、双向心跳检测（Heartbeat）、通信监控（Monitor）及发送队列管理。

---

## 📦 安装

通过 NuGet 命令行安装：

```bash
dotnet add package STTech.BytesIO
```

---

## 🚀 快速上手

### 1. TCP 客户端示例

```csharp
using STTech.BytesIO.Tcp;

// 创建 TCP 客户端
var client = new TcpClient
{
    Host = "127.0.0.1",
    Port = 8080
};

// 绑定数据接收事件
client.OnDataReceived += (sender, e) =>
{
    Console.WriteLine($"收到服务端数据: {e.Data.ToHexString()}");
};

// 连接并发送数据
var connectResult = await client.ConnectAsync();
if (connectResult.IsSuccess)
{
    await client.SendAsync(new byte[] { 0x01, 0x02, 0x03 });
}
```

### 2. 串口客户端示例

```csharp
using STTech.BytesIO.Serial;

// 创建串口客户端
var serialClient = new SerialClient
{
    PortName = "COM1",
    BaudRate = 115200
};

serialClient.OnDataReceived += (sender, e) =>
{
    Console.WriteLine($"收到串口数据: {e.Data.ToHexString()}");
};

await serialClient.ConnectAsync();
```

---

## 🧩 官方扩展模块

- **`STTech.BytesIO.Modbus`**：支持 Modbus RTU / TCP / ASCII 协议的高级工业通信库。
- **`STTech.BytesIO.Quic`**：基于 .NET 8+ 原生 QUIC 协议的高性能多路复用通信库。
- **`STTech.BytesIO.P2P`**：基于 Libp2p 实现的去中心化点对点通信模块。
- **`STTech.BytesIO.SerialPortStream`**：基于 `SerialPortStream` 底层实现的增强型串口通信库。

---

## 📄 开源许可证

本项目基于 [Apache-2.0](https://www.apache.org/licenses/LICENSE-2.0) 协议开源。