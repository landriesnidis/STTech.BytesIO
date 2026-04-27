# IpcClient Class

## 概述
`IpcClient` 提供了基于命名管道（Named Pipes）的本地进程间通信（IPC）能力。它继承自 `BytesClient`，允许位于同一台机器（或者是局域网内支持命名管道的机器）上的不同进程之间进行高效、稳定的字节流传输。该类封装了 `NamedPipeClientStream` 的驱动逻辑，使其符合 `STTech.BytesIO` 的统一通信接口。

## 定义
- **命名空间**: `STTech.BytesIO.Ipc`
- **程序集**: `STTech.BytesIO.dll`

```csharp
public class IpcClient : BytesClient, IIpcClient
```

**继承关系**: `Object` -> `BytesClient` -> `IpcClient`
**实现接口**: `IBytesClient`, `IIpcClient`, `IDisposable`

## 注解
- **高性能**: 相比于回环 TCP (127.0.0.1)，命名管道在 Windows 系统上通常具有更低的延迟和更少的协议开销，是进程间数据交换的首选方案。
- **异步支持**: 通过 `ConnectAsync` 和内部的异步读写任务，确保在高并发 IPC 场景下不会阻塞调用者的主线程。
- **全双工**: 内部使用的管道流配置为 `PipeDirection.InOut`，支持同步的双向通信。

## 构造函数概览
| 名称 | 说明 |
| :--- | :--- |
| `IpcClient()` | 初始化一个新的 `IpcClient` 实例。 |
| `IpcClient(PipeStream)` | 使用现有的 `PipeStream` 对象初始化客户端（通常用于服务端接受连接后的客户端包装）。 |

## 属性概览
| 名称 | 说明 |
| :--- | :--- |
| `IsConnected` | 获取一个值，指示命名管道当前是否已连接。 |
| `PipeName` | 获取或设置管道名称。默认为 `STTech.BytesIO.Ipc.Default`。 |
| `ServerName` | 获取或设置服务端名称。默认为 `.`（表示本地计算机）。 |
| `InnerClient` | （受保护）获取内部使用的 `PipeStream` 实例。 |

## 方法概览
| 名称 | 说明 |
| :--- | :--- |
| `Connect(ConnectArgument)` | 尝试同步建立管道连接。 |
| `ConnectAsync(ConnectArgument)` | 尝试异步建立管道连接。 |
| `Disconnect(DisconnectArgument)` | 断开当前的管道连接。 |
| `ReceiveDataCompletedHandle()` | （受保护）重写数据接收任务完成时的清理逻辑。 |
| `ReceiveDataHandleAsync(...)` | （受保护）异步读取管道数据流的循环实现。 |
| `SendHandlerAsync(...)` | （受保护）将字节数据异步写入命名管道。 |

## 方法详细说明

### Connect
执行到服务端的管道连线。如果服务端尚未启动，此方法将根据 `ConnectArgument.Timeout` 进行阻塞等待。
- **签名**: `public override ConnectResult Connect(ConnectArgument argument = null)`

### SendHandlerAsync (Protected)
重写基类的发送处理器，利用 `PipeStream.WriteAsync` 进行无损数据传输。
- **签名**: `protected override async Task SendHandlerAsync(SendArgs args)`

## 示例
```csharp
var client = new IpcClient { PipeName = "MyPipe" };

// 异步连接
var result = await client.ConnectAsync(new ConnectArgument { Timeout = 3000 });

if(result.IsSuccess)
{
    // 发送消息
    await client.SendAsync(Encoding.UTF8.GetBytes("Hello Server"));
}
```
