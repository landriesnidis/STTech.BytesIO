# STTech.BytesIO.Quic

[![NuGet](https://img.shields.io/nuget/v/STTech.BytesIO.Quic.svg?style=flat-square)](https://www.nuget.org/packages/STTech.BytesIO.Quic)
[![License](https://img.shields.io/badge/license-Apache%202.0-blue.svg)](LICENSE)

**STTech.BytesIO.Quic** 是基于原生 QUIC 协议（`System.Net.Quic`）的高性能 .NET 字节流通信库。作为 `STTech.BytesIO` 的扩展模块，它提供了统一的客户端/服务端抽象以及现代传输层的极速体验。

---

## 🌟 核心特性

- ⚡ **现代传输层协议**：基于 UDP 的 QUIC 协议原生具备 TLS 1.3 加密、极速握手建连、无传输层队头阻塞（Head-of-Line Blocking）。
- 🔀 **多流复用（Multiplexing）**：单条物理连接支持通过 `CreateStreamClientAsync()` 快速开辟多条独立逻辑流，各流拥有独立的数据收发管道与解包器，流断开互不干扰。
- 🎯 **统一 API 设计**：提供 `QuicClient` 与 `QuicServer`，与 TCP/串口/IPC 保持完全一致的 `BytesClient`/`BytesServer` 编程模型。
- 💬 **全模式通信支持**：
  - **客户端先发言**：标准请求响应模式；
  - **服务端先发言**：支持通过 `InitialPayload` 激活流通知或通过 `ServerInitiatedStreams` 由服务端主动发流下发问候报文，彻底解决 RFC 9000 下的握手死锁陷阱。
- 🌐 **全网络支持**：支持 IPv4、IPv6 及域名解析，原生传递 SNI（Server Name Indication）以完美支持反向代理与云网关。
- 🔐 **完备的安全机制**：严格遵循 TLS 1.3 规范，生产环境默认验证系统 CA 证书，支持通过 `ServerAuthenticationOptionsCallback` 实现 SNI 多证书路由与 mTLS 双向认证。
- 🛡️ **生产级生命周期管理**：物理连接（`QuicConnection`）与通信流（`QuicStream`）生命周期隔离，原子自增连接限流（`MaxConnections`）与优雅停机（`StopAsync` 暂停监听而不中断在线会话，且支持调用 `StartAsync` 无缝恢复）。

---

## 📦 支持的运行平台与原生依赖

本库已内置 `Microsoft.Native.Quic.MsQuic.OpenSSL` 原生依赖包，在 Windows、Linux 与 macOS 上开箱即用：
- **Windows**: Windows 11 / Windows Server 2022 及以上原生支持；Windows 10 / Server 2019 通过内嵌的原生包自动支持。
- **Linux**: 支持常见 Linux 发行版（Debian, Ubuntu, Alpine, RHEL 等）。在部分精简容器镜像中，若未预装 OpenSSL，请通过包管理器安装 `libmsquic` 或 `libssl3`。
- **macOS**: 支持 macOS 14 及以上。

在初始化或连接前，库内会自动预检 `QuicConnection.IsSupported` 与 `QuicListener.IsSupported`。

---

## 🚀 快速上手

### 1. 服务端示例

```csharp
using STTech.BytesIO.Quic;

// 创建 QUIC 服务端
var server = new QuicServer
{
    Host = "127.0.0.1",
    Port = 5000,
    MaxConnections = 1000 // 原子计数防突发穿透
};

// 监听客户端连接
server.ClientConnected += (s, e) =>
{
    Console.WriteLine($"客户端已连接: {e.Client.Host}:{e.Client.Port}");

    e.Client.OnDataReceived += (cs, ce) =>
    {
        Console.WriteLine($"收到客户端消息: {Encoding.UTF8.GetString(ce.Data.ToArray())}");
        // 回发响应
        _ = e.Client.SendAsync(Encoding.UTF8.GetBytes("Echo: " + Encoding.UTF8.GetString(ce.Data.ToArray())));
    };
};

// 启动服务（未配置 ServerCertificate 时将自动使用包含 SAN 的自签名测试证书）
await server.StartAsync();
```

### 2. 客户端示例

```csharp
using STTech.BytesIO.Quic;

// 创建 QUIC 客户端
var client = new QuicClient
{
    Host = "localhost", // 支持域名及 IPv4/IPv6
    Port = 5000,
    AllowUntrustedCertificates = true // 开发/自签证书测试环境允许不受信任证书，生产环境设为 false
};

client.OnDataReceived += (s, e) =>
{
    Console.WriteLine($"收到服务端回发: {Encoding.UTF8.GetString(e.Data.ToArray())}");
};

// 建立连接
var connectResult = await client.ConnectAsync();
if (connectResult.IsSuccess)
{
    // 发送数据
    await client.SendAsync(Encoding.UTF8.GetBytes("Hello QUIC Server!"));
}
```

### 3. 多流并发复用（Multiplexing）

```csharp
// 在同一条物理 QUIC 连接上并发开辟第二条独立逻辑流
var streamClient2 = await client.CreateStreamClientAsync();

streamClient2.OnDataReceived += (s, e) =>
{
    Console.WriteLine($"流2收到独立数据: {e.Data.ToHexString()}");
};

await streamClient2.SendAsync(new byte[] { 0x01, 0x02 });
```

---

## 📄 开源许可证

本项目基于 [Apache-2.0](https://www.apache.org/licenses/LICENSE-2.0) 协议开源。
