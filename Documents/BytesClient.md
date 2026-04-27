# BytesClient Class

## 概述
`BytesClient` 是一个抽象基类，用于构建基于字节流的通信客户端。它实现了 `IBytesClient` 接口，并提供了高度封装的异步数据收发调度、缓冲区池化管理、自动事件通知以及插件扩展机制。该类旨在简化复杂的底层通信逻辑，为开发者提供一个稳定且可扩展的开发模型。

## 定义
- **命名空间**: `STTech.BytesIO.Core`
- **程序集**: `STTech.BytesIO.dll`

```csharp
public abstract partial class BytesClient : IBytesClient
```

**继承关系**: `Object` -> `BytesClient`
**实现接口**: `IBytesClient`, `IDisposable`

## 注解
`BytesClient` 引入了以下关键设计模式：
- **异步泵 (Async Pump)**: 数据发送采用无锁设计的并发队列和异步泵模式，确保高频发送场景下的任务有序性且不阻塞调用线程。
- **零拷贝与池化**: 集成了 `System.Buffers.ArrayPool<byte>`，在数据接收时通过租借缓冲区减少垃圾回收（GC）压力，提升系统吞吐量。
- **安全性**: 所有的事件回调均通过 `SafelyInvokeCallback` 进行安全包装，捕捉回调代码中的异常以防止基础设施崩溃。
- **生命周期管理**: 每次重新连接都会生成唯一的 `ConnectionId`，方便进行链路追踪。

## 构造函数概览
| 名称 | 说明 |
| :--- | :--- |
| `BytesClient()` | 初始化 `BytesClient` 类的新实例。 |

## 属性概览
| 名称 | 说明 |
| :--- | :--- |
| `ConnectionId` | 获取当前活动会话的唯一凭证标识，每次重连后会刷新。 |
| `DefaultSendOptions` | 获取默认的发送选项。 |
| `IsConnected` | （抽象）获取当前客户端是否处于连接状态。 |
| `LastMessageReceivedTime` | 获取最后一次接收到消息的时间点。 |
| `ReceiveBufferSize` | 获取或设置接收缓冲区的大小。 |
| `SendBufferSize` | 获取或设置发送缓冲区的大小。 |
| `LastConnectedTime` | （受保护）记录最近一次建立连接的时间。 |
| `ReceiveTaskCancellationTokenSource` | （受保护）用于取消异步数据接收任务的令牌源。 |

## 方法概览
| 名称 | 说明 |
| :--- | :--- |
| `Connect(ConnectArgument)` | （抽象）执行建立连接的操作。 |
| `Connect(int)` | 使用指定的超时时间进行连接。 |
| `ConnectAsync(ConnectArgument)` | 异步建立连接。 |
| `Disconnect(DisconnectArgument)` | （抽象）断开通信连接。 |
| `DisconnectAsync(DisconnectArgument)` | 异步断开通信连接。 |
| `Dispose()` | （抽象）释放资源。 |
| `Send(byte[], SendOptions)` | 同步发送字节数组。 |
| `Send(byte[], int, ReplyMatchHandler, SendOptions)` | 发送数据并阻塞等待满足匹配条件的回应。 |
| `SendAsync(byte[], SendOptions)` | 异步发送字节数组。 |
| `SendAsync(byte[], int, ReplyMatchHandler, SendOptions)` | 异步发送数据并等待满足匹配条件的回应。 |
| `CancelReceiveDataTask()` | （受保护）取消当前的异步接收任务。 |
| `CreateReceiveContext(byte[], int, int)` | （受保护）创建接收上下文实例。 |
| `InvokeDataReceivedEventCallback(ReceiveContext)` | （受保护）分发接收到的数据并触发事件。 |
| `RentBuffer()` | （受保护）从数组池中租借缓冲区。 |
| `SafelyInvokeCallback(Action)` | （受保护）安全执行回调函数，防止异常中断。 |
| `StartReceiveDataTask()` | （受保护）启动底层数据接收扫描任务。 |

## 事件概览
| 名称 | 说明 |
| :--- | :--- |
| `OnConnectedSuccessfully` | 在连接建立成功时触发。 |
| `OnConnectionFailed` | 在尝试连接失败时触发。 |
| `OnDataReceived` | 在接收到新的数据帧时触发。 |
| `OnDataSent` | 在成功发送数据后触发。 |
| `OnDisconnected` | 在通信断开时触发。 |
| `OnExceptionOccurs` | 在内部逻辑发生异常时触发。 |

## 属性详细说明

### ConnectionId
每次成功建立连接后生成的 GUID 字符串，用于在复杂的日志系统中标记唯一的会话生命周期。
- **签名**: `public string ConnectionId { get; protected set; }`

### ReceiveBufferSize
设置接收缓冲区大小时，若客户端已处于连接状态，将抛出 `InvalidOperationException`。
- **签名**: `public virtual int ReceiveBufferSize { get; set; }`

## 方法详细说明

### SendAsync
核心异步发送方法。将待发送数据压入内部并发队列，并激活发送泵进行非阻塞传输。
- **签名**: `public Task SendAsync(byte[] data, SendOptions options = null)`
- **参数**:
  - `data`: 原始字节数组。
  - `options`: 包含超时、取消令牌、发送间隙等配置。
- **返回**: 一个 `Task`，表示异步发送操作。

### Send (Request/Reply 模式)
该方法允许开发者发送一条指令并等待特定的响应。
- **签名**: `public ReplyBytes Send(byte[] data, int timeout, ReplyMatchHandler<byte[], ReceiveContext> matchHandler = null, SendOptions options = null)`
- **参数**:
  - `matchHandler`: 回调逻辑，用于在并发的上下文中判断收到的数据包是否是该请求的响应。
- **返回**: `ReplyBytes` 包含响应状态及数据。

### StartReceiveDataTask (Protected)
启动一个后台异步任务循环调用具体的接收处理器。
- **签名**: `protected virtual void StartReceiveDataTask()`

### SafelyInvokeCallback (Protected)
封装用户的第三方代码块。如果代码块抛出异常，将被重定向至 `OnExceptionOccurs` 事件，而不会导致协议栈泵死。
- **签名**: `protected void SafelyInvokeCallback(Action action)`

## 示例
```csharp
public class MyClient : BytesClient
{
    // 实现抽象方法...
    protected override async Task ReceiveDataHandleAsync(CancellationToken token)
    {
        while(!token.IsCancellationRequested)
        {
            byte[] buf = RentBuffer();
            int len = await socket.ReadAsync(buf);
            InvokeDataReceivedEventCallback(CreateReceiveContext(buf, 0, len));
        }
    }
}

// 调用示例
var client = new MyClient();
client.OnDataReceived += (s, e) => Console.WriteLine($"收到: {e.Data.Length} 字节");
await client.ConnectAsync();
await client.SendAsync(new byte[] { 0x01, 0x02 });
```
