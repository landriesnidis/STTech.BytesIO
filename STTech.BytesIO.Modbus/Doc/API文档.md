# STTech.BytesIO.Modbus 类库 API 技术手册

本手册基于 Microsoft 技术文档规范编写，详细介绍了 **STTech.BytesIO.Modbus** 类库的整体架构、API 接口、枚举定义及典型应用场景。

- **命名空间**：`STTech.BytesIO.Modbus`、`STTech.BytesIO.Modbus.Entity`、`STTech.BytesIO.Modbus.Monitors`
- **程序集**：`STTech.BytesIO.Modbus.dll`
- **依赖框架**：`.NET Standard 2.0` / `.NET Framework 4.6.1+` / `.NET Core 2.0+`
- **核心依赖**：`STTech.BytesIO.Core`

---

## 目录

1. [架构概述](#1-架构概述)
2. [枚举参考规范](#2-枚举参考规范)
   - [ModbusProtocolFormat](#modbusprotocolformat)
   - [FunctionCode](#functioncode)
   - [ModbusErrorCode](#modbuserrorcode)
   - [ModbusRegisterType](#modbusregistertype)
3. [核心类库 API 参考](#3-核心类库-api-参考)
   - [ModbusClient (主站基类)](#modbusclient-主站基类)
   - [ModbusTcpClient](#modbustcpclient)
   - [ModbusSerialClient](#modbusserialclient)
   - [ModbusServer (从站基类)](#modbusserver-从站基类)
   - [ModbusTcpServer](#modbustcpserver)
   - [ModbusRtuServer](#modbusrtuserver)
   - [VirtualModbusDevice (虚拟设备模拟器)](#virtualmodbusdevice-虚拟设备模拟器)
   - [RegisterValueMonitor (寄存器数值监视器)](#registervaluemonitor-寄存器数值监视器)
4. [报文实体参考 (Request & Response)](#4-报文实体参考-request--response)
5. [解包组件参考 (Unpacker)](#5-解包组件参考-unpacker)
6. [开发指南与典型应用场景](#6-开发指南与典型应用场景)

---

## 1. 架构概述

`STTech.BytesIO.Modbus` 是一个基于管道技术（`System.IO.Pipelines`）的高性能 Modbus 协议实现库。该类库将 Modbus 协议层与底层通信传输介质（TCP、串口等）进行解耦，实现了通信层与协议层的无缝挂载。

其主要模块关系如下：

```mermaid
graph TD
    subgraph Core Communication Layer (STTech.BytesIO)
        TcpClient[TcpClient]
        SerialClient[SerialClient]
        TcpServer[TcpServer]
    end

    subgraph Modbus Protocol Layer
        ModbusClient[ModbusClient 基类]
        ModbusTcpClient[ModbusTcpClient]
        ModbusSerialClient[ModbusSerialClient]
        
        ModbusServer[ModbusServer 基类]
        ModbusTcpServer[ModbusTcpServer]
        ModbusRtuServer[ModbusRtuServer]
        
        Unpacker[ModbusUnpacker / RequestUnpacker]
    end

    subgraph Application Helpers
        VirtualDevice[VirtualModbusDevice 虚拟从站]
        Monitors[RegisterValueMonitor 寄存器监视器]
    end

    ModbusTcpClient -->|继承| ModbusClient
    ModbusSerialClient -->|继承| ModbusClient
    ModbusClient -->|关联绑定| TcpClient
    ModbusClient -->|关联绑定| SerialClient
    ModbusClient -->|装配解包器| Unpacker

    ModbusTcpServer -->|继承| ModbusServer
    ModbusRtuServer -->|继承| ModbusServer
    ModbusTcpServer -->|关联| TcpServer
    
    VirtualDevice -->|绑定驱动| ModbusServer
    VirtualDevice -->|挂载数据区| Monitors
```

---

## 2. 枚举参考规范

### ModbusProtocolFormat
定义 Modbus 报文在物理链路上的编码传输格式。

- **命名空间**：`STTech.BytesIO.Modbus`
- **语法**：`public enum ModbusProtocolFormat`

| 成员名称 | 值 | 描述 |
| :--- | :---: | :--- |
| `RTU` | `0` | **RTU 格式**：使用二进制编码。帧间以至少 3.5 个字符的空闲时间进行分隔。末尾采用 CRC-16 校验码。 |
| `ASCII` | `1` | **ASCII 格式**：使用可见字符（HEX ASCII 编码）进行传输。数据帧以冒号 `:`（0x3A）开头，以回车换行符 `\r\n`（0x0D 0x0A）结尾。采用 LRC 校验码。 |

---

### FunctionCode
定义 Modbus 协议标准支持的功能码。

- **命名空间**：`STTech.BytesIO.Modbus`
- **语法**：`public enum FunctionCode : byte`

| 成员名称 | 十进制值 | 十六进制值 | 对应寄存器类型 | 访问属性 | 描述 |
| :--- | :---: | :---: | :--- | :---: | :--- |
| `ReadCoilRegister` | `1` | `0x01` | 线圈寄存器 (Coils) | 读/写 | 读取单个或多个线圈寄存器的状态值。 |
| `ReadDiscreteInputRegister` | `2` | `0x02` | 离散输入寄存器 (Discrete Inputs) | 只读 | 读取单个或多个离散输入状态值。 |
| `ReadHoldingRegister` | `3` | `0x03` | 保持寄存器 (Holding Registers) | 读/写 | 读取一个或多个保持寄存器的二进制数值。 |
| `ReadInputRegister` | `4` | `0x04` | 输入寄存器 (Input Registers) | 只读 | 读取一个或多个模拟输入寄存器的数值。 |
| `WriteSingleCoilRegister` | `5` | `0x05` | 线圈寄存器 (Coils) | 读/写 | 写入单个线圈寄存器的状态（ON/OFF）。 |
| `WriteSingleHoldingRegister` | `6` | `0x06` | 保持寄存器 (Holding Registers) | 读/写 | 写入单个保持寄存器的数值。 |
| `WriteMultipleCoilRegisters` | `15` | `0x0F` | 线圈寄存器 (Coils) | 读/写 | 批量写入多个线圈寄存器的状态值。 |
| `WriteMultipleHoldingRegisters` | `16` | `0x10` | 保持寄存器 (Holding Registers) | 读/写 | 批量写入多个连续保持寄存器的数据值。 |

---

### ModbusErrorCode
Modbus 协议定义的标准异常错误码（当从站无法处理请求时向主站返回）。

- **命名空间**：`STTech.BytesIO.Modbus`
- **语法**：`public enum ModbusErrorCode`

| 成员名称 | 值 | 含义说明 | 详细解释与产生场景 |
| :--- | :---: | :--- | :--- |
| `NoError` | `0x00` | 没有错误 | 正常响应状态，指示该操作已成功执行。 |
| `IllegalFunction` | `0x01` | 非法功能码 | 接收到的功能码不被该从设备支持或允许。通常发生在设备不支持该指令或处于限制模式时。 |
| `IllegalDataAddress` | `0x02` | 非法数据地址 | 请求的寄存器起始地址不在设备的合法寻址范围内，或者读取的地址范围超出了寄存器区上限。 |
| `IllegalDataValue` | `0x03` | 非法数据值 | 请求报文中的数据内容或结构不符合要求。例如：读取长度为0，或者写入的数据格式与寄存器类型冲突。 |
| `SlaveDeviceFailure` | `0x04` | 从设备故障 | 从站在试图执行所请求的操作时发生了不可恢复的内部硬件/软件错误。 |
| `Acknowledge` | `0x05` | 应答 | 告知主站，从设备已接收到请求并正在处理，但需要较长的时间才能完成。为防止主站发生超时溢出，从站先发出此响应。 |
| `SlaveDeviceBusy` | `0x06` | 从设备忙 | 从站正在执行一个耗时较长的后台命令。主站应当在稍后重新发送请求报文。 |
| `MemoryParityError` | `0x08` | 存储器奇偶校验错 | 设备在尝试读取存储器（如 EEPROM / Flash）时，检测到了奇偶校验错误，代表内部存储数据已损坏。 |
| `GatewayPathUnavailable` | `0x0A` | 网关路径不可用 | Modbus 网关专用。表示网关内部的路由通道配置错误，或者连接目标子网的桥接断开。 |
| `GatewayTargetDeviceFailedToRespond` | `0x0B` | 网关目标设备未响应 | Modbus 网关专用。网关能够访问，但是在转发请求至后端的子网设备（如底层仪表）时，该从站设备未能在超时时间内响应网关。 |

---

### ModbusRegisterType
标识 Modbus 寄存器的存储区类型。

- **命名空间**：`STTech.BytesIO.Modbus.Monitors`
- **语法**：`public enum ModbusRegisterType`

| 成员名称 | 物理区划分 | 数据格式 | 默认地址范围(常规) | 读写属性 | 描述 |
| :--- | :---: | :---: | :---: | :---: | :--- |
| `Coil` | `0X` | `bool` (1 bit) | `00001 - 09999` | 读/写 | 输出线圈。控制继电器、开关、警报指示灯状态。 |
| `DiscreteInput` | `1X` | `bool` (1 bit) | `10001 - 19999` | 只读 | 离散物理输入。如按钮开关、传感器限位检测状态。 |
| `Input` | `3X` | `ushort` (16 bit) | `30001 - 39999` | 只读 | 模拟量输入寄存器。如物理温度、压力传感器的当前采样值。 |
| `Holding` | `4X` | `ushort` (16 bit) | `40001 - 49999` | 读/写 | 保持寄存器。用于配置参数、报警上下限设置、模拟量输出控制。 |

---

## 3. 核心类库 API 参考

### ModbusClient (主站基类)
Modbus 主站客户端的基类，定义了向从站发起读写请求的统一方法和接收事件。

- **继承关系**：`VirtualClient` -> `IUnpackerSupport<ModbusResponse>` -> `ModbusClient`

#### 属性 (Properties)
* `ProtocolFormat` (`ModbusProtocolFormat`): 获取或设置当前的 Modbus 协议格式（RTU 或 ASCII）。更改此属性会自动更新内部解包器 (`ModbusUnpacker.Format`) 的解码格式。
* `Unpacker` (`Unpacker<ModbusResponse>`): 关联的响应包解包器实例。

#### 方法 (Methods)
所有读写方法均支持同步阻塞调用，若需要在异步上下文中使用，可配合 `Task.Run()` 运行。

##### 1. SendModbusRequest
发送自定义 Modbus 请求并等待响应。
```csharp
public Reply<T> SendModbusRequest<T>(ModbusRequest request, int timeout = 3000, SendOptions options = null) where T : ModbusResponse;
```

##### 2. ReadCoilRegister (0x01)
读取从站的线圈状态值。
```csharp
public Reply<ReadCoilRegisterResponse> ReadCoilRegister(byte slaveId, ushort startAddress, ushort length, int timeout = 3000, SendOptions options = null);
public Reply<ReadCoilRegisterResponse> ReadCoilRegister(ReadCoilRegisterRequest request, int timeout = 3000, SendOptions options = null);
```

##### 3. ReadDiscreteInputRegister (0x02)
读取从站离散输入寄存器的状态。
```csharp
public Reply<ReadDiscreteInputRegisterResponse> ReadDiscreteInputRegister(byte slaveId, ushort startAddress, ushort length, int timeout = 3000, SendOptions options = null);
public Reply<ReadDiscreteInputRegisterResponse> ReadDiscreteInputRegister(ReadDiscreteInputRegisterRequest request, int timeout = 3000, SendOptions options = null);
```

##### 4. ReadHoldingRegister (0x03)
读取从站保持寄存器的数值。
```csharp
public Reply<ReadHoldingRegisterResponse> ReadHoldingRegister(byte slaveId, ushort startAddress, ushort length, int timeout = 3000, SendOptions options = null);
public Reply<ReadHoldingRegisterResponse> ReadHoldingRegister(ReadHoldingRegisterRequest request, int timeout = 3000, SendOptions options = null);
```

##### 5. ReadInputRegister (0x04)
读取模拟量输入寄存器的数值。
```csharp
public Reply<ReadInputRegisterResponse> ReadInputRegister(byte slaveId, ushort startAddress, ushort length, int timeout = 3000, SendOptions options = null);
public Reply<ReadInputRegisterResponse> ReadInputRegister(ReadInputRegisterRequest request, int timeout = 3000, SendOptions options = null);
```

##### 6. WriteSingleCoilRegister (0x05)
写入单个线圈寄存器的布尔值。
```csharp
public Reply<WriteRegisterResponse> WriteSingleCoilRegister(byte slaveId, ushort writeAddress, bool data, int timeout = 3000, SendOptions options = null);
public Reply<WriteRegisterResponse> WriteSingleCoilRegister(WriteSingleCoilRegisterRequest request, int timeout = 3000, SendOptions options = null);
```

##### 7. WriteSingleHoldingRegister (0x06)
写入单个保持寄存器（支持字节数组或 `ushort` 格式）。
```csharp
public Reply<WriteRegisterResponse> WriteSingleHoldingRegister(byte slaveId, ushort writeAddress, byte[] data, int timeout = 3000, SendOptions options = null);
public Reply<WriteRegisterResponse> WriteSingleHoldingRegister(byte slaveId, ushort writeAddress, ushort value, int timeout = 3000, SendOptions options = null);
public Reply<WriteRegisterResponse> WriteSingleHoldingRegister(WriteSingleHoldingRegisterRequest request, int timeout = 3000, SendOptions options = null);
```

##### 8. WriteMultipleCoilRegisters (0x0F)
批量写入多个连续线圈的状态。
```csharp
public Reply<WriteRegisterResponse> WriteMultipleCoilRegisters(byte slaveId, ushort writeAddress, bool[] data, int timeout = 3000, SendOptions options = null);
public Reply<WriteRegisterResponse> WriteMultipleCoilRegisters(WriteMultipleCoilRegistersRequest request, int timeout = 3000, SendOptions options = null);
```

##### 9. WriteMultipleHoldingRegisters (0x10)
批量写入多个连续保持寄存器的数值。
```csharp
public Reply<WriteRegisterResponse> WriteMultipleHoldingRegisters(byte slaveId, ushort writeAddress, ushort[] values, int timeout = 3000, SendOptions options = null);
public Reply<WriteRegisterResponse> WriteMultipleHoldingRegisters(byte slaveId, ushort writeAddress, byte[] data, int timeout = 3000, SendOptions options = null);
public Reply<WriteRegisterResponse> WriteMultipleHoldingRegisters(WriteMultipleHoldingRegistersRequest request, int timeout = 3000, SendOptions options = null);
```

#### 事件 (Events)
* `OnModbusPacketReceived`: 当收到任意 Modbus 响应数据包时触发。
* `OnReadCoilRegisterPacketReceived`: 当接收到功能码 `0x01`（读线圈）响应时触发。
* `OnReadDiscreteInputRegisterPacketReceived`: 当接收到功能码 `0x02`（读离散输入）响应时触发。
* `OnReadHoldingRegisterPacketReceived`: 当接收到功能码 `0x03`（读保持寄存器）响应时触发。
* `OnReadInputRegisterPacketReceived`: 当接收到功能码 `0x04`（读输入寄存器）响应时触发。
* `OnWriteSingleCoilRegisterPacketReceived`: 当接收到功能码 `0x05`（写单线圈）响应时触发。
* `OnWriteSingleHoldingRegisterPacketReceived`: 当接收到功能码 `0x06`（写单保持寄存器）响应时触发。
* `OnWriteMultipleCoilRegistersPacketReceived`: 当接收到功能码 `0x0F`（写多线圈）响应时触发。
* `OnWriteMultipleHoldingRegistersPacketReceived`: 当接收到功能码 `0x10`（写多保持寄存器）响应时触发。

---

### ModbusTcpClient
Modbus TCP 通信主站客户端。

- **继承关系**：`ModbusClient<TcpClient>` -> `ModbusTcpClient`
- **主要接口**：`ITcpClient`

#### 构造函数
```csharp
public ModbusTcpClient(ModbusProtocolFormat format);
```
> [!NOTE]
> 必须传递 `ModbusProtocolFormat` 参数指定报文协议格式（通常在 Modbus TCP 下仍将载荷解析为 RTU 的二进制格式）。

#### 常用属性
* `Host` (`string`): 目标 Modbus TCP 服务端的 IP 地址。
* `Port` (`int`): 服务端端口号（Modbus TCP 默认标准端口是 `502`）。
* `LocalEndPoint` (`IPEndPoint`): 本地绑定的网卡 IP 地址与端口端点。
* `RemoteEndPoint` (`IPEndPoint`): 连接建立后，返回的远程从站终结点。
* `InnerClient` (`TcpClient`): 关联的底层 `TcpClient` 套接字连接对象。

---

### ModbusSerialClient
Modbus 串口通信客户端。通常配合物理串口屏、变频器、温控表等仪表使用。

- **继承关系**：`ModbusClient<SerialClient>` -> `ModbusSerialClient`
- **主要接口**：`ISerialClient`

#### 构造函数
```csharp
public ModbusSerialClient(ModbusProtocolFormat format);
```

#### 常用属性
封装了 `System.IO.Ports.SerialPort` 常用串口控制属性：
* `PortName` (`string`): 物理串口号（如 `"COM1"`，Windows）或虚拟串口设备路径（Linux 如 `"/dev/ttyS0"`）。
* `BaudRate` (`int`): 波特率（常见有 `9600`、`115200` 等）。
* `Parity` (`Parity`): 奇偶校验位（`None`, `Odd`, `Even` 等）。
* `DataBits` (`int`): 数据位长度（一般为 `8`）。
* `StopBits` (`StopBits`): 停止位（`One`, `Two` 等）。
* `ReadTimeout` / `WriteTimeout` (`int`): 串口读写底层超时等待时长（毫秒）。

---

### ModbusServer (从站基类)
用于构建 Modbus 从站设备、网关或模拟服务器的基类。支持在本地模拟从站内存数据，或者透明代理转发非本站号报文。

#### 属性 (Properties)
* `SlaveId` (`byte`): 本机从站站号，默认为 `1`。当上游主站请求的站号等于该值时，触发本机的读取/写入请求事件。
* `DownstreamClient` (`BytesClient`): 下游通信物理通道客户端。当收到非本机 `SlaveId` 的查询报文时，如果绑定了此物理客户端，请求将透明地自动转发至该客户端。
* `DownstreamNodes` (`Dictionary<byte, BytesClient>`): 站号到下游通信客户端的静态映射表。支持细粒度的多路从站代理转发。

#### 方法 (Methods)
* `AddDownstreamNode(byte slaveId, BytesClient client)`: 向映射表中添加指定站号路由的下游客户端。
* `RemoveDownstreamNode(byte slaveId)`: 移除指定路由配置。

#### 读取事件 (Read Requested Events)
当接收到针对本机站号的读请求时触发，需要在事件处理逻辑中为 `ModbusReadRequestedEventArgs<T>.ResponseData` 赋值：
* `ReadCoilRegisterRequested`: 读取线圈请求事件。回调参数为 `ModbusReadRequestedEventArgs<bool[]>`。
* `ReadDiscreteInputRegisterRequested`: 读取离散输入请求事件。回调参数为 `ModbusReadRequestedEventArgs<bool[]>`。
* `ReadHoldingRegisterRequested`: 读取保持寄存器请求事件。回调参数为 `ModbusReadRequestedEventArgs<byte[]>`。
* `ReadInputRegisterRequested`: 读取输入寄存器请求事件。回调参数为 `ModbusReadRequestedEventArgs<byte[]>`。

#### 写入事件 (Write Requested Events)
当接收到针对本机站号的写请求时触发，处理本地内存值的变更逻辑：
* `WriteSingleCoilRegisterRequested`: 写入单线圈。
* `WriteSingleHoldingRegisterRequested`: 写入单保持寄存器。
* `WriteMultipleCoilRegistersRequested`: 写入多个线圈。
* `WriteMultipleHoldingRegistersRequested`: 写入多个保持寄存器。

---

### ModbusTcpServer
Modbus TCP 从站模拟服务或透明转发网关。可以同时接收多个 TCP 连接。

- **继承关系**：`ModbusServer` -> `ModbusTcpServer`

#### 构造函数
```csharp
public ModbusTcpServer(ModbusProtocolFormat format = ModbusProtocolFormat.RTU);
```

#### 方法
* `Start()`: 开启侦听，启动 TCP 服务端。
* `Stop()`: 关闭侦听，中断所有客户端套接字。

#### 常用属性
* `Port` (`int`): 服务监听绑定的本地端口。
* `Clients` (`IEnumerable<TcpClient>`): 获取当前所有已建立连接的主站客户端。

---

### ModbusRtuServer
Modbus 串口（RTU 或 ASCII 编码）从站模拟服务或物理路由网关。

- **继承关系**：`ModbusServer` -> `ModbusRtuServer`

#### 构造函数
```csharp
public ModbusRtuServer(ModbusProtocolFormat format = ModbusProtocolFormat.RTU);
```

#### 常用属性
* `PortName` (`string`): 绑定的物理串口号。
* `BaudRate` (`int`): 通信波特率。

---

### VirtualModbusDevice (虚拟设备模拟器)
虚拟 Modbus 物理设备，是快速开发上位机测试程序或本地运行仿真设备的核心工具。它能够与 `ModbusServer`（如 `ModbusTcpServer` / `ModbusRtuServer`）深度绑定，自动接管读写请求，将事件数据转发到挂载的寄存器监视数据区。

#### 构造函数
```csharp
public VirtualModbusDevice(ModbusServer server);
```

#### 方法
* `Mount(IModbusRegister register)`: 挂载寄存器内存数据区（可以使用 `HoldingRegisterValueMonitor` 等监视器实例）。
* `Unmount(IModbusRegister register)`: 卸载指定的数据区。

---

### RegisterValueMonitor (寄存器数值监视器)
封装了连续寄存器物理内存映射区，支持监测指定偏移地址的值变更。

#### 强类型接口 `IModbusRegister<T>`
```csharp
public interface IModbusRegister<T> : IModbusRegister where T : struct
{
    T[] Source { get; }  // 本地映射数组数据源
    void Notify();       // 触发数值改变通知事件
}
```

#### 派生实现类
1. **CoilRegisterValueMonitor**: 布尔型线圈寄存器数据区。
2. **DiscreteInputRegisterValueMonitor**: 只读离散输入数据区。
3. **InputRegisterValueMonitor**: 只读 16 位整型输入寄存器数据区。
4. **HoldingRegisterValueMonitor**: 读写 16 位整型保持寄存器数据区。

#### 示例：初始化与事件订阅
```csharp
// 创建一个起始地址为 1000，大小为 50 个保持寄存器的连续内存映射区
var monitor = new HoldingRegisterValueMonitor(1000, 50);

// 订阅值改变事件
monitor.ValueChanged += (sender, e) =>
{
    // e.Index：相对偏移下标（0代表地址1000，1代表地址1001）
    // e.OriginalValue：旧值 (ushort)
    // e.NewValue：新值 (ushort)
    Console.WriteLine($"地址 {monitor.StartAddress + e.Index} 发生数据更新: {e.OriginalValue} -> {e.NewValue}");
};
```

---

## 4. 报文实体参考 (Request & Response)

### ModbusRequest (基类)
* `SlaveId` (`byte`): 从机地址。
* `FunctionCode` (`FunctionCode`): 请求的功能码。
* `ProtocolFormat` (`ModbusProtocolFormat`): 协议类型。
* `GetBytes()`: 序列化返回可以直接发送的原始字节数组（自动计算 CRC 或 LRC 校验码）。

#### 常用请求实体派生类
* **ReadRegisterRequest**: 通用读取寄存器请求。包含 `StartAddress`（起始地址）和 `Length`（寄存器数量）。
* **WriteSingleCoilRegisterRequest**: 单个线圈写入请求。包含 `WriteAddress` 和布尔值 `Data`。
* **WriteSingleHoldingRegisterRequest**: 单个保持寄存器写入请求。包含 `WriteAddress` 和 `byte[]` 格式的 `Data`。
* **WriteMultipleCoilRegistersRequest**: 批量线圈写入请求。包含 `WriteAddress` 和 `bool[]` 格式的 `Data`。
* **WriteMultipleHoldingRegistersRequest**: 批量保持寄存器写入请求。包含 `WriteAddress` 和 `byte[]` 格式的 `Data`。
* **ModbusForwardRequest**: 透明转发路由报文，不对外解析载荷，包含完整的 `RawBytes` 数据。

---

### ModbusResponse (基类)
* `SlaveId` (`ushort`): 响应的从站站号。
* `FunctionCode` (`FunctionCode`): 响应的功能码。
* `IsSuccess` (`bool`): 表示操作是否成功。
* `ErrorCode` (`ModbusErrorCode`): 若 `IsSuccess` 为 `false`，则返回从站返回的错误原因码。

> [!WARNING]
> 若从站发生了异常回复（例如地址越界返回 `0x02` 错误），调用 `ModbusResponse.Payload` 或子类的 `Values` 时会抛出异常：
> `InvalidOperationException: Unable to acquire payload. (ErrorCode = IllegalDataAddress)`
> **强烈建议：在访问响应对象的数值属性前，必须首先判断 `IsSuccess` 属性是否为 `true`。**

#### 常用响应实体派生类
* **ReadCoilRegisterResponse**: 线圈读取响应。包含 `Values` (`bool[]`)。
* **ReadDiscreteInputRegisterResponse**: 离散输入读取响应。包含 `Values` (`bool[]`)。
* **ReadHoldingRegisterResponse**: 保持寄存器读取响应。包含 `Values` (`byte[]`)。可以调用 `GetUInt16Array()` 直接转换为 `ushort[]`。
* **ReadInputRegisterResponse**: 模拟输入读取响应。包含 `Values` (`byte[]`)。可以调用 `GetUInt16Array()` 直接转换为 `ushort[]`。
* **WriteRegisterResponse**: 写入响应。包含 `WriteAddress`（写入起始地址）和 `Values`（从机确认回传的数值字节数组）。可以调用 `GetUInt16()` 来获取写入的数值或数量。

---

## 5. 解包组件参考 (Unpacker)

`STTech.BytesIO` 支持底层的零拷贝流解析。在 Modbus 实现中，通过解包器机制来确保完整报文的数据帧拆包，有效解决通信时的粘包和断包问题。

* **ModbusUnpacker**: 用于主站（Client），对接收到的响应（Response）根据功能码动态计算报文长度，封装成 `ModbusResponse` 传回给上层逻辑。
* **ModbusRequestUnpacker**: 用于从站（Server），对主站发送过来的请求（Request）进行解析。
  - 特有属性 `IsLocalSlaveId` (`Func<byte, bool>`): 委托判定回调。若传入的 SlaveId 返回为 `false`，则说明是非本机请求，该解包器会自动将其装配为 `ModbusForwardRequest` 供网关代理转发，不耗费性能进行功能码深层映射解析。

---

## 6. 开发指南与典型应用场景

### 场景一：Modbus TCP 主站客户端读写
最常用的场景：连接到 PLC 或者工业网关，周期性读取保持寄存器。

```csharp
using System;
using System.Threading.Tasks;
using STTech.BytesIO.Core;
using STTech.BytesIO.Modbus;

namespace ModbusDemo
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // 1. 创建 Modbus TCP 客户端，并指定格式为 RTU payload
            var client = new ModbusTcpClient(ModbusProtocolFormat.RTU)
            {
                Host = "192.168.1.100",
                Port = 502
            };

            // 订阅通信层事件
            client.OnExceptionOccurs += (s, e) => Console.WriteLine($"[异常] 通信异常: {e.Exception.Message}");
            client.OnDisconnected += (s, e) => Console.WriteLine("[断开] 与从站断开连接");

            // 2. 建立网络连接
            Console.WriteLine("正在连接设备...");
            var connResult = client.Connect();
            if (!connResult.IsSuccess)
            {
                Console.WriteLine($"连接失败: {connResult.ErrorCode}");
                return;
            }
            Console.WriteLine("连接成功！");

            try
            {
                // 3. 读取保持寄存器 (站号: 1, 起始地址: 0, 读取长度: 5)
                // 由于 API 为同步阻塞，使用 Task.Run 包装以防阻塞 UI 或主线程
                var reply = await Task.Run(() => client.ReadHoldingRegister(slaveId: 1, startAddress: 0, length: 5));

                if (reply.Status == ReplyStatus.Completed)
                {
                    ReadHoldingRegisterResponse response = reply.GetResponse();
                    if (response.IsSuccess)
                    {
                        // 获取经过大小端转换的 ushort 数组
                        ushort[] registerValues = response.GetUInt16Array();
                        for (int i = 0; i < registerValues.Length; i++)
                        {
                            Console.WriteLine($"寄存器 {i} 数值: {registerValues[i]}");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"从站回复错误，错误码: {response.ErrorCode}");
                    }
                }
                else
                {
                    Console.WriteLine($"通信失败，ReplyStatus: {reply.Status}");
                }

                // 4. 写入单个保持寄存器 (将地址 2 的值设置为 123)
                var writeReply = await Task.Run(() => client.WriteSingleHoldingRegister(slaveId: 1, writeAddress: 2, value: 123));
                if (writeReply.Status == ReplyStatus.Completed && writeReply.GetResponse().IsSuccess)
                {
                    Console.WriteLine("写入地址 2 成功！");
                }
            }
            finally
            {
                // 5. 断开连接并释放
                client.Disconnect();
                client.Dispose();
            }
        }
    }
}
```

---

### 场景二：创建虚拟从站并监测数值改变
在开发 HMI 屏或者主站系统时，需要模拟一个从站供外部系统读写。本库支持通过挂载 ValueMonitor 驱动虚拟从站快速响应。

```csharp
using System;
using System.Threading;
using STTech.BytesIO.Modbus;
using STTech.BytesIO.Modbus.Monitors;

namespace ModbusSlaveSimulator
{
    class Program
    {
        static void Main(string[] args)
        {
            // 1. 初始化 TCP 服务端并设置本地从站站号
            var server = new ModbusTcpServer(ModbusProtocolFormat.RTU)
            {
                Port = 502,
                SlaveId = 1
            };

            // 监听客户端连接
            server.ClientConnected += (s, e) => Console.WriteLine($"[服务器] 主站客户端已连接: {e.Socket.RemoteEndPoint}");

            // 2. 初始化寄存器数据段映射
            // 模拟 100 个保持寄存器数据区 (40001 ~ 40100)
            var holdingRegs = new HoldingRegisterValueMonitor(0, 100);
            
            // 初始化一些模拟初始值
            holdingRegs.Source[0] = 50;  // 设 40001 为 50
            holdingRegs.Source[1] = 99;  // 设 40002 为 99

            // 订阅数值发生变化的通知
            holdingRegs.ValueChanged += (s, e) =>
            {
                Console.WriteLine($"[数据更新] 寄存器地址 {holdingRegs.StartAddress + e.Index} 发生变化: {e.OriginalValue} -> {e.NewValue} (主站写入)");
            };

            // 3. 构建虚拟从站，并将本地寄存器数据映射区挂载到设备上
            var virtualDevice = new VirtualModbusDevice(server);
            virtualDevice.Mount(holdingRegs);

            // 4. 开启从站监听服务
            server.Start();
            Console.WriteLine("Modbus 虚拟从站已在 502 端口启动，正在等待主站连接...");

            // 模拟从站本地主动改变数值以触发通知
            int count = 0;
            while (count < 5)
            {
                Thread.Sleep(5000);
                holdingRegs.Source[0] = (ushort)(100 + count);
                holdingRegs.Notify(); // 本地更新后手动触发通知
                count++;
            }

            // 5. 停止服务
            server.Stop();
            server.Dispose();
        }
    }
}
```

---

### 场景三：搭建 Modbus 透明代理网关
当一台设备具备多网口或多串口时，需要把不能处理的站号请求路由转发到其他物理通道。可以使用 `ModbusServer` 的 `DownstreamClient` 路由机制。

```csharp
using System;
using STTech.BytesIO.Modbus;
using STTech.BytesIO.Tcp;
using STTech.BytesIO.Serial;

namespace ModbusGatewayDemo
{
    class Program
    {
        static void Main(string[] args)
        {
            // 1. 创建网关的 TCP 上游接收端 (监听 502)
            var gatewayServer = new ModbusTcpServer(ModbusProtocolFormat.RTU)
            {
                Port = 502,
                SlaveId = 1 // 本机站号为 1
            };

            // 2. 本机处理站号为 1 的读取请求
            gatewayServer.ReadHoldingRegisterRequested += (s, e) =>
            {
                Console.WriteLine($"[网关] 本机站号 1 收到读取请求，起始地址: {e.Request.StartAddress}");
                // 返回假数据 [0x00, 0x0A]
                e.ResponseData = new byte[] { 0x00, 0x0A }; 
            };

            // 3. 配置下游通道
            // 实例化一个串口通道连接至 RS485 总线设备
            var serialClient = new SerialClient()
            {
                PortName = "COM2",
                BaudRate = 9600
            };
            serialClient.Connect();

            // 4. 挂载下游客户端。
            // 当主站发送站号不为 1 (如站号 2) 的请求时，网关将原样通过 COM2 串口转发至 RS485 总线；
            // 串口总线返回的响应数据也会自动逆向回传给主站。
            gatewayServer.DownstreamClient = serialClient;

            // 5. 开启网关服务
            gatewayServer.Start();
            Console.WriteLine("Modbus 转发网关已启动...");
            Console.ReadLine();

            gatewayServer.Stop();
            gatewayServer.Dispose();
            serialClient.Disconnect();
            serialClient.Dispose();
        }
    }
}
```

---
*版权所有 © 2026 KarFans Industrial Co., LTD. 保留所有权利。*
