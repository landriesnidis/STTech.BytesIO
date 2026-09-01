# STTech.BytesIO.Quic

[![NuGet](https://img.shields.io/nuget/v/STTech.BytesIO.Quic.svg?style=flat-square)](https://www.nuget.org/packages/STTech.BytesIO.Quic)
[![License](https://img.shields.io/badge/license-Apache%202.0-blue.svg)](LICENSE)

**STTech.BytesIO.Quic** 是基于原生 QUIC 协议（`System.Net.Quic`）的高性能 .NET 字节流通信库。作为 `STTech.BytesIO` 的扩展模块，它提供了统一的客户端/服务端抽象以及现代传输层的极速体验。

---

## 🌟 核心特性

- ⚡ **现代传输层协议**：基于 UDP 的 QUIC 协议原生具备 TLS 1.3 加密、0-RTT/1-RTT 极速建连、无队头阻塞及连接迁移能力。
- 🎯 **统一 API 设计**：提供 `QuicClient` 与 `QuicServer`，与 TCP/串口/IPC 保持完全一致的调用方式。
- 🔐 **自动证书管理**：内置自签名证书生成器，开发与测试环境下实现开箱即用、零配置启动。

---

## 📦 安装

```bash
dotnet add package STTech.BytesIO.Quic
```

---

## 🚀 快速上手

```csharp
using STTech.BytesIO.Quic;

// 1. 创建 QUIC 客户端
var client = new QuicClient
{
    Host = "127.0.0.1",
    Port = 443
};

client.OnDataReceived += (s, e) =>
{
    Console.WriteLine($"收到 QUIC 数据: {e.Data.ToHexString()}");
};

// 2. 连接并发送
await client.ConnectAsync();
await client.SendAsync(new byte[] { 0x11, 0x22 });
```

---

## 📄 开源许可证

本项目基于 [Apache-2.0](https://www.apache.org/licenses/LICENSE-2.0) 协议开源。