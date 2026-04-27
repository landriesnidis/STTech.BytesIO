# TcpClient Class

## 概述
`TcpClient` 是基于 TCP 协议的通信客户端实现。它继承自 `BytesClient`，提供了标准的 TCP 连接管理、异步数据收发以及高度集成的 SSL/TLS 加密通信功能。该类支持同步与异步操作模式，并内置了针对 TCP 连接生命周期的完整处理逻辑，包括自动重连支持（配合插件使用）和优雅的资源释放。

## 定义
- **命名空间**: `STTech.BytesIO.Tcp`
- **程序集**: `STTech.BytesIO.dll`

```csharp
public partial class TcpClient : BytesClient, ITcpClient, ITcpSSL
```

**继承关系**: `Object` -> `BytesClient` -> `TcpClient`
**实现接口**: `IBytesClient`, `ITcpClient`, `ITcpSSL`, `IDisposable`

## 注解
- **性能**: 内部利用 `System.IO.Pipelines` 思路及 `ArrayPool` 进行接收缓存管理，在高吞吐量的 TCP 通信中表现优异。
- **安全性**: 通过 `UseSsl` 开关可一键启用 TLS 安全传输。支持自定义证书验证回调和本地证书选择，兼容绝大多数工业级 SSL 连接场景。
- **线程安全性**: `Connect` 和 `Disconnect` 操作内部持有状态锁，确保在多线程环境下并发调用时不会导致 Socket 状态混乱。

## 构造函数概览
| 名称 | 说明 |
| :--- | :--- |
| `TcpClient()` | 使用默认设置初始化一个新的 `TcpClient` 实例。 |
| `TcpClient(Socket)` | 使用现有的 `Socket` 对象初始化 `TcpClient` 实例。 |

## 属性概览
| 名称 | 说明 |
| :--- | :--- |
| `Certificate` | 获取或设置用于 SSL 身份验证的证书。 |
| `Host` | 获取或设置远程主机的 IP 地址或域名。默认为 `127.0.0.1`。 |
| `IsConnected` | 获取一个值，指示 Socket 是否已建立连接并处于活动状态。 |
| `LocalEndPoint` | 获取本地终端节点（IP 和 端口）。 |
| `Port` | 获取或设置远程主机的端口号。默认为 `8086`。 |
| `RemoteEndPoint` | 获取远程主机的终端节点。 |
| `ServerCertificateName` | 获取或设置服务端证书的名称。 |
| `SslProtocol` | 获取或设置适用的 TLS 协议版本。默认为 `Tls12`。 |
| `SslStream` | 获取当前的 SSL 通信流（仅在 `UseSsl` 为 `true` 时有效）。 |
| `UseSsl` | 获取或设置一个值，指示是否启用 SSL/TLS 加密通信。 |
| `InnerClient` | （受保护）获取内部使用的 `Socket` 实例。 |

## 方法概览
| 名称 | 说明 |
| :--- | :--- |
| `Connect(ConnectArgument)` | 建立 TCP 通信连接。 |
| `Disconnect(DisconnectArgument)` | 断开 TCP 通信连接。 |
| `GetInnerClient()` | 获取底层的 `Socket` 对象。 |
| `InitializeSslStream()` | 手动初始化 SSL 通信流。 |
| `LocalCertificateSelectionCallback(...)` | 用于本地证书选择的默认回调实现。 |
| `PerformTlsVerifySuccessfully(...)` | 手动触发 TLS 验证成功事件。 |
| `ReceiveDataCompletedHandle()` | （受保护）重写父类的数据接收完成处理逻辑。 |
| `ReceiveDataHandleAsync(...)` | （受保护）核心异步接收循环实现。 |
| `RemoteCertificateValidateCallback(...)` | （受保护）用于远端证书验证的默认回调逻辑。 |
| `SendHandlerAsync(...)` | （受保护）核心异步发送底层实现。 |

## 事件概览
| 名称 | 说明 |
| :--- | :--- |
| `OnTlsVerifySuccessfully` | 在 SSL/TLS 手握手成功且验证通过时触发。 |

## 方法详细说明

### Connect
执行 TCP 连接逻辑。如果启用了 SSL，将在底层 Socket 连接成功后自动执行 TLS 握手。
- **签名**: `public override ConnectResult Connect(ConnectArgument argument = null)`
- **返回值**: `ConnectResult`。如果连接中途超时或 SSL 握手失败，会返回对应的错误码。

### InitializeSslStream
该方法会基于当前的 `InnerClient` 创建 `SslStream` 并根据配置的证书进行身份验证。
- **签名**: `public void InitializeSslStream()`
- **异常**: 若证书非法或握手被拒绝，可能抛出 `AuthenticationException`。

### ReceiveDataHandleAsync (Protected)
实现了基于 `Stream.ReadAsync` 的生产-消费模型。该方法会不断租借池化缓冲区，直到连接断开。
- **签名**: `protected override async Task ReceiveDataHandleAsync(CancellationToken cancellationToken)`

## 示例
### 1. 基础连接
```csharp
var client = new TcpClient { Host = "192.168.1.10", Port = 502 };
var result = await client.ConnectAsync();
if(result.IsSuccess) 
{
    await client.SendAsync(new byte[] { 0x00, 0x01 });
}
```

### 2. TLS 安全连接
```csharp
var client = new TcpClient { Host = "myserver.com", Port = 443, UseSsl = true };
client.ServerCertificateName = "myserver.com";
client.OnTlsVerifySuccessfully += (s, e) => Console.WriteLine("SSL连接安全");
await client.ConnectAsync();
```
