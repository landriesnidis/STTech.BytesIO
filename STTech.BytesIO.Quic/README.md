# STTech.BytesIO.Quic

这是一基于 QUIC 协议的高性能 .NET 字节流通信库，作为 [STTech.BytesIO](https://github.com/landriesnidis/STTech.BytesIO) 的扩展库，提供了统一的编程模型。

## 特性

- **统一架构**：继承自 `BytesClient` 和 `BytesServer`，与 TCP/串口/IPC 具有一致的 API 调用体验。
- **.NET 原生支持**：基于 `System.Net.Quic` 实现，充分利用 .NET 8+ 的性能优化。
- **自动证书管理**：内置自签名证书生成器，支持开发环境下的零配置启动。
- **高性能**：QUIC 协议原生支持加密（TLS 1.3）和多流并发，减少握手延迟并避免队头阻塞。

## 安装

```shell
dotnet add package STTech.BytesIO.Quic
```

## 快速开始

### QuicClient (客户端)

```csharp
using STTech.BytesIO.Quic;

var client = new QuicClient { Host = "127.0.0.1", Port = 443 };
client.OnDataReceived += (s, e) => Console.WriteLine($"收到数据: {e.Data.ToHexString()}");
await client.ConnectAsync();
await client.SendAsync("Hello QUIC".GetBytes());
```

### QuicServer (服务端)

```csharp
using STTech.BytesIO.Quic;

var server = new QuicServer { Port = 443 };
server.ClientConnected += (s, e) => Console.WriteLine($"客户端已连接: {e.Client.ConnectionId}");
await server.StartAsync();
```

## 项目地址

GitHub: [https://github.com/landriesnidis/STTech.BytesIO](https://github.com/landriesnidis/STTech.BytesIO)
