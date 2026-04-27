# SerialClient Class

## 概述
`SerialClient` 提供了对串行端口（Serial Port）通信的支持。它通过封装 `System.IO.Ports.SerialPort` 实现了统一的 `BytesClient` 模型，使开发者能以异步方式处理复杂的串口数据流。该类特别适用于工业自动化、嵌入式设备通讯等需要精确控制串口参数和处理粘包问题的场景。

## 定义
- **命名空间**: `STTech.BytesIO.Serial`
- **程序集**: `STTech.BytesIO.dll`

```csharp
public partial class SerialClient : BytesClient, ISerialClient
```

**继承关系**: `Object` -> `BytesClient` -> `SerialClient`
**实现接口**: `IBytesClient`, `ISerialClient`, `IDisposable`

## 注解
- **粘包处理**: `SerialClient` 内置了一个基于时间的逻辑（`ReceiveTimeout`）。当串口接收到数据后，若在指定的超时时间内没有新数据进入，则认为当前帧结束并分发至应用层。这有效解决了串口通信中常见的“分片”问题。
- **性能**: 采用了 `BaseStream.ReadAsync` 实现真正的异步 I/O 读写，在不占用 CPU 线程的情况下等待硬件中断。
- **线程安全**: 内部对 `Connect` 和 `Disconnect` 过程进行了保护，防止并发操作导致串口资源冲突。

## 构造函数概览
| 名称 | 说明 |
| :--- | :--- |
| `SerialClient()` | 初始化一个新的 `SerialClient` 实例并创建内部的串口对象。 |

## 属性概览
| 名称 | 说明 |
| :--- | :--- |
| `BaudRate` | 获取或设置串行波特率。 |
| `DataBits` | 获取或设置每个字节的标准数据位长度。 |
| `DiscardNull` | 指示是否丢弃在通信期间接收到的空字节。 |
| `DtrEnable` | 获取或设置一个值，该值在串行通信期间启用数据终端就绪 (DTR) 信号。 |
| `Handshake` | 获取或设置串行端口通信的握手协议。 |
| `IsConnected` | 获取串口当前是否处于打开状态。 |
| `NewLine` | 获取或设置用于解释 `ReadLine` 等方法末尾的值。 |
| `Parity` | 获取或设置端口的奇偶校验协议。 |
| `PortName` | 获取或设置通信端口，包括但不限于所有可用的 COM 端口。 |
| `ReceiveTimeout` | 获取或设置串口接收数据的检测超时时间（毫秒）。默认 50ms。 |
| `RtsEnable` | 获取或设置一个值，指示在串行通信期间是否启用请求发送 (RTS) 信号。 |
| `StopBits` | 获取或设置每个字节的标准停止位数。 |
| `InnerClient` | （受保护）内部使用的 `SerialPort` 对象。 |

## 方法概览
| 名称 | 说明 |
| :--- | :--- |
| `Connect(ConnectArgument)` | 打开串行端口连接。 |
| `Disconnect(DisconnectArgument)` | 关闭串行端口连接。 |
| `DiscardInBuffer()` | 丢弃串口接收缓冲区中的所有数据。 |
| `DiscardOutBuffer()` | 丢弃串口发送缓冲区中的所有数据。 |
| `GetInnerClient()` | 获取原始的 `SerialPort` 实例。 |
| `GetPortNames()` | 获取当前计算机可用的串口名称数组。 |
| `ReceiveDataCompletedHandle()` | （受保护）当接收任务结束时的清理逻辑。 |
| `ReceiveDataHandleAsync(...)` | （受保护）核心串口接收异步循环实现。 |
| `SendHandlerAsync(...)` | （受保护）底层串口数据异步写入实现。 |

## 属性详细说明

### ReceiveTimeout
串口通信是流式的，没有明确的帧界限。`ReceiveTimeout` 属性用于定义在接收到数据后等待下一段数据的最大间隔。若超过此时间未收新数据，则认为一帧完整，触发 `OnDataReceived`。
- **签名**: `public virtual int ReceiveTimeout { get; set; }`

## 方法详细说明

### Connect
尝试打开由 `PortName` 指定的端口。如果端口已被其他程序占用，将抛出 `UnauthorizedAccessException`。
- **签名**: `public override ConnectResult Connect(ConnectArgument argument = null)`

### SendHandlerAsync (Protected)
通过串口底层的 `BaseStream` 执行异步写入。
- **签名**: `protected override async Task SendHandlerAsync(SendArgs args)`

## 示例
```csharp
var client = new SerialClient { PortName = "COM1", BaudRate = 9600, Parity = Parity.None };
client.ReceiveTimeout = 20; // 快速响应模式
var result = await client.ConnectAsync();
if(result.IsSuccess)
{
    await client.SendAsync(new byte[] { 0x55, 0xAA });
}
```
