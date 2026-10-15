# STTech.BytesIO.Modbus 基础开发指南

`STTech.BytesIO.Modbus` 是基于 `STTech.BytesIO` 高性能通信内核打造的现代化工业 Modbus 协议库。该库充分继承了 BytesIO **纯异步非阻塞（Pure Async I/O）** 与 **零拷贝（Zero-Copy Pipelines）** 的核心优势，提供了统一、强类型的 Modbus Master（客户端）通信接口，并全面支持 **Modbus RTU** 与 **Modbus ASCII** 两种工业通用协议格式。

本文档为 **基础开发指南**，主要涵盖 Modbus 协议基础、TCP/串口客户端配置、8 大标准功能码读写实战、请求-响应机制以及工业常用数据类型的解析与转换。

> [!TIP]
> 如果你需要构建 Modbus 从站（服务端/仿真器）、虚拟 PLC 内存设备、多从机透明网关、或了解底层解包器与自定义功能码扩展，请参阅进阶文档：[Modbus 高级开发指南](./ModbusAdvancedTutorial.md)。

---

## 1. Modbus 基础知识与库架构

### 1.1 Modbus 协议核心概念

Modbus 采用主从（Master/Slave）或客户端/服务端（Client/Server）通信架构。在工业网络中，主站发起请求，从站响应请求。Modbus 将设备内部数据划分为四大寄存器区域：

| 寄存器类型 | 英文名称 | 读写权限 | 寻址范围 (PLC 习惯) | 单项数据类型 | 常用功能码 (Hex / Dec) |
| :--- | :--- | :---: | :---: | :---: | :---: |
| **线圈寄存器** | Discrete Outputs (Coils) | 可读可写 | 00001 ~ 09999 | 1 bit (布尔值) | 读: `0x01` (1)<br>写单个: `0x05` (5)<br>写多个: `0x0F` (15) |
| **离散输入寄存器** | Discrete Inputs | 只读 | 10001 ~ 19999 | 1 bit (布尔值) | 读: `0x02` (2) |
| **输入寄存器** | Input Registers | 只读 | 30001 ~ 39999 | 16 bits (双字节) | 读: `0x04` (4) |
| **保持寄存器** | Holding Registers | 可读可写 | 40001 ~ 49999 | 16 bits (双字节) | 读: `0x03` (3)<br>写单个: `0x06` (6)<br>写多个: `0x10` (16) |

> [!NOTE]
> **关于地址偏置（Offset）**：
> PLC 编程软件常使用 `40001` 或 `00001` 作为起始显示地址，而实际在 Modbus 底层通信协议报文中，寄存器物理偏移地址从 `0` 开始（即 `0x0000`）。`STTech.BytesIO.Modbus` 中所有 API 接收的 `startAddress` / `writeAddress` 均为 **0 基物理地址**。

### 1.2 库设计架构

```
┌────────────────────────────────────────────────────────┐
│                   业务应用层 (App / PLC / SCADA)        │
└───────────────────────────┬────────────────────────────┘
                            │
┌───────────────────────────▼────────────────────────────┐
│                    ModbusClient 核心抽象                │
│  - 8 大标准功能码方法 (ReadCoil, WriteHolding...)        │
│  - 强类型请求/响应 (ReadCoilRegisterResponse...)        │
│  - 智能报文匹配器 (ReplyMatchHandle)                    │
└─────────────┬────────────────────────────┬─────────────┘
              │                            │
┌─────────────▼────────────┐ ┌─────────────▼────────────┐
│     ModbusTcpClient      │ │    ModbusSerialClient    │
│  (基于 TcpClient 异步流)  │ │  (基于 SerialClient 串口) │
└─────────────┬────────────┘ └─────────────┬────────────┘
              │                            │
┌─────────────▼────────────────────────────▼─────────────┐
│           ModbusUnpacker (RTU CRC16 / ASCII LRC)       │
└───────────────────────────┬────────────────────────────┘
                            │
┌───────────────────────────▼────────────────────────────┐
│             STTech.BytesIO 底层异步网络与管道内核        │
└────────────────────────────────────────────────────────┘
```

---

## 2. 客户端配置与连接

`STTech.BytesIO.Modbus` 提供了针对网络通信的 `ModbusTcpClient` 和针对物理串口的 `ModbusSerialClient`。

### 2.1 Modbus TCP 客户端 (`ModbusTcpClient`)

适用于通过以太网连接 PLC、工业网关或远程 Modbus TCP 设备。

```csharp
using System;
using System.Threading.Tasks;
using STTech.BytesIO.Modbus;

class Program
{
    static async Task Main(string[] args)
    {
        // 创建 Modbus TCP 客户端 (默认协议格式为 RTU，绝大多数 Modbus TCP 网关内部负载使用 RTU 帧)
        using var client = new ModbusTcpClient(ModbusProtocolFormat.RTU)
        {
            Host = "192.168.1.100",  // 目标设备 IP
            Port = 502               // Modbus TCP 标准端口
        };

        // 订阅连接生命周期事件
        client.OnConnectedSuccessfully += (s, e) => Console.WriteLine("Modbus 设备连接成功！");
        client.OnDisconnected += (s, e) => Console.WriteLine($"连接断开，原因: {e.ReasonCode}");
        client.OnExceptionOccurs += (s, e) => Console.WriteLine($"通信异常: {e.Exception.Message}");

        // 执行异步连接
        var connectResult = await client.ConnectAsync();
        if (!connectResult.IsSuccess)
        {
            Console.WriteLine($"连接失败: {connectResult.ErrorCode}");
            return;
        }

        Console.WriteLine("设备已就绪，可以开始读写数据。");
    }
}
```

### 2.2 Modbus 串口客户端 (`ModbusSerialClient`)

适用于通过 RS-485 / RS-232 硬件串口连接变频器、温控器、电表等现场设备。

```csharp
using System;
using System.IO.Ports;
using STTech.BytesIO.Modbus;

class Program
{
    static void Main(string[] args)
    {
        // 创建 Modbus 串口客户端
        using var serialClient = new ModbusSerialClient(ModbusProtocolFormat.RTU)
        {
            PortName = "COM3",               // 串口名称
            BaudRate = 9600,                 // 波特率 (常见 9600, 19200, 115200)
            DataBits = 8,                    // 数据位
            Parity = Parity.None,            // 校验位 (None, Even, Odd)
            StopBits = StopBits.One,         // 停止位
            RtsEnable = true                 // RS485 硬件流控使能
        };

        // 同步连接
        var result = serialClient.Connect();
        if (result.IsSuccess)
        {
            Console.WriteLine("串口打开成功！");
        }
        else
        {
            Console.WriteLine($"串口打开失败: {result.ErrorCode}");
        }
    }
}
```

### 2.3 协议格式切换 (RTU 与 ASCII)

- **RTU 格式（二进制模式）**：传输效率高，紧凑，每帧通过 CRC16 进行冗余循环校验，是工业上最普及的格式。
- **ASCII 格式（文本模式）**：以冒号 `:`（`0x3A`）作为帧头，以回车换行 `CRLF`（`\r\n`）作为结束，通过 LRC 纵向冗余校验。报文均为可打印十六进制字符，方便抓包与调试。

切换格式只需在构造或初始化时指定 `ModbusProtocolFormat`：

```csharp
// 构造 ASCII 模式的客户端
var asciiClient = new ModbusTcpClient(ModbusProtocolFormat.ASCII)
{
    Host = "192.168.1.100",
    Port = 502
};

// 也可以在运行中动态切换 (底层解包器的 StartMark 与校验器会自动同步更新)
asciiClient.ProtocolFormat = ModbusProtocolFormat.RTU;
```

---

## 3. 标准功能码读写实战

`ModbusClient` 提供了直接针对 8 大 Modbus 标准功能码的高级封装方法。所有读写方法均支持指定**从机地址（SlaveId）**、**寄存器地址（Address）**、**超时时间（Timeout）**等。

### 3.1 读线圈寄存器 (Function 0x01)

用于读取一组连续的输出线圈（开关量输出）的状态。

```csharp
// 读取从机 1，起始地址 0，连续读取 16 个线圈，超时时间 2000ms
var reply = client.ReadCoilRegister(slaveId: 1, startAddress: 0, length: 16, timeout: 2000);

if (reply.IsCompleted && reply.GetResponse().IsSuccess)
{
    var response = reply.GetResponse();
    bool[] coils = response.Values; // 获取布尔数组
    
    for (int i = 0; i < coils.Length; i++)
    {
        Console.WriteLine($"线圈 Q{i}: {(coils[i] ? "ON (闭合)" : "OFF (断开)")}");
    }
}
else
{
    Console.WriteLine($"读取失败或超时，状态: {reply.Status}");
}
```

### 3.2 读离散输入寄存器 (Function 0x02)

用于读取开关量输入通道（如光电开关、接近开关、按钮等）。

```csharp
// 读取从机 1，起始地址 10，读取 8 个离散输入
var reply = client.ReadDiscreteInputRegister(slaveId: 1, startAddress: 10, length: 8);

if (reply.IsCompleted && reply.GetResponse().IsSuccess)
{
    bool[] inputs = reply.GetResponse().Values;
    for (int i = 0; i < inputs.Length; i++)
    {
        Console.WriteLine($"输入 DI{10 + i}: {inputs[i]}");
    }
}
```

### 3.3 读保持寄存器 (Function 0x03)

最常用的寄存器读取方法，用于读取设定值、控制字、模拟量输出、计数器等。

```csharp
// 读取从机 1，起始地址 0，读取 5 个保持寄存器 (每个寄存器 16 位 / 2 字节)
var reply = client.ReadHoldingRegister(slaveId: 1, startAddress: 0, length: 5);

if (reply.IsCompleted && reply.GetResponse().IsSuccess)
{
    var response = reply.GetResponse();
    
    // 方式 1: 直接获取 ushort 数组
    ushort[] registers = response.GetUInt16Array();
    for (int i = 0; i < registers.Length; i++)
    {
        Console.WriteLine($"寄存器 [{i}]: 0x{registers[i]:X4} ({registers[i]})");
    }

    // 方式 2: 获取原始高低字节序列
    byte[] rawBytes = response.Values;
    Console.WriteLine($"原始报文字节: {BitConverter.ToString(rawBytes)}");
}
```

### 3.4 读输入寄存器 (Function 0x04)

用于读取现场只读模拟量数据（如传感器温度、压力、流量计实时读数等）。

```csharp
// 读取从机 1，起始地址 100，读取 2 个输入寄存器
var reply = client.ReadInputRegister(slaveId: 1, startAddress: 100, length: 2);

if (reply.IsCompleted && reply.GetResponse().IsSuccess)
{
    ushort[] values = reply.GetResponse().GetUInt16Array();
    Console.WriteLine($"温度输入: {values[0] * 0.1f} ℃");
    Console.WriteLine($"湿度输入: {values[1] * 0.1f} %RH");
}
```

### 3.5 写单个线圈寄存器 (Function 0x05)

用于控制单路数字量输出的闭合（通）或断开（断）。

```csharp
// 将从机 1，地址 5 的线圈闭合 (置 true / ON)
var replyOn = client.WriteSingleCoilRegister(slaveId: 1, writeAddress: 5, data: true);
if (replyOn.IsCompleted && replyOn.GetResponse().IsSuccess)
{
    Console.WriteLine("线圈 5 置通成功！");
}

// 将从机 1，地址 5 的线圈断开 (置 false / OFF)
var replyOff = client.WriteSingleCoilRegister(slaveId: 1, writeAddress: 5, data: false);
if (replyOff.IsCompleted && replyOff.GetResponse().IsSuccess)
{
    Console.WriteLine("线圈 5 复位成功！");
}
```

> [!NOTE]
> Modbus 规范规定写单线圈置通时传输的数值为 `0xFF00`，断开时为 `0x0000`。`ModbusClient` 在底层已自动处理符合规范的高低字节映射，业务层只需传递直观的布尔值 `true` 或 `false`。

### 3.6 写单个保持寄存器 (Function 0x06)

用于向从机单个 16 位寄存器写入数值。

```csharp
// 向从机 1，地址 20 写入 ushort 数值 1234
var reply = client.WriteSingleHoldingRegister(slaveId: 1, writeAddress: 20, value: 1234);

if (reply.IsCompleted && reply.GetResponse().IsSuccess)
{
    Console.WriteLine("寄存器写入成功！");
}

// 也可以直接写入 2 字节原始数组 (大端序)
var replyBytes = client.WriteSingleHoldingRegister(slaveId: 1, writeAddress: 20, data: new byte[] { 0x04, 0xD2 });
```

### 3.7 写多个线圈寄存器 (Function 0x0F / 15)

用于一次性批量写入一组连续线圈的状态。

```csharp
// 准备写入 10 个线圈的开关状态
bool[] coilStates = new bool[] { true, false, true, true, false, false, true, false, true, true };

// 向从机 1，起始地址 10 写入该数组
var reply = client.WriteMultipleCoilRegisters(slaveId: 1, writeAddress: 10, data: coilStates);

if (reply.IsCompleted && reply.GetResponse().IsSuccess)
{
    Console.WriteLine($"批量写入 {coilStates.Length} 个线圈成功！");
}
```

### 3.8 写多个保持寄存器 (Function 0x10 / 16)

用于一次性批量写入连续的保持寄存器，常用于下发参数配方或写入 32 位/64 位多寄存器组合数据。

```csharp
// 写入 3 个连续寄存器
ushort[] settings = new ushort[] { 100, 200, 300 };

// 向从机 1，起始地址 50 写入数据
var reply = client.WriteMultipleHoldingRegisters(slaveId: 1, writeAddress: 50, values: settings);

if (reply.IsCompleted && reply.GetResponse().IsSuccess)
{
    Console.WriteLine("批量寄存器写入成功！");
}
```

---

## 4. 请求-响应模型与异常处理

### 4.1 `Reply<T>` 状态机机制

在 `STTech.BytesIO` 中，所有发送操作都返回强类型的 `Reply<T>` 对象。它是一个完整的异步任务状态机：

```csharp
var reply = client.ReadHoldingRegister(slaveId: 1, startAddress: 0, length: 10, timeout: 3000);

switch (reply.Status)
{
    case ReplyStatus.Completed:
        // 收到来自从机的合法 Modbus 响应包
        var response = reply.GetResponse();
        if (response.IsSuccess)
        {
            Console.WriteLine("通信成功且从机执行无误！");
        }
        else
        {
            // 从机返回了 Modbus 异常响应帧 (功能码最高位置 1)
            Console.WriteLine($"从机返回错误码: {response.ErrorCode} (0x{(byte)response.ErrorCode:X2})");
        }
        break;

    case ReplyStatus.Timeout:
        // 在指定的超时时间 (3000ms) 内未收到匹配的响应
        Console.WriteLine("请求超时！请检查物理连线、从机供电或从机地址是否匹配。");
        break;

    case ReplyStatus.Error:
        // 底层网络错误，如发送时连接突然断开
        Console.WriteLine($"网络层发送异常: {reply.Exception?.Message}");
        break;
}
```

### 4.2 Modbus 标准错误码 (`ModbusErrorCode`)

当从机接收到报文但无法执行时，会返回带有错误码的异常帧。`response.ErrorCode` 映射了所有工业标准错误码：

```csharp
if (!response.IsSuccess)
{
    switch (response.ErrorCode)
    {
        case ModbusErrorCode.IllegalFunction:
            Console.WriteLine("错误: 从机不支持该功能码。");
            break;
        case ModbusErrorCode.IllegalDataAddress:
            Console.WriteLine("错误: 寄存器地址越界或不存在。");
            break;
        case ModbusErrorCode.IllegalDataValue:
            Console.WriteLine("错误: 写入的寄存器数值不在设备允许的取值范围内。");
            break;
        case ModbusErrorCode.SlaveDeviceFailure:
            Console.WriteLine("错误: 从机内部硬件发生故障。");
            break;
        case ModbusErrorCode.SlaveDeviceBusy:
            Console.WriteLine("错误: 从机正忙，稍后重试。");
            break;
        default:
            Console.WriteLine($"其他 Modbus 异常: {response.ErrorCode}");
            break;
    }
}
```

---

## 5. 事件驱动模式 (PacketReceived)

除了直接在 `Read` / `Write` 方法中获取 `Reply`，`ModbusClient` 还提供了全套事件体系，支持以**事件驱动（Event-Driven）**或**数据监听广播**的模式处理报文：

```csharp
// 监听所有 Modbus 响应数据包
client.OnModbusPacketReceived += (sender, e) =>
{
    ModbusResponse resp = e.Packet;
    Console.WriteLine($"[全局监听] 收到从机 {resp.SlaveId} 功能码 {resp.FunctionCode} 报文");
};

// 专门监听读保持寄存器的返回数据包
client.OnReadHoldingRegisterPacketReceived += (sender, e) =>
{
    ReadHoldingRegisterResponse resp = e.Packet;
    Console.WriteLine($"[保持寄存器] 收到数据长度: {resp.Length} 字节");
};

// 专门监听写单个线圈的返回数据包
client.OnWriteSingleCoilRegisterPacketReceived += (sender, e) =>
{
    WriteRegisterResponse resp = e.Packet;
    Console.WriteLine($"[写线圈确认] 写入地址: {resp.WriteAddress}");
};
```

---

## 6. 工业常用多字节数据转换

Modbus 寄存器以 16 位（2 字节）为基准单位。在工业现场中，32 位整数（Float、Int32）和 64 位双精度浮点数（Double）通常跨越 2 个或 4 个寄存器存储。由于不同 PLC 厂商（西门子、施耐德、三菱、欧姆龙）采用的字节序（Endianness）各异，掌握数据转换至关重要。

### 6.1 读取 32 位浮点数 (Float / Single)

一个 32 位 IEEE 754 单精度浮点数占用 2 个连续的保持寄存器（4 字节）。

```csharp
// 读取 2 个寄存器 (4 字节)
var reply = client.ReadHoldingRegister(slaveId: 1, startAddress: 0, length: 2);

if (reply.IsCompleted && reply.GetResponse().IsSuccess)
{
    byte[] bytes = reply.GetResponse().Values; // 长度为 4 的字节数组

    // 常见字节序解析示例:
    // 1. ABCD (大端 Big-Endian，如西门子 S7 系列网关):
    float valABCD = BitConverter.ToSingle(new byte[] { bytes[3], bytes[2], bytes[1], bytes[0] }, 0);

    // 2. CDAB (小端带字交换 Little-Endian Word Swap，多数 Modbus 仪表默认):
    float valCDAB = BitConverter.ToSingle(new byte[] { bytes[1], bytes[0], bytes[3], bytes[2] }, 0);

    // 3. BADC (大端带字节交换 Byte Swap):
    float valBADC = BitConverter.ToSingle(new byte[] { bytes[2], bytes[3], bytes[0], bytes[1] }, 0);

    Console.WriteLine($"解析浮点数值: {valCDAB}");
}
```

### 6.2 写入 32 位整数 (Int32 / UInt32)

将 C# 的 `int` 拆分为 2 个 `ushort` 写入保持寄存器：

```csharp
int targetValue = 12345678;

// 将 32 位整数转为 4 字节
byte[] intBytes = BitConverter.GetBytes(targetValue);

// 按 ABCD (大端序) 组装两个 ushort 寄存器
ushort regHigh = (ushort)((intBytes[3] << 8) | intBytes[2]);
ushort regLow = (ushort)((intBytes[1] << 8) | intBytes[0]);

var writeReply = client.WriteMultipleHoldingRegisters(
    slaveId: 1, 
    writeAddress: 10, 
    values: new ushort[] { regHigh, regLow }
);
```

### 6.3 辅助扩展类封装推荐

在大型项目中，建议封装通用的字节序转换工具类：

```csharp
public static class ModbusDataConverter
{
    public static float ToFloat(byte[] data, int startIndex, ByteOrder order = ByteOrder.CDAB)
    {
        byte b0 = data[startIndex];
        byte b1 = data[startIndex + 1];
        byte b2 = data[startIndex + 2];
        byte b3 = data[startIndex + 3];

        byte[] raw = order switch
        {
            ByteOrder.ABCD => new[] { b3, b2, b1, b0 },
            ByteOrder.CDAB => new[] { b1, b0, b3, b2 },
            ByteOrder.BADC => new[] { b2, b3, b0, b1 },
            ByteOrder.DCBA => new[] { b0, b1, b2, b3 },
            _ => throw new ArgumentOutOfRangeException(nameof(order))
        };

        return BitConverter.ToSingle(raw, 0);
    }
}

public enum ByteOrder
{
    ABCD, // Big-Endian
    CDAB, // Little-Endian byte swap
    BADC, // Big-Endian byte swap
    DCBA  // Little-Endian
}
```

---

## 7. 基础开发完整示例：PLC 循环巡检程序

以下是一个工业场景中常见的自动化监控程序：连接 PLC，循环读取模拟量与开关状态，根据异常自动重连。

```csharp
using System;
using System.Threading;
using System.Threading.Tasks;
using STTech.BytesIO.Modbus;

class Program
{
    static async Task Main(string[] args)
    {
        using var client = new ModbusTcpClient(ModbusProtocolFormat.RTU)
        {
            Host = "127.0.0.1",
            Port = 502
        };

        var connectResult = await client.ConnectAsync();
        if (!connectResult.IsSuccess)
        {
            Console.WriteLine("连接失败，程序退出。");
            return;
        }

        Console.WriteLine("已连接 PLC，启动每秒巡检...");
        using var cts = new CancellationTokenSource();

        // 捕获 Ctrl+C 安全退出
        Console.CancelKeyPress += (s, e) => { e.Cancel = true; cts.Cancel(); };

        while (!cts.IsCancellationRequested)
        {
            try
            {
                // 1. 读取水泵开关状态 (地址 0 开始的 4 个线圈)
                var coilReply = client.ReadCoilRegister(1, 0, 4, timeout: 1000);
                if (coilReply.IsCompleted && coilReply.GetResponse().IsSuccess)
                {
                    var coils = coilReply.GetResponse().Values;
                    Console.WriteLine($"[线圈状态] 1#泵: {coils[0]}, 2#泵: {coils[1]}, 阀门: {coils[2]}");
                }

                // 2. 读取管网压力与水箱温度 (地址 10 开始的 2 个保持寄存器)
                var holdReply = client.ReadHoldingRegister(1, 10, 2, timeout: 1000);
                if (holdReply.IsCompleted && holdReply.GetResponse().IsSuccess)
                {
                    var regs = holdReply.GetResponse().GetUInt16Array();
                    double pressure = regs[0] * 0.01; // 放大 100 倍存储的压力值 (MPa)
                    double temp = regs[1] * 0.1;       // 放大 10 倍存储的温度值 (℃)
                    Console.WriteLine($"[模拟量] 压力: {pressure:F2} MPa, 温度: {temp:F1} ℃");
                }

                await Task.Delay(1000, cts.Token);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"巡检异常: {ex.Message}");
                await Task.Delay(2000);
            }
        }

        Console.WriteLine("巡检结束，安全断开连接。");
    }
}
```

---

## 8. 下一步阅读

掌握了基础读写操作后，你可以继续探索：
- 构建 Modbus 从站（服务端）：**[Modbus 高级开发指南 - 服务端与从站开发](./ModbusAdvancedTutorial.md#1-modbus-服务端从站仿真开发)**
- 虚拟设备与 PLC 内存区监视器：**[Modbus 高级开发指南 - 虚拟设备与寄存器监视区](./ModbusAdvancedTutorial.md#2-虚拟-modbus-设备与内存区监视系统)**
- 透明网关转发与自定扩展：**[Modbus 高级开发指南 - 工业网关与透明转发](./ModbusAdvancedTutorial.md#3-工业网关与透明转发-gateway--forwarding)**
