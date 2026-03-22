# STTech.BytesIO API 文档概述

STTech.BytesIO 是一款专为高吞吐量、低延迟设计的现代化 .NET 异步网络与串口通信库。它全面拥抱了 `.NET` 最新的 **原生异步网络流模型 (Native Async I/O Pump)** 以及 **零拷贝系统 (`System.IO.Pipelines`与`Span<T>`)**，使它无论是用于轻量级上位机工具开发，或是支撑十万级并发的工业级物联网网关，都能胜任。

## 核心特性

- **纯异步非阻塞 (Pure Async/Await)**: 各大通信管道底层彻底抹除了同步阻塞与休眠等待，通过高阶 `TaskCompletionSource`与 `Channel` 无锁化状态机实现 0 线程休眠占用。
- **极致的零拷贝 (Zero-Copy)**: 利用 `.NET System.IO.Pipelines` 进行网络数据的传输和接收。消除了大块网络报文拆粘包中产生的大量 `byte[]` 内存分配与拷贝。
- **高度解耦的插件架构**: 可以通过声明 `IBytesClientPlugin` 安全侵入底层生命周期并对连接、断线重连、心跳等做出细粒度管控。

## 目录索引

- [TcpClient API 手册](./TcpClient.md) - 用于建立高并发的 TCP 客户端。
- [TcpServer API 手册](./TcpServer.md) - 支持数万连接的非阻塞网关级 TCP 服务器。
- [SerialClient API 手册](./SerialClient.md) - 用于与硬件串口交互，支持断连保护与非阻塞读写。
- [IPC API 手册 (进程间通信)](./IPC.md) - 基于 Named Pipes 的极速本地跨进程通信。
- [Unpacker API (拆解包体系)](./Unpacker.md) - 通过 `UnpackContext` 处理复杂的粘包/拆包。

## 全局安装与起步示例

确保项目基于 `netstandard2.0`、`netcoreapp3.1`、或是 `.NET 6/7/8`。

**最简单的建立一次 TCP 连接通信范例：**

```csharp
using STTech.BytesIO.Tcp;

class Program
{
    static async Task Main(string[] args)
    {
        using var client = new TcpClient()
        {
            Host = "192.168.1.100",
            Port = 8080
        };

        // 订阅连接成功事件
        client.OnConnectedSuccessfully += (s, e) => Console.WriteLine("服务器已安全连接!");

        // 订阅原生的异步数据返回事件(由于零拷贝，拿到的直接是 ReceiveContext)
        client.OnDataReceived += (s, e) => 
        {
            Console.WriteLine($"收到服务器返回的数据，长度：{e.Data.Length}");
        };

        // 原生异步连接，释放当前线程
        var connectResult = await client.ConnectAsync();
        
        if(connectResult.IsSuccess)
        {
            // 通过高速异步发送队列把命令投发网卡
            await client.SendAsync(new byte[] { 0x01, 0x02, 0x03 });
        }
    }
}
```
