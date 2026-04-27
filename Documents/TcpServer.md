# TcpServer Class

## 概述
`TcpServer` 是一个高性能、支持多连接及 SSL/TLS 加密的 TCP 服务端实现。它继承自 `BytesServer<TcpClient>`，为开发者提供了开箱即用的 TCP 网络监听能力。该类处理了复杂的底层 Socket 接受逻辑、连接限流、以及客户端实例的自动化封装。

## 定义
- **命名空间**: `STTech.BytesIO.Tcp`
- **程序集**: `STTech.BytesIO.dll`

```csharp
public partial class TcpServer : TcpServer<TcpClient>
```

**继承关系**: `Object` -> `BytesServer<TcpClient>` -> `TcpServer<T>` -> `TcpServer`

## 注解
- **连接拒绝策略**: 当服务器状态为 `Paused` 或达到 `MaxConnections` 限制时，服务器将自动拒绝并断开新连入的 Socket。
- **SSL 支持**: 支持在服务端配置证书，一旦启用 `UseSsl`，服务器将在接受新连接后自动异步执行 SSL 握手。
- **泛型扩展**: 虽然提供了默认的 `TcpServer`（使用 `TcpClient`），由于采用了泛型基类 `TcpServer<T>`，开发者可以轻松创建使用自定义客户端类型的服务端。

## 构造函数概览
| 名称 | 说明 |
| :--- | :--- |
| `TcpServer()` | 初始化 `TcpServer` 类的新实例，并默认将 Socket 封装为 `TcpClient`。 |

## 属性概览
| 名称 | 说明 |
| :--- | :--- |
| `Backlog` | 获取或设置挂起连接队列的最大长度。默认为 10。 |
| `Certificate` | 获取或设置服务端用于 SSL 身份验证的证书。 |
| `Host` | 获取或设置服务端监听的网络地址（如 `0.0.0.0` 或 `127.0.0.1`）。 |
| `Port` | 获取或设置服务端监听的端口号。 |
| `ServerCertificateName` | 获取或设置服务器证书的显示名称。 |
| `SslProtocol` | 获取或设置支持的 SSL/TLS 协议版本。 |
| `UseSsl` | 获取或设置一个值，指示是否启用 SSL/TLS 通信。 |
| `ClientConnectionAcceptedHandle` | 获取或设置一个委托，用于判定是否接受特定的客户端连接。 |

## 方法概览
| 名称 | 说明 |
| :--- | :--- |
| `CloseAsync()` | 异步关闭服务器并释放所有客户端连接。 |
| `StartAsync()` | 异步启动网络监听任务。 |
| `StopAsync()` | 异步停止网络监听（不影响已连接的客户端）。 |
| `EncapsulateSocket` | （受保护）获取或设置将 Socket 包装为客户端实例的处理程序。 |

## 方法详细说明

### StartAsync
根据配置的 `Host` 和 `Port` 绑定并监听 Socket。启动后会进入后台接收循环 `AcceptLoopAsync`。
- **签名**: `public override Task StartAsync()`
- **备注**: 如果已处于监听状态，调用此方法将直接返回。

### StopAsync (暂停)
关闭当前的监听 Socket，并将服务器状态置为 `Paused`。已连接的客户端仍然可以继续通信，但不再接受新连接。
- **签名**: `public override Task StopAsync()`

## 示例
### 1. 基础启动
```csharp
var server = new TcpServer { Host = "0.0.0.0", Port = 6000 };
server.ClientConnected += (s, e) => {
    Console.WriteLine($"客户端连入: {e.Client.RemoteEndPoint}");
};
await server.StartAsync();
```

### 2. SSL/TLS 加密服务端
```csharp
var server = new TcpServer { Port = 8888, UseSsl = true };
server.Certificate = new X509Certificate2("server.pfx", "password");
await server.StartAsync();
```
