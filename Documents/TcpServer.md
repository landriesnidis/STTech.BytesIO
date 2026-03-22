# TcpServer 库 API 手册

`STTech.BytesIO.Tcp.TcpServer` 提供了一个不依赖旧式 `BeginAccept/EndAccept` 死锁线程的现代化零阻塞并发服务器基座。它可以自动维护一个内建并发字典连接池并实现安全的最大终端抗压熔断。

## 属性 (Properties)

| 成员列表 | 类型 | 说明 |
| :--- | :--- | :--- |
| `Host` | `string` | 绑定的本地监听地址。(如 `"0.0.0.0"`) |
| `Port` | `int` | 绑定的本地监听端口。 |
| `UseSsl` | `bool` | 获取或设置是否开启 SSL/TLS 加密。 |
| `Certificate` | `X509Certificate2` | 获取或设置服务器证书。 |
| `ServerCertificateName` | `string` | 获取或设置服务器证书名称。 |
| `SslProtocol` | `SslProtocols` | 获取或设置 SSL 协议版本 (如 `Tls12`)。 |
| `MaxConnections` | `uint` | 获取或设置服务器承载的最大客户端数量。达到此阈值后，新的外部 Accept 请求会被秒速安全关闭释放，而不是崩溃或者直接掐断整个服务端口。设定为 `0` 代表不限并发。 |
| `Clients` | `TcpClient[]` | (**只读**) 获取当前服务成功建立连接的所有活动的客户端对象快照数组。 |

---

## SSL/TLS 加密支持

从 3.0 版本开始，`TcpServer` 提供了深度集成的 SSL/TLS 支持：

```csharp
var server = new TcpServer() {
    Port = 8086,
    UseSsl = true,
    Certificate = new X509Certificate2("server.pfx", "password")
};
await server.StartAsync();
```

## 方法 (Methods)

### StartAsync()
**定义：**
```csharp
public Task StartAsync();
```
**说明：**
监听绑定的网络设备并无阻塞地启动并发 `Accept` 数据入场泵队列守护协程。调用此方法会直接返回，不会引发单点锁块执行假死。

### StopAsync()
暂停服务端的数据接入操作（挂起）。在此时如果有处于最大并发熔断状态，其依旧遵循拒绝安全策略而不会关闭正在通信池里的其他通讯兵。

## 事件 (Events)

- **`ClientConnected`**
  新客户端设备无损连入并完成了安全分配（包括完成非常耗时的内部 TLS 重计算）后触发。

- **`ClientDisconnected`**
  当由于外界拔网线、超时或对方主动切断长连接造成的套接字 0 字节捕获时，它能秒感知并向你触发汇报离线原因及哪个客户端下线。

## 示例学习 (Examples)

### 示例 1： 构建坚不可摧极速转发网关

```csharp
using STTech.BytesIO.Tcp;

var server = new TcpServer()
{
    Host = "0.0.0.0",
    Port = 5002,
    MaxConnections = 10000, 
    // 若达到 10000 活跃，直接返回 SYN 包熔断但不关服务！ 
};

// 某台机器建立完好套接字分配了新的处理槽以后激发
server.ClientConnected += (s, e) =>
{
    Console.WriteLine($"[网关联动] 终端上线 - {e.Client.RemoteEndPoint}");

    // 对于被 Server 分解出的连接设备，你直接注入其接收事件即可：
    e.Client.OnDataReceived += (cs, ce) =>
    {
         Console.WriteLine($"服务器处理中...已收发包量：{ce.Data.Length} 字节");
         // 甚至无需装箱，直接投进原生异步发送管道将同样数据发送回去(Echo 服务器):
         // fire-and-forget: 
         _ = e.Client.SendAsync(ce.Data.ToArray());
    };
};

server.OnExceptionOccurs += (s, e) =>
{
    Console.WriteLine($"[网关异常告警] : {e.Exception.Message}");
};

await server.StartAsync();

Console.WriteLine("万级网关处于监听，敲击任意按盘关闭");
Console.ReadKey();
```
