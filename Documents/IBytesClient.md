# IBytesClient Interface

## 概述
`IBytesClient` 接口定义了字节流通信客户端的核心功能标准。它规定了建立连接、断开连接以及管理通信参数（如缓冲区大小和状态监控）的基本协议。所有具体的通信客户端（如 TCP、串口、UDP 等）都应实现此接口以保证一致的编程模型。

## 定义
- **命名空间**: `STTech.BytesIO.Core`
- **程序集**: `STTech.BytesIO.dll`

```csharp
public interface IBytesClient : IDisposable
```

## 属性概览
| 名称 | 说明 |
| :--- | :--- |
| `IsConnected` | 获取一个值，该值指示当前客户端是否已建立连接。 |
| `LastMessageReceivedTime` | 获取最后一次接收到消息的时间戳。 |
| `ReceiveBufferSize` | 获取或设置接收缓冲区的大小（字节）。 |
| `SendBufferSize` | 获取或设置发送缓冲区的大小（字节）。 |

## 方法概览
| 名称 | 说明 |
| :--- | :--- |
| `Connect(ConnectArgument)` | 尝试使用指定的参数建立连接。 |
| `Disconnect(DisconnectArgument)` | 尝试根据指定的参数断开当前连接。 |
| `Dispose()` | 释放客户端占用的所有资源。 |

## 属性详细说明

### IsConnected
获取一个值，该值指示当前客户端是否已建立连接。
- **签名**: `bool IsConnected { get; }`
- **返回值**: `bool` 类型。如果已连接则返回 `true`；否则返回 `false`。

### LastMessageReceivedTime
获取最后一次接收到消息的时间戳。
- **签名**: `DateTime LastMessageReceivedTime { get; }`
- **返回值**: `DateTime` 类型，表示最后一次成功接收数据的时间。

### ReceiveBufferSize
获取或设置接收缓冲区的大小（字节）。
- **签名**: `int ReceiveBufferSize { get; set; }`
- **返回值**: `int` 类型，表示接收缓存的字节数。

### SendBufferSize
获取或设置发送缓冲区的大小（字节）。
- **签名**: `int SendBufferSize { get; set; }`
- **返回值**: `int` 类型，表示发送缓存的字节数。

## 方法详细说明

### Connect
尝试使用指定的参数建立连接。
- **签名**: `ConnectResult Connect(ConnectArgument argument)`
- **参数**:
  - `argument`: `ConnectArgument` 类型，包含连接所需的参数（如主机地址、端口、超时等）。
- **返回**: `ConnectResult` 类型，指示连接操作的结果及可能的错误信息。
- **异常**: 无直接抛出异常，错误信息通常包含在 `ConnectResult` 中。

### Disconnect
尝试根据指定的参数断开当前连接。
- **签名**: `DisconnectResult Disconnect(DisconnectArgument argument)`
- **参数**:
  - `argument`: `DisconnectArgument` 类型，包含断开连接时的附加信息（如原因码、异常信息等）。
- **返回**: `DisconnectResult` 类型，指示断开连接操作的结果。
- **异常**: 无。

## 示例
以下示例演示了如何使用实现 `IBytesClient` 的类进行基本操作。
```csharp
// 假设 TcpClient 实现了 IBytesClient
IBytesClient client = new TcpClient("127.0.0.1", 8080);

// 开始连接
ConnectResult result = client.Connect(new ConnectArgument { Timeout = 5000 });

if (result.IsSuccess)
{
    Console.WriteLine("连接成功");
    Console.WriteLine($"当前连接状态: {client.IsConnected}");
}
else
{
    Console.WriteLine($"连接失败: {result.ErrorCode}");
}

// 释放资源
client.Dispose();
```
