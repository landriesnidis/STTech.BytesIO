# STTech.BytesIO.Modbus 进阶开发指南

在工业物联网、自动化集成与智慧工厂等复杂场景中，单纯作为 Modbus 客户端读写寄存器远远不能满足所有的业务需求。系统往往需要作为 **从站（Slave/Server）** 接收上位机控制、作为 **仿真器（Simulator）** 模拟现场 PLC 运行、作为 **透明网关（Gateway）** 进行多总线协议转发，或者扩展私有功能码并实现高并发的寄存器内存管理。

本文档是 `STTech.BytesIO.Modbus` 的 **进阶开发指南**，深入剖析服务端架构、虚拟设备与内存区监视系统、工业网关转发机制、解包与报文匹配内核，以及工业级可靠性最佳实践。

> [!TIP]
> 如果你是初学者，或者希望快速了解 Modbus 基础连接、8 大功能码调用与数据类型转换，请先阅读：[Modbus 基础开发指南](./ModbusBasicTutorial.md)。

---

## 1. Modbus 服务端 (从站/仿真器开发)

`STTech.BytesIO.Modbus` 提供了针对网络通信的 `ModbusTcpServer` 和针对物理串口的 `ModbusRtuServer`。作为服务端时，你的应用程序将充当 Modbus 从站（Slave），监听主站（Master）发送的读写指令并回送数据。

### 1.1 Modbus TCP 服务端 (`ModbusTcpServer`)

适用于构建软件 PLC、工业边缘计算节点、SCADA 数据转发节点或上位机仿真器。

```csharp
using System;
using System.Threading.Tasks;
using STTech.BytesIO.Modbus;

class Program
{
    static async Task Main(string[] args)
    {
        // 创建 Modbus TCP 服务端 (协议格式支持 RTU 帧负载与 ASCII 格式)
        using var server = new ModbusTcpServer(ModbusProtocolFormat.RTU)
        {
            Port = 502,      // 监听端口
            SlaveId = 1      // 本机从站地址 (Station Number)
        };

        // 监听客户端连接与断开生命周期
        server.ClientConnected += (s, e) =>
        {
            Console.WriteLine($"[客户端接入] 客户端地址: {e.Client.RemoteEndPoint}");
        };

        server.ClientDisconnected += (s, e) =>
        {
            Console.WriteLine($"[客户端断开] 客户端地址: {e.Client.RemoteEndPoint}, 断开原因: {e.ReasonCode}, 持续时长: {e.Duration}");
        };

        // 异步启动监听 (非阻塞)
        await server.StartAsync();
        Console.WriteLine($"Modbus TCP 服务端已在端口 {server.Port} 启动，从站地址: {server.SlaveId}");

        // 等待退出信号
        Console.ReadLine();

        // 异步停止
        await server.StopAsync();
    }
}
```

### 1.2 Modbus 串口/RTU 服务端 (`ModbusRtuServer`)

适用于在嵌入式工控机（如工控板、树莓派）上通过物理串口（RS-485）充当从站设备，挂载在现场 485 总线上供主站轮询。

```csharp
using System;
using STTech.BytesIO.Modbus;

class Program
{
    static void Main(string[] args)
    {
        using var rtuServer = new ModbusRtuServer(ModbusProtocolFormat.RTU)
        {
            PortName = "COM2",
            BaudRate = 19200,
            SlaveId = 2
        };

        rtuServer.Start();
        Console.WriteLine($"Modbus RTU 从站已在 {rtuServer.PortName} 启动，地址: {rtuServer.SlaveId}");

        Console.ReadLine();
        rtuServer.Stop();
    }
}
```

---

## 2. 手动事件驱动响应机制

如果你的从站逻辑非常简单，或者每个请求都需要动态计算并查询数据库，你可以直接订阅 `ModbusServer` 暴露的 8 大请求事件。

### 2.1 读请求事件与响应赋值

当客户端发送读线圈、保持寄存器等请求时，服务端会触发对应的 `Read...Requested` 事件。事件参数中包含了请求对象，以及用于存放返回结果的 `ResponseData`。

```csharp
// 监听主站读取保持寄存器请求 (0x03)
server.ReadHoldingRegisterRequested += (sender, e) =>
{
    ushort startAddr = e.Request.StartAddress;
    ushort length = e.Request.Length;
    Console.WriteLine($"收到主站读取保持寄存器: 起始地址={startAddr}, 长度={length}");

    // 1. 地址合法性校验 (假设仅开放 0~99 号寄存器)
    if (startAddr + length > 100)
    {
        // 模拟返回从站异常帧：非法数据地址
        e.IsError = true;
        e.ErrorCode = ModbusErrorCode.IllegalDataAddress;
        return;
    }

    // 2. 准备返回数据 (每个寄存器 2 字节)
    ushort[] registerValues = new ushort[length];
    for (int i = 0; i < length; i++)
    {
        // 动态模拟生成数据 (例如实时时间秒数、计数值)
        registerValues[i] = (ushort)(startAddr + i + DateTime.Now.Second);
    }

    // 3. 将数据转换为大端序字节数组填入 ResponseData
    List<byte> bytes = new List<byte>();
    foreach (var val in registerValues)
    {
        bytes.Add((byte)(val >> 8));    // 高位
        bytes.Add((byte)(val & 0xFF));  // 低位
    }

    e.ResponseData = bytes.ToArray();
};
```

### 2.2 写请求事件与业务控制

当主站发送写单个/多个寄存器或线圈指令时，触发对应的 `Write...Requested` 事件：

```csharp
// 监听主站写单个线圈请求 (0x05)
server.WriteSingleCoilRegisterRequested += (sender, e) =>
{
    ushort coilAddr = e.Request.WriteAddress;
    bool state = e.Request.Data; // true 为通，false 为断

    Console.WriteLine($"[控制指令] 主站请求将线圈 {coilAddr} 设置为: {(state ? "闭合" : "断开")}");

    // 执行物理动作 (如驱动继电器或安全互锁校验)
    if (coilAddr == 10 && state == true)
    {
        // 假设 10 号泵需要安全互锁，不允许启动
        e.IsError = true;
        e.ErrorCode = ModbusErrorCode.SlaveDeviceFailure; // 返回从机故障
        return;
    }

    // 正常执行：不设置 e.IsError，底层会自动回显标准确认帧
};

// 监听主站写多个保持寄存器请求 (0x10)
server.WriteMultipleHoldingRegistersRequested += (sender, e) =>
{
    ushort startAddr = e.Request.WriteAddress;
    ushort[] values = e.Request.GetUInt16Array(); // 直接获取传入的 ushort 数组

    Console.WriteLine($"[配方下发] 主站向地址 {startAddr} 写入了 {values.Length} 个寄存器");
    for (int i = 0; i < values.Length; i++)
    {
        Console.WriteLine($"  地址 [{startAddr + i}] = {values[i]}");
    }
};
```

---

## 3. 虚拟 Modbus 设备与内存区监视系统

### 3.1 为什么需要 `VirtualModbusDevice`？

在大型仿真或实际工业软件中，如果所有读写都使用手动事件，会导致大量重复的地址计算、边界判断和高低字节组装代码。

`STTech.BytesIO.Modbus` 提供了高度解耦的 **虚拟 Modbus 设备（`VirtualModbusDevice`）** 与 **寄存器内存监视区（`RegisterValueMonitor`）** 体系：
- **自动路由**：虚拟设备挂载到 `ModbusServer` 上，自动分发读写请求至对应的内存区。
- **自动边界与错误处理**：主站访问未开放或越界的地址时，自动抛出标准的 `IllegalDataAddress` 异常响应。
- **内存变更驱动业务**：主站向寄存器写入数据时，监视器会自动触发 `ItemUpdated` 或 `BatchUpdated` 事件，以数据响应式驱动业务逻辑。

```
┌────────────────────────────────────────────────────────┐
│                   ModbusTcpServer                      │
└───────────────────────────┬────────────────────────────┘
                            │ (路由读写请求)
┌───────────────────────────▼────────────────────────────┐
│                  VirtualModbusDevice                   │
│   (负责地址区间命中判定、数据切片与标准错误响应组包)      │
└─────────────┬────────────────────────────┬─────────────┘
              │ Mount                      │ Mount
┌─────────────▼────────────┐ ┌─────────────▼────────────┐
│ CoilRegisterValueMonitor │ │HoldingRegisterValueMonitor│
│ (线圈区: 地址 0 ~ 99)    │ │(保持寄存器: 地址 0 ~ 999) │
└─────────────┬────────────┘ └─────────────┬────────────┘
              │ Source[i]                  │ Source[i]
┌─────────────▼────────────────────────────▼─────────────┐
│                 PLC 内存镜像 / 物理 I/O                │
└────────────────────────────────────────────────────────┘
```

### 3.2 4 大寄存器监视区

| 监视器类型 | 映射寄存器 | 基础数据类型 | 常见应用 |
| :--- | :--- | :---: | :--- |
| **`CoilRegisterValueMonitor`** | 线圈寄存器 (0x01, 0x05, 0x0F) | `bool` | 阀门开闭、启停指示、继电器输出状态 |
| **`DiscreteInputRegisterValueMonitor`** | 离散输入寄存器 (0x02) | `bool` | 急停按钮、限位开关、红外光电检测 |
| **`HoldingRegisterValueMonitor`** | 保持寄存器 (0x03, 0x06, 0x10) | `ushort` | 目标设定温度、变频器频率、配方参数 |
| **`InputRegisterValueMonitor`** | 输入寄存器 (0x04) | `ushort` | 温度传感器、液位计、电压电流采样值 |

### 3.3 搭建虚拟从站设备的完整范例

```csharp
using System;
using STTech.BytesIO.Modbus;
using STTech.BytesIO.Modbus.Monitors;

class Program
{
    static void Main(string[] args)
    {
        // 1. 初始化 TCP 服务端
        var server = new ModbusTcpServer(ModbusProtocolFormat.RTU)
        {
            Port = 502,
            SlaveId = 1
        };

        // 2. 创建虚拟设备绑定到服务端
        var virtualDevice = new VirtualModbusDevice(server);

        // 3. 规划并挂载寄存器内存区
        // 挂载线圈区：起始地址 0，长度 100 (地址范围 0 ~ 99)
        var coilMonitor = new CoilRegisterValueMonitor(startAddress: 0, length: 100);
        virtualDevice.Mount(coilMonitor);

        // 挂载保持寄存器区：起始地址 0，长度 200 (地址范围 0 ~ 199)
        var holdingMonitor = new HoldingRegisterValueMonitor(startAddress: 0, length: 200);
        virtualDevice.Mount(holdingMonitor);

        // 4. 监听保持寄存器的数值变化事件 (主站写寄存器或本地刷新时触发)
        holdingMonitor.ValueChanged += (sender, e) =>
        {
            int index = e.Index;                                         // 数组内相对索引
            ushort addr = (ushort)(holdingMonitor.StartAddress + index); // 物理寄存器地址
            ushort oldVal = e.OriginalValue;                             // 变更前数值
            ushort newVal = e.NewValue;                                  // 变更后数值

            Console.WriteLine($"[寄存器写入告警] 地址 40001 偏移 [{addr}] 由 {oldVal} 变更为 -> {newVal}");
            
            // 例如：当地址 10 的控制字被写入 1 时，触发水泵启动
            if (addr == 10 && newVal == 1)
            {
                Console.WriteLine(">>> 收到启动信号，启动工业水泵！");
                coilMonitor.Source[0] = true; // 同步将 0 号线圈(水泵运行指示)置为 true
                coilMonitor.Notify();         // 通知刷新
            }
        };

        // 5. 本地程序直接修改内存区 (供主站读取)
        holdingMonitor.Source[0] = 500;  // 设定转速为 500 RPM
        holdingMonitor.Source[1] = 235;  // 实时温度 23.5 ℃ (放大10倍)
        holdingMonitor.Notify();

        server.Start();
        Console.WriteLine("虚拟 Modbus 设备已启动，正在监听 502 端口...");
        Console.ReadLine();
    }
}
```

### 3.4 客户端定时同步与变更差分 (`UpdateRegisterValueMonitor`)

`STTech.BytesIO.Modbus` 为客户端提供了高效的内存区同步扩展方法。客户端本地持有一个 `HoldingRegisterValueMonitor`，通过扩展方法自动轮询从机并将数据比对刷新到本地：

```csharp
// 客户端在本地维护一个从机 0~49 号保持寄存器的镜像
var localMonitor = new HoldingRegisterValueMonitor(0, 50);

// 订阅本地镜像的变更事件
localMonitor.ValueChanged += (s, e) =>
{
    Console.WriteLine($"检测到从机寄存器 [{e.Index}] 发生变更: {e.OriginalValue} -> {e.NewValue}");
};

// 在定时器或后台轮询循环中更新本地监视区
var result = client.UpdateHoldingRegisterValueMonitor(localMonitor, slaveId: 1, timeout: 500);

// MonitorUpdateResult 自带 IsSuccess 快捷判断 (Reply 完成且无错误码)
if (result.IsSuccess)
{
    // localMonitor 内部已自动更新，并在数据有差异时触发了 ValueChanged 事件
}
```

---

## 4. 工业网关与透明转发 (Gateway & Forwarding)

### 4.1 工业网关典型场景

在实际工控架构中，以太网交换机通常接入一个 Modbus TCP 网关，网关下挂了一条 RS-485 菊花链总线，连接了从机地址为 1, 2, 3... 的多个仪表。
上位机统一连接网关的 IP 和端口，但请求的 `SlaveId` 各不相同：
- 若请求的 `SlaveId` 是网关自身，由网关本地处理。
- 若请求的 `SlaveId` 是现场下位机仪表，网关必须**原封不动**地将报文透传转发到 485 串口，并将串口返回的数据回复给上位机。

### 4.2 `ModbusServer` 原生透明网关架构

`STTech.BytesIO.Modbus` 在底层深度集成了工业网关路由中枢，完全无需开发者自行拦截报文或手动转发生命周期：

```
                              ┌───────────────────────────────────┐
                              │    上位机 (SCADA / MES / PLC)      │
                              └─────────────────┬─────────────────┘
                                                │ Modbus TCP (以太网)
                                                ▼
┌─────────────────────────────────────────────────────────────────────────────────┐
│                              ModbusTcpServer                                    │
│                                                                                 │
│  [ModbusRequestUnpacker.IsLocalSlaveId] ───► 请求站号匹配？                     │
│                                                   ├── 是 (SlaveId == 1)         │
│                                                   │    └──► [本地 VirtualDevice] │
│                                                   │                              │
│                                                   └── 否 (SlaveId != 1)         │
│                                                        └──► [ModbusForwardRequest]
│                                                                   │             │
│  [DownstreamNodes 专属路由 / DownstreamClient 默认路由] ◄──────────┘             │
└───────────────────────┬─────────────────────────────────┬───────────────────────┘
                        │ DownstreamNodes[2]              │ DownstreamClient
                        ▼ SerialClient (COM1)             ▼ SerialClient (COM2)
                 ┌──────────────┐                  ┌──────────────┐
                 │ 2# 现场温控器 │                  │ 3# 现场电度表 │
                 └──────────────┘                  └──────────────┘
```

1. **自动识别转发帧（`ModbusForwardRequest`）**：
   服务端接收到主站报文时，内置的 `ModbusRequestUnpacker` 会调用 `IsLocalSlaveId` 校验站号。若不属于网关自身（`SlaveId != server.SlaveId`），直接打包为 `ModbusForwardRequest`。
2. **多通道路由派发（`DownstreamNodes` / `DownstreamClient`）**：
   - 优先通过 `DownstreamNodes[slaveId]` 匹配专属下游通信通道（例如：2 号站走串口 1，3 号站走串口 2）。
   - 若未配置专属节点，则退而求其次交付给默认的 `DownstreamClient`。
3. **全双工自动回复（`BroadcastUpstream`）**：
   下游设备处理完毕后经串口/TCP 返回报文，服务端监听到下游 `OnDataReceived` 后，自动调用 `BroadcastUpstream` 将原始报文广播回复给上游客户端。整个过程**零阻塞、零内存拷贝、全异步流转**！

### 4.3 完整实现：5 行代码构建工业级 Modbus TCP 转 RTU 透明网关

```csharp
using System;
using STTech.BytesIO.Modbus;
using STTech.BytesIO.Modbus.Monitors;
using STTech.BytesIO.Serial;

class Program
{
    static void Main(string[] args)
    {
        // 1. 初始化下行现场物理 RS-485 串口总线
        var rtuSerialClient = new SerialClient()
        {
            PortName = "COM3",
            BaudRate = 9600
        };
        rtuSerialClient.Connect();

        // 2. 建立上行 Modbus TCP 网关服务端，并直连下游客户端
        var gatewayServer = new ModbusTcpServer(ModbusProtocolFormat.RTU)
        {
            Port = 502,
            SlaveId = 1,                         // 1 号站为网关本体 (用于查看网关状态)
            DownstreamClient = rtuSerialClient   // 将非 1 号站的所有报文透明转发至该串口！
        };

        // 3. (可选) 如果不同站号分布在不同的物理串口或网络节点，可配置专属路由：
        // var serialCom4 = new SerialClient { PortName = "COM4", BaudRate = 19200 };
        // serialCom4.Connect();
        // gatewayServer.AddDownstreamNode(slaveId: 2, serialCom4); // 2 号从机走 COM4

        // 4. 挂载网关本体的虚拟监控区 (当上位机访问 1 号站时，直接由本机内存响应)
        var gatewayDevice = new VirtualModbusDevice(gatewayServer);
        var statusMonitor = new HoldingRegisterValueMonitor(startAddress: 0, length: 10);
        statusMonitor.Source[0] = 1; // 寄存器 40001: 网关运行状态 (1=正常)
        statusMonitor.Source[1] = (ushort)(rtuSerialClient.IsConnected ? 1 : 0); // 40002: 485 总线连接状态
        gatewayDevice.Mount(statusMonitor);

        // 5. 启动网关服务
        gatewayServer.Start();
        Console.WriteLine($"Modbus 工业透明网关已就绪！监听端口: 502, 网关站号: 1, 下游串口: COM3");
        Console.WriteLine("上位机访问站号 1 时由网关直接响应；访问其他站号自动透明透传至下游现场总线。");
        Console.ReadLine();

        gatewayServer.Stop();
        rtuSerialClient.Disconnect();
    }
}
```

---

## 5. 深入 Unpacker 与 MatchHandler 内核

### 5.1 响应解包器 (`ModbusUnpacker`) 的流式切包

Modbus 报文在 TCP 流或串口流式传输中会面临粘包（多包连发）和半包（分段到达）的问题。`ModbusUnpacker` 基于 `System.IO.Pipelines` 的零拷贝机制切分数据帧：

```csharp
protected override int CalculatePacketLength(ReadOnlySequence<byte> buffer)
```

1. **RTU 模式帧长判定**：
   - 先行校验头部 2 字节（从站地址与功能码）。
   - **异常帧**：若功能码最高位为 1（`code >= 0x80`），固定长度为 5 字节（从机 + 错误功能码 + 错误码 + CRC16 两字节）。
   - **读响应（0x01 ~ 0x04）**：第 3 字节为 `ByteCount`，总长度为 `3 + ByteCount + 2 (CRC)`。
   - **写响应（0x05, 0x06, 0x0F, 0x10）**：标准确认帧固定为 8 字节。
2. **ASCII 模式帧长判定**：
   - 依赖 `StartMark = ':'`（`0x3A`）作为帧头定位。
   - 搜索 `\r\n` 作为结束符，中间所有 ASCII 字符按十六进制对还原为真实字节，并做 LRC 校验。

### 5.2 报文精准匹配机制 (`ModbusClient.ReplyMatchHandle`)

在异步高并发或全双工网络中，主站可能会快速连续发送多条 Modbus 请求。主站如何确定当前收到的应答属于哪一个请求？

`STTech.BytesIO.Modbus` 实现了严密的匹配校验链：

```csharp
private bool ReplyMatchHandle(ModbusRequest req, ModbusResponse resp)
```

```
[请求发送] ──► 保存到待回复队列 (附带 ReplyMatchHandle 回调)
                 │
[响应到达] ──► 1. 比对从站地址: req.SlaveId == resp.SlaveId
                 │
             2. 比对功能码: req.FunctionCode == resp.FunctionCode
                 │
             3. 异常帧分支: !resp.IsSuccess ──► 直接匹配放行
                 │
             4. 读操作长度校验:
                - 读线圈 (0x01/0x02): (req.Length + 7) / 8 == resp.Length
                - 读寄存器 (0x03/0x04): req.Length == resp.Length / 2
                 │
             5. 写操作回显校验:
                - 写单线圈 (0x05): req.WriteAddress == resp.WriteAddress 
                                 && resp.GetUInt16() == (req.Data ? 0xFF00 : 0x0000)
                - 写单保持 (0x06): req.WriteAddress == resp.WriteAddress 
                                 && resp.Values == req.Data
                - 写多线圈 (0x0F): req.WriteAddress == resp.WriteAddress 
                                 && resp.GetUInt16() == req.Data.Length
                - 写多保持 (0x10): req.WriteAddress == resp.WriteAddress 
                                 && resp.GetUInt16() == req.Data.Length / 2
```

> [!NOTE]
> 正是基于这一层严苛的校验，`STTech.BytesIO.Modbus` 能在恶劣、高干扰、网络延时的现场环境下杜绝**“张冠李戴”串包**现象。如果从机因故障返回了长度不对或回显不符的报文，该报文会被丢弃，客户端将在超时后安全报错，而不会将错误数据当作正确结果返回业务层。

---

## 6. 扩展开发：自定义 Modbus 请求与私有功能码

某些厂商设备支持非标功能码（例如：`0x17` 读写多个寄存器、`0x41` 写入设备序列号与出厂校准参数）。你可以通过派生 `ModbusRequest` 与 `ModbusResponse` 轻松扩展。

### 6.1 编写自定义功能码请求实体

```csharp
using System;
using System.Collections.Generic;
using STTech.BytesIO.Modbus;

// 假设定义私有功能码 0x41：设置设备标定系数
public class SetCalibrationRequest : ModbusRequest
{
    public float CalibrationFactor { get; set; } // 标定浮点数

    public SetCalibrationRequest() : base((FunctionCode)0x41) { }

    protected override void SerializePayloadHandle()
    {
        // 将浮点数转换为大端 4 字节负载
        byte[] factorBytes = BitConverter.GetBytes(CalibrationFactor);
        Array.Reverse(factorBytes); // 大端序

        Payload = factorBytes;
    }
}
```

### 6.2 编写对应的响应实体

```csharp
using STTech.BytesIO.Modbus;

public class SetCalibrationResponse : ModbusResponse
{
    public bool IsCalibrated { get; private set; }

    public SetCalibrationResponse() { }

    public SetCalibrationResponse(byte[] data) : base(data)
    {
        if (IsSuccess && Payload != null && Payload.Length >= 1)
        {
            IsCalibrated = Payload[0] == 0x01;
        }
    }
}
```

### 6.3 发送并获取强类型回复

```csharp
var customReq = new SetCalibrationRequest
{
    SlaveId = 1,
    CalibrationFactor = 1.052f
};

// 调用通用的 SendModbusRequest<T>
var reply = client.SendModbusRequest<SetCalibrationResponse>(customReq, timeout: 2000);

if (reply.IsCompleted && reply.GetResponse().IsSuccess)
{
    Console.WriteLine($"标定系数写入成功，设备状态: {reply.GetResponse().IsCalibrated}");
}
```

---

## 7. 工业高可靠性最佳实践

### 7.1 自动断线重连

在工厂现场，受电磁干扰或网线震动影响，TCP 连接不可避免会发生瞬断。推荐配合 `STTech.BytesIO` 的重连机制：

```csharp
var client = new ModbusTcpClient()
{
    Host = "192.168.1.50",
    Port = 502
};

// 开启自动重连 (每 3 秒检测并尝试重新连线)
client.OnDisconnected += async (s, e) =>
{
    Console.WriteLine("Modbus 连接意外中断，5 秒后尝试重连...");
    await Task.Delay(5000);
    var res = await client.ConnectAsync();
    if (res.IsSuccess)
    {
        Console.WriteLine("重连成功，恢复监控！");
    }
};
```

### 7.2 多线程并发与防串包：深入理解 Unpacker + ReplyMatchHandler 的原生优势

在传统 Modbus 库开发中，多线程并发调用（例如：主界面线程正在刷新线圈，后台轮询线程同时读取寄存器）是一个极其棘手的痛点：
- **传统库的弊端**：传统库通常采用同步的“发送字节 -> 立即等待读取字节”逻辑。一旦两个线程同时发起请求，后发请求的响应往往会被先发请求的线程误读，导致严重的**数据错乱与串包（Crosstalk）**。因此开发者不得不被迫在外部手写全局锁（`lock` 或 `SemaphoreSlim`）来包装客户端。
- **STTech.BytesIO 的原生解决方案**：在 `STTech.BytesIO.Modbus` 中，**完全不需要编写任何外部互斥锁或包装类**，库内部的“发送泵 + 解包器 + 匹配器”三层闭环架构已经从根本上原生解决了并发防冲突与防串包问题：

```
线程 A ──► client.ReadCoilRegister(...)            ──┐
                                                    ├──► [内部 asyncSendQueue 发送泵] ──► 严格按帧完整发送
线程 B ──► client.ReadHoldingRegister(...)        ──┘
                                                                                               │
                                                                                       [物理通道传输]
                                                                                               │
线程 A ◄── [ReplyMatchHandle 精准分发: 功能码 0x01] ◄──┐                                        ▼
                                                       ├── [ModbusUnpacker 零拷贝按帧切包] ◄── [设备返回]
线程 B ◄── [ReplyMatchHandle 精准分发: 功能码 0x03] ◄──┘
```

1. **发送端保序排队（`asyncSendQueue`）**：
   底层 `BytesClient` 内置了高吞吐的并发安全队列与无锁发送泵。即使多线程同时调用发送，各请求的数据帧也会被完整保序地排队送往底层，绝对不会出现字节交错或半包穿插。
2. **接收端严格分帧（`ModbusUnpacker`）**：
   无论总线返回的数据是以何种分段到达，`ModbusUnpacker` 都会依据 Modbus 协议规约（CRC16/LRC 校验与长度规则）提取出合法的独立数据帧。
3. **多路匹配与安全回传（`ReplyMatchHandle`）**：
   当任何一个响应帧到达时，内核会将该帧广播给所有正在等待回复的挂起任务。每个任务的 `ReplyMatchHandle` 会严密比对：
   - 从机地址 `SlaveId` 是否一致；
   - 功能码 `FunctionCode` 是否一致；
   - 请求寄存器地址与写入回显/读取长度是否完全契合。
   只有完全匹配的那个任务才会完成并返回结果，其余任务继续等待属于自己的响应。

> [!TIP]
> 因此，在 `STTech.BytesIO.Modbus` 中，你可以放心地在多个异步任务或后台线程中直接调用 `client` 的读写 API，无需编写任何多余的锁或外部包装代码。直接面向业务开发，享受简洁统一的现代异步体验！

### 7.3 高频采样下的垃圾回收 (GC) 优化

- **复用请求实体**：如果在 100ms 高频周期轮询同一个寄存器区间，不要在 `while` 循环内部频繁 `new ReadHoldingRegisterRequest()`，建议在初始化时构造一次请求对象并复用它。
- **使用原生数组**：调用 `response.Values` 获取只读字节数组，尽量避免在循环体内高频使用 LINQ 生成中间临时的集合对象。

---

## 8. 总结

`STTech.BytesIO.Modbus` 既为日常的基础 PLC 读写提供了极简友好的 API，也为复杂的工业上位机与网关系统提供了强大底层的扩展支持：
- 基础功能：直接利用 `ModbusTcpClient` / `ModbusSerialClient` 配合 8 大功能码 API。
- 从站与仿真：利用 `ModbusTcpServer` + `VirtualModbusDevice` + `RegisterValueMonitor` 轻松搭建 PLC 内存镜像模型。
- 网关与透传：基于 `ModbusRequestUnpacker.IsLocalSlaveId` 实现智能协议转换与地址过滤。
- 高可用性：基于 Unpacker + ReplyMatchHandler 原生解决并发防串包，结合自动断线重连，构建工业级高可靠通信基石。
