# SerialClient 库 API 手册 

串口通讯是一个与计算机底层接口和设备系统关联紧密的通讯方式。在 `STTech.BytesIO.Serial.SerialClient` 中，我们抹除了以往大量存在的休眠与系统定时抽查式（Threading/Timer）扫描读取。
**自 3.0 版本起**，我们回到了官方 `System.IO.Ports` 的怀抱，并结合 `.BaseStream.ReadAsync` 从内层接管了操作系统的总线通信系统通知，这解决了多余 10 个串口设备同时通信时造成电脑 CPU 风扇狂转且界面卡死的历史硬伤。

## 属性 (Properties)

| 成员列表 | 类型 | 说明 |
| :--- | :--- | :--- |
| `PortName` | `string` | 此通信端口通讯的目标物理串口名称 (如 `"COM1"`, `"/dev/ttyUSB0"`)。 |
| `BaudRate` | `int` | 串口波特率 (默认 `9600`)。 |
| `DataBits` | `int` | 单个字节的标准数据位长度 (默认 `8`)。 |
| `Parity` | `Parity` | 奇偶校验检查协议。 |
| `StopBits` | `StopBits` | 数据终止信标位。 |
| `ReceiveTimeout` | `int` | **[高频重要]** 组包超时合并时间(粘包间隔时间) (默认 `0`)。例如发送一串字符 32 位底层拆开成了 3 个包发上来，将此值设置为 `50` 代表在 `50` 毫秒内接收到的内容会自动合并后再一次性触发 `OnDataReceived`。该机制现在由纯正 `Task.Delay(cancellation)` 支撑，0卡顿。  |

## 方法 (Methods)

由于 `SerialClient` 也是 `BytesClient` 的正统派生端实现，完全可以使用极速高能并发接口：

- `Task<ConnectResult> ConnectAsync(...)` 挂起串口物理资源连接。
- `Task SendAsync(byte[] data, ...)` 将指令通过独占异步总线发给下端板子。
- `string[] GetPortNames()` 获取当前计算机所有枚举到的虚拟和实体有效串口标识。

## 高级延展 - Modbus 适配支持

在工业场合下大多串口连接对应着 PLC 或标准 Modbus 设备。STTech.BytesIO 提供了开箱即用经过“原封零分配(Zero Allocations)”优化的适配器！通过给串口挂靠解包拓展实现一键剥离：

```csharp
using STTech.BytesIO.Serial;
using STTech.BytesIO.Modbus;

var serialClient = new SerialClient()
{
    PortName = "COM3",
    BaudRate = 115200,
};

// 声明我们要用 零拷贝机制 智能去捕获属于 Modbus RTU 的粘包长帧序列
var unpacker = new ModbusRtuUnpacker(serialClient);

// 以解包者的身份监听数据：现在你拿到的事件永远是一帧极其完整的，不会残缺的纯正合法 Modbus 响应载荷
unpacker.OnDataParsed += (s, e) => 
{
    // 利用 UnpackContext 取出 ReadOnlySequence，没有任何二次数组转换带来的 GC 压力。
    var sequence = e.Context.Data;
    Console.WriteLine($"收到一包经过强验证不缺斤短两的 Modbus 框架：{sequence.Length}");
};

await serialClient.ConnectAsync();
```
