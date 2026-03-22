# TcpClient 库 API 手册

<a name="TcpClient"></a>
`STTech.BytesIO.Tcp.TcpClient` 类提供了在 .NET 中原生支持非阻塞、全异步调用的 TCP 客户端。它继承自 `BytesClient`。由于内部利用了 `SocketAsyncEventArgs` / `NetworkStream.ReadAsync` / `NetworkStream.WriteAsync`，本类不仅适用于单连接硬件终端通信，即便同时实例化一万个对象，也不会阻塞线程池资源。

## 属性 (Properties)

| 成员列表 | 类型 | 说明 |
| :--- | :--- | :--- |
| `Host` | `string` | 获取或设置要连接的服务器 IP 地址 / 域名。(默认值 `"127.0.0.1"`) |
| `Port` | `int` | 获取或设置要连接的服务器目标监听端口号。(默认值 `8086`) |
| `IsConnected` | `bool` | (**只读**) 获取当前套接字的实时物理与逻辑在线状态。 |
| `ReceiveBufferSize` | `int` | 获取或设置单次网卡最大下发读取容量。(默认 `32768` 即 32KB) |
| `UseSsl` | `bool` | 获取或设置是否开启 SSL/TLS 加密。 |
| `SslProtocol` | `SslProtocols` | 获取或设置 SSL 协议版本 (如 `Tls12`)。 |
| `Certificate` | `X509Certificate2` | 获取或设置用于客户端身份验证的证书。 |
| `ServerCertificateName` | `string` | 获取或设置服务器证书的预期名称（用于 SNI 验证）。 |

## 方法 (Methods)

### ConnectAsync()
**定义：**
```csharp
public Task<ConnectResult> ConnectAsync(ConnectArgument argument = null);
```
**说明：**
使用纯异步非阻塞形式将客户端连接到所配置的 `Host` 和 `Port`。

**返回：**
返回代表当前重载操作连接状态的 `ConnectResult` 结构。检查 `ConnectResult.IsSuccess` 来验证。

---

### SendAsync(...)
**定义：**
```csharp
// 基本用法：直接向远程宿主机异步发射字节队列
public Task SendAsync(byte[] data, SendOptions options = null);

// 高级用法：发出数据之后挂起，直到通过 matchHandler 检测到合法的“心跳回应”或“请求答复”帧才继续执行流
public Task<ReplyBytes> SendAsync(byte[] data, int timeout, ReplyMatchHandler<byte[], ReceiveContext> matchHandler, SendOptions options = null);
```
**说明：**
完全消除线程阻塞，通过 `TaskCompletionSource` 返回发送/接受等待凭证，只有实际调用网卡 IO 完成或触发超时才会返回。利用了内部的 `ConcurrentQueue` 实现，绝对线程安全。

### Disconnect()
断开目前 TCP 连接，并安全释放当前相关的长驻异步协程监听。

## 事件 (Events)

- **`OnDataReceived`**
  当有远端消息投递时触发。携带 `DataReceivedEventArgs`，内含零拷贝缓冲区序列的装载类型 `ReceiveContext`。

- **`OnDisconnected`**
  触发断开连接事件，无论时远端主动发来 FIN 关闭双流，还是被动产生本地错误或超时，都会被可靠拦截。

---

## 示例学习 (Examples)

### 示例 1: 等待具体的设备响应 (Sync-over-Async) 的现代写法
以往开发者常被“请求-等待响应”的复杂黏包处理逼疯。借助最新的 `.SendAsync` 函数重载以及底层的真异步：

```csharp
// 我们给远端发送 0x01 命令并要求在 5 秒内得到 0x01 开头的响应，否则属于超时中断。
var reply = await client.SendAsync(
    data: new byte[] { 0x01, 0xFF, 0xFE }, 
    timeout: 5000, 
    matchHandler: (request, response) => 
    {
        // 此委托用于确定当前从网络流剥离的接收帧是不是我们这发请求对应的响应
        // 如果是，返回 true。否则会被底层抛弃继续等待下一个底层事件
        return response.Data.Length > 0 && response.Data.FirstSpan[0] == request[0]; 
    });

if (reply.Status == ReplyStatus.Success)
{
    Console.WriteLine("收到答复：" + string.Join("-", reply.Response.Data.ToArray().Select(b=>b.ToString("X2"))));
}
else if(reply.Status == ReplyStatus.Timeout)
{
    Console.WriteLine("这台硬件迟迟不出声！超时了！");
}
```
