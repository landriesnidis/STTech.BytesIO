# IpcServer Class

## 概述
`IpcServer` 是基于命名管道（Named Pipes）的服务端实现。它允许本地的一个进程作为服务端监听特定的管道路径，并接受来自多个 `IpcClient` 的并发连接。它继承自 `BytesServer<IpcClient>`，提供了完善的连接分发、并发管理和异步监听模型。

## 定义
- **命名空间**: `STTech.BytesIO.Ipc`
- **程序集**: `STTech.BytesIO.dll`

```csharp
public class IpcServer : IpcServer<IpcClient>
```

**继承关系**: `Object` -> `BytesServer<IpcClient>` -> `IpcServer<T>` -> `IpcServer`
**实现接口**: `IBytesServer`, `IIpcServer`, `IDisposable`

## 注解
- **自动扩容**: 每次有一个客户端接入后，服务器会自动创建一个新的 `NamedPipeServerStream` 实例以等待下一个连接，从而实现多实例并行服务。
- **并发控制**: 通过 `MaxConnections` 属性控制管道的最大实例数，默认为系统允许的最大值。
- **安全性**: 开发者可以通过重写或配置 `ClientConnectionAcceptedHandle` 委托，根据接入客户端的标识（如进程 ID、模拟令牌等，取决于具体的管道实现）来决定是否接受连入。

## 构造函数概览
| 名称 | 说明 |
| :--- | :--- |
| `IpcServer()` | 初始化 `IpcServer` 实例，并将接入的流默认封装为 `IpcClient`。 |

## 属性概览
| 名称 | 说明 |
| :--- | :--- |
| `ClientConnectionAcceptedHandle` | 获取或设置一个功能委托，用于判定接收到的管道连接是否合法。 |
| `MaxConnections` | 获取或设置服务端允许的最大并发管道实例数量。 |
| `PipeName` | 获取或设置服务端监听的管道名称。 |
| `EncapsulateStream` | （受保护）获取或设置将原始 `PipeStream` 包装为特定客户端类型的回调。 |

## 方法概览
| 名称 | 说明 |
| :--- | :--- |
| `CloseAsync()` | 异步关闭服务端并释放所有活动的管道客户端。 |
| `Dispose()` | 释放服务端占用的系统资源和令牌源。 |
| `StartAsync()` | 异步开始在该名称管道上的监听任务。 |
| `StopAsync()` | 异步停止接收新连接（暂停监听），但保留现有连接。 |

## 方法详细说明

### StartAsync
初始化异步任务流。一旦启动，服务器将进入 `AcceptLoopAsync` 循环，持续生成新的 `NamedPipeServerStream` 并等待连接请求。
- **签名**: `public override Task StartAsync()`

### CloseAsync
将 `ServerState` 置为 `Closed`，取消内部的所有 CancellationToken，并强制断开所有已连接的 `IpcClient`。
- **签名**: `public override Task CloseAsync()`

## 示例
```csharp
var server = new IpcServer { PipeName = "MyPipe" };
server.ClientConnected += (s, e) => {
    Console.WriteLine($"收到连入，当前在线客户端数: {server.InternalClients.Count}");
};

await server.StartAsync();
```
