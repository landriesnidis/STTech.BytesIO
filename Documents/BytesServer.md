# BytesServer Class

## 概述
`BytesServer` 是一个抽象基类，用于开发高性能的字节流通信服务端。它提供了一个通用的服务器架构，涵盖了状态管理、客户端生命周期追踪、并发连接限制以及标准化的启动/停止/关闭流程。通过泛型设计，它能够适配各种类型的 `BytesClient` 子类。

## 定义
- **命名空间**: `STTech.BytesIO.Core`
- **程序集**: `STTech.BytesIO.dll`

```csharp
public abstract class BytesServer<TClient> : IBytesServer where TClient : BytesClient
```

**继承关系**: `Object` -> `BytesServer<TClient>`
**实现接口**: `IBytesServer`, `IDisposable`

## 注解
- **连接管理**: 内部使用 `ConcurrentDictionary` 管理当前在线的所有客户端，确保在并发接入和断开时的线程安全。
- **状态机**: 维护一套完整的 `ServerState`（Closed, Listening, Paused），使外部能够精确监控服务器运行阶段。
- **异步设计**: 核心控制方法均采用 `Task` 异步模型，避免阻塞 UI 或主服务线程。

## 属性概览
| 名称 | 说明 |
| :--- | :--- |
| `Clients` | 获取当前所有已连接客户端的快照数组。 |
| `IsListening` | 获取一个值，指示服务器是否正在监听新连接。 |
| `IsPaused` | 获取一个值，指示服务器是否已暂停监听。 |
| `IsRunning` | 获取一个值，指示服务器是否处于非关闭状态。 |
| `MaxConnections` | 获取或设置服务器允许的最大并发连接数（0 为无限制）。 |
| `State` | 获取服务器当前的运行状态。 |
| `InternalClients` | （受保护）内部管理的客户端字典。 |
| `ServerStateLocker` | （受保护）用于同步服务器状态更改的锁对象。 |

## 方法概览
| 名称 | 说明 |
| :--- | :--- |
| `CloseAsync()` | （抽象）异步关闭预览。释放所有活跃连接并停止监听。 |
| `GetClients()` | 获取当前在线客户端的迭代器。 |
| `RaiseExceptionOccurs(Exception)` | 手动触发异常通知事件。 |
| `StartAsync()` | （抽象）异步启动服务器监听功能。 |
| `StopAsync()` | （抽象）异步停止新连接的监听，但保持现有连接不中断。 |
| `OnClientConnected(TClient)` | （受保护）当新客户端成功建立通信时的逻辑处理。 |
| `OnClientDisconnected(TClient, DisconnectedEventArgs)` | （受保护）当客户端断开连接时的逻辑处理。 |
| `OnStarted/OnClosed/OnPaused` | （受保护）状态变更事件的触发器方法。 |

## 事件概览
| 名称 | 说明 |
| :--- | :--- |
| `ClientConnected` | 在新客户端成功接入并准备好通信时触发。 |
| `ClientDisconnected` | 在客户端断开连接后触发。 |
| `Closed` | 在服务器彻底关闭后触发。 |
| `OnExceptionOccurs` | 在服务器运行期间发生未捕获异常时触发。 |
| `Paused` | 在服务器停止监听新连接（暂停）后触发。 |
| `Started` | 在服务器成功启动监听后触发。 |

## 方法详细说明

### CloseAsync
执行服务器的清理操作。该方法不仅会关闭监听端口，还会遍历并调用所有在线客户端的 `Disconnect` 方法。
- **签名**: `public abstract Task CloseAsync()`

### OnClientConnected (Protected)
将客户端添加至内部字典，并自动订阅该客户端的 `OnDisconnected` 事件以实现自动移除。
- **签名**: `protected virtual void OnClientConnected(TClient client)`

## 示例
```csharp
public class MyServer : BytesServer<TcpClient>
{
    public override async Task StartAsync() { /* 实现 Socket 绑定与监听逻辑 */ }
    public override async Task CloseAsync() { /* 实现 Socket 释放逻辑 */ }
    public override async Task StopAsync() { /* 实现 暂停监听 逻辑 */ }
}

var server = new MyServer();
server.ClientConnected += (s, e) => Console.WriteLine($"新客户端接入: {e.Client.ConnectionId}");
await server.StartAsync();
```
