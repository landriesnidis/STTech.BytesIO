# STTech.BytesIO.SerialPortStream

[![NuGet](https://img.shields.io/nuget/v/STTech.BytesIO.SerialPortStream.svg?style=flat-square)](https://www.nuget.org/packages/STTech.BytesIO.SerialPortStream)
[![License](https://img.shields.io/badge/license-Apache%202.0-blue.svg)](LICENSE)

**STTech.BytesIO.SerialPortStream** 是 `STTech.BytesIO` 的增强型串口扩展库。底层基于成熟稳定的 `SerialPortStream` 实现，为 .NET 开发者在工业自动化场景下提供比原生 `System.IO.Ports.SerialPort` 更高可靠性、更高吞吐量与跨平台一致性的串口通信体验。

---

## 🌟 核心特性

- 🛡 **工业级稳定性**：解决原生串口库在部分虚拟串口、USB 转串口芯片（如 CH340/FTDI/PL2303）以及高频读写下出现的异常断开、数据丢包与假死问题。
- 🚀 **缓冲区优化**：专为高频、大吞吐工业通讯场景优化读写缓冲与事件调度机制。
- 🎯 **无缝集成**：作为 `SerialPortStreamClient` 完美接入 `STTech.BytesIO` 的统一通信接口与解包管道。

---

## 📦 安装

```bash
dotnet add package STTech.BytesIO.SerialPortStream
```

---

## 🚀 快速上手

```csharp
using STTech.BytesIO.SerialPortStream;

// 使用 SerialPortStreamClient 代替默认的 SerialClient
var client = new SerialPortStreamClient
{
    PortName = "COM1",
    BaudRate = 115200
};

client.OnDataReceived += (s, e) =>
{
    Console.WriteLine($"收到数据: {e.Data.ToHexString()}");
};

await client.ConnectAsync();
await client.SendAsync(new byte[] { 0x01, 0x03, 0x00, 0x00, 0x00, 0x01, 0x84, 0x0A });
```

---

## 📄 开源许可证

本项目基于 [Apache-2.0](https://www.apache.org/licenses/LICENSE-2.0) 协议开源。