# IPC (Named Pipes) 库 API 手册

`STTech.BytesIO.Ipc` 命名空间提供了基于 **命名管道 (Named Pipes)** 的进程间通信实现。它完全遵循 `BytesClient` / `BytesServer` 的统一编程模型，使得在同一台机器的不同进程间交换字节流变得异常简单。

## IpcClient (客户端)

`IpcClient` 用于连接到本地或远程机器上的命名管道服务端。

### 属性 (Properties)

| 成员列表 | 类型 | 说明 |
| :--- | :--- | :--- |
| `PipeName` | `string` | 命名管道的唯一名称。 |
| `ServerName` | `string` | 目标服务器名称。默认为 `.` (代表本地)。 |
| `IsConnected` | `bool` | (**只读**) 是否已建立管道连接。 |

### 快速上手 (Quick Start)

```csharp
using STTech.BytesIO.Ipc;

var client = new IpcClient { PipeName = "MyTestPipe" };

client.OnDataReceived += (s, e) => {
    Console.WriteLine($"收到服务端数据: {e.Data.Length} 字节");
};

await client.ConnectAsync();
await client.SendAsync(new byte[] { 0x01, 0x02, 0x03 });
```

---

## IpcServer (服务端)

`IpcServer` 允许您创建一个监听特定名称管道的服务端，支持多个客户端并发连接。

### 属性 (Properties)

| 成员列表 | 类型 | 说明 |
| :--- | :--- | :--- |
| `PipeName` | `string` | 要监听的管道名称。 |
| `MaxConnections` | `int` | 最大允许并发连接的客户端数量。 |
| `Clients` | `IpcClient[]` | (**只读**) 当前所有已连接的客户端列表。 |

### 快速上手 (Quick Start)

```csharp
using STTech.BytesIO.Ipc;

var server = new IpcServer { PipeName = "MyTestPipe" };

server.ClientConnected += (s, e) => {
    Console.WriteLine($"[IPC] 客户端已连接: {e.Client.PipeName}");
    
    e.Client.OnDataReceived += (cs, ce) => {
        // 回显数据 (Echo)
        _ = e.Client.SendAsync(ce.Data.ToArray());
    };
};

await server.StartAsync();
```

---

## 核心优势

1. **高性能**: 内存级数据交换，绕过网络协议栈，延迟极低。
2. **零拷贝**: 同样集成 `System.IO.Pipelines`，在大负荷跨进程数据传输时表现优异。
3. **无缝切换**: 由于 API 与 `TcpClient` 高度一致，您可以轻松地在网络通信与本地 IPC 之间切换，而无需修改核心业务逻辑。
