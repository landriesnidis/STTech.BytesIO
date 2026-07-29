# Unpacker (解包器) 快速上手教程

在基于字节流的网络通信（如 TCP Socket）或高速串口通信中，由于传输层是面向字节流的而不是面向消息包的，不可避免地会遇到**粘包（Sticky Packets）**和**半包/断包（Partial Packets）**的问题。

`STTech.BytesIO` 提供了一套高性能、无内存分配（零拷贝）的解包器框架——`Unpacker`。本教程将带你通过**类继承（Inheritance）**的方式深入了解 `Unpacker` 的设计与使用。

---

## 1. 为什么需要 Unpacker？

### 字节流与协议边界
- **流式传输**：TCP 协议或串口通信发送的数据是无边界的字节流。接收端在一次接收事件中可能收到多个粘连的数据包（粘包），或者只收到半个数据包（断包）。
- **物理切分**：开发人员需要根据约定的协议规则，将接收到的连续字节流切分成独立的、完整的“协议包”。

### 传统解包方式的痛点
传统的解包方法通常会频繁地进行 `byte[]` 内存分配与拷贝（如 `ToArray()` 或 `Buffer.BlockCopy`），在高速、高并发的通信场景下，这会给垃圾回收器（GC）带来巨大的压力，从而导致系统卡顿或延迟增加。

### Unpacker 的设计优势
`STTech.BytesIO` 的 `Unpacker` 基于 `System.IO.Pipelines` 和 `ReadOnlySequence<byte>` 构建：
- **零拷贝与零分配**：在切分协议包时，Unpacker **仅利用指针和位移操作**（通过切片 `Sequence.Slice`），不需要为每个数据包重新分配内存。
- **内存池管理**：配合底层缓冲区，大幅度降低 GC 压力，提升吞吐量。

---

## 2. 泛型解包器 `Unpacker<T>`

`STTech.BytesIO` 设计了非泛型的 `Unpacker` 基类与泛型的 `Unpacker<T>` 类：

- **`Unpacker` (基类)**：解析出的结果事件参数包裹的是 `UnpackContext`（内含只读数据序列 `ReadOnlySequence<byte>`）。它关注于底层的分段与剥离。
- **`Unpacker<T>` (泛型版本)**：在底层剥离出完整的数据包后，通过用户指定的**转换委托**，直接将原始字节序列解析并转换为强类型对象 `T`（如 `string` 或自定义契约类），使得上层业务直接获取强类型数据，无需手动解析字节。

---

## 3. 核心方法：CalculatePacketLength

无论是在派生类中重写，还是通过委托注入，解包器的核心在于计算出当前缓存中第一个完整数据包的长度。

### 方法签名
```csharp
protected abstract int CalculatePacketLength(ReadOnlySequence<byte> buffer);
```

### 参数说明
- **`ReadOnlySequence<byte> buffer`**：当前解包器缓存区中所有**未消费**的字节数据序列。
  > [!NOTE]
  > 该序列可能是单段的（`IsSingleSegment` 为 true），也可能是跨多个内存块的（多段链式结构）。在读取数据时，推荐使用 `STTech.BytesIO.Core` 命名空间下为 `ReadOnlySequence<byte>` 提供的免分配扩展方法，例如 `ReadInt16BigEndian`、`GetByte`、`IndexOf` 等。

### 返回值及用法说明
解包器引擎会根据 `CalculatePacketLength` 的返回值进行不同的内部状态转换：

| 返回值条件 | 含义 | 解包器后续动作 |
| :--- | :--- | :--- |
| **`packetLen <= 0`** | 暂无法判断数据包的长度（通常因为数据太少，连头部长度字段都还没收齐）。 | 挂起，结束本次解包，等待下一次数据接收事件。 |
| **`packetLen > buffer.Length`** | 已知当前包的预期长度为 `packetLen`，但当前缓冲区内的数据不足 `packetLen` 字节（断包）。 | 挂起，结束本次解包，等待更多数据拼包。 |
| **`0 < packetLen <= buffer.Length`** | 成功识别到一个完整的协议包，长度为 `packetLen`（可能刚好一包，也可能后面还粘着其他包）。 | 1. 截取（Slice）该包的数据，触发 `OnDataParsed` 事件。<br>2. 内部缓存区向后移位消费 `packetLen` 字节。<br>3. **立即循环**，用剩余的缓冲区数据再次调用本方法，直到数据不足以切包。 |

---

## 4. 场景实战与类继承示例

在实际开发中，推荐通过继承 `Unpacker`（或 `Unpacker<T>`）来封装特定协议的解包逻辑。这样不仅利于代码的模块化和复用，还能在子类中优雅地利用 `ReadOnlySequenceExtensions` 提供的零拷贝扩展方法。

以下演示三种最常见的协议场景，你可以根据自己的协议特征直接匹配对应的场景实现。

---

### 场景一：固定长度协议（如每包固定 10 字节）

**适用场景**：数据包长度固定，没有任何长度字段或结束符标识（如某些传感器周期性上报的数据帧）。

```csharp
using System.Buffers;
using System.Text;
using STTech.BytesIO.Core;
using STTech.BytesIO.Core.Component;

namespace MyProtocol
{
    /// <summary>
    /// 固定长度解包器（继承 Unpacker，处理原始字节序列）
    /// </summary>
    public class FixedLengthUnpacker : Unpacker
    {
        private readonly int _fixedLength;

        public FixedLengthUnpacker(int fixedLength)
        {
            _fixedLength = fixedLength;
        }

        protected override int CalculatePacketLength(ReadOnlySequence<byte> buffer)
        {
            // 只有当前积累的缓冲区有效字节数达到或超过固定长度，才返回该长度进行切包，否则返回 0 挂起等待
            return buffer.Length >= _fixedLength ? _fixedLength : 0;
        }
    }
}
```

**使用方式**：
```csharp
using STTech.BytesIO.Tcp;
using STTech.BytesIO.Core;

var client = new TcpClient() { Host = "127.0.0.1", Port = 6006 };
var unpacker = new FixedLengthUnpacker(10);

// 绑定解包器到客户端
client.BindUnpacker(unpacker);

// 订阅解包成功事件
unpacker.OnDataParsed += (sender, e) =>
{
    // 利用 ReadOnlySequenceExtensions 免分配提取数据
    string dataStr = e.Data.Data.GetString(0, 10, Encoding.ASCII);
    Console.WriteLine($"[收到固定长度包]: {dataStr}");
};

await client.ConnectAsync();
```

---

### 场景二：头部含长度字段的协议（如偏移 2 处占 2 字节）

**适用场景**：数据包长度动态变化。通常消息头部有固定魔数（包头标识），以及指示后续载荷长度的字段。

**协议规则示例**：
- 包头标识（Magic Header）：固定 2 字节（`0x55 0xAA`）。
- 长度字段：在偏移 `2` 处占 2 字节（大端序），指示**后续载荷**的长度。
- **总包长度** = 包头 (2) + 长度字段 (2) + 载荷长度。

```csharp
using System;
using System.Buffers;
using System.Text;
using STTech.BytesIO.Core;
using STTech.BytesIO.Core.Component;

namespace MyProtocol
{
    public class MyMessage
    {
        public ushort DeviceId { get; set; }
        public string Payload { get; set; }
    }

    /// <summary>
    /// 头部含长度的动态解包器（继承自泛型 Unpacker<T>）
    /// </summary>
    public class LengthFieldUnpacker : Unpacker<MyMessage>
    {
        protected override int CalculatePacketLength(ReadOnlySequence<byte> buffer)
        {
            // 1. 如果当前连包头和长度字段（共 4 字节）都没收齐，则返回 0 继续等待
            if (buffer.Length < 4) return 0;

            // 2. 使用 GetByte() 扩展方法读取头部字节，验证包头标识 (0x55, 0xAA)
            if (buffer.GetByte(0) != 0x55 || buffer.GetByte(1) != 0xAA)
            {
                // 包头不匹配，返回 -1 告知框架清除异常字节以防死锁
                return -1;
            }

            // 3. 使用 ReadUInt16BigEndian() 扩展方法，直接在偏移 2 的位置读取大端序的长度
            ushort payloadLen = buffer.ReadUInt16BigEndian(2);

            // 4. 计算整个物理包的预期总长度
            int totalPacketLen = 4 + payloadLen;

            return totalPacketLen;
        }

        protected override MyMessage ResponseSerializeHandler(UnpackContext context)
        {
            // 此时已切分出独立包，context.Data 的长度恰好等于上面计算出的总长度
            var sequence = context.Data;

            // 5. 提取载荷字符串：在偏移 4 的位置，使用 GetString() 扩展方法，避免 byte[] 分配
            int payloadLen = (int)sequence.Length - 4;
            string payload = sequence.GetString(4, payloadLen, Encoding.UTF8);

            return new MyMessage
            {
                DeviceId = 0x55AA,
                Payload = payload
            };
        }
    }
}
```

**使用方式**：
```csharp
var client = new TcpClient() { Host = "127.0.0.1", Port = 6006 };
var unpacker = new LengthFieldUnpacker();

client.BindUnpacker(unpacker);

unpacker.OnDataParsed += (sender, e) =>
{
    // 直接获取强类型的解析对象 MyMessage
    MyMessage msg = e.Data;
    Console.WriteLine($"[收到强类型消息] DeviceId: {msg.DeviceId:X}, Payload: {msg.Payload}");
};

await client.ConnectAsync();
```

---

### 场景三：以特定结束符分割的协议（如 `\r\n` 结尾的命令行/文本协议）

**适用场景**：协议每个包的长度不定，但都是通过特定的字符或字节序列作为分隔符（如控制台指令、HTTP 头等）。

```csharp
using System.Buffers;
using System.Text;
using STTech.BytesIO.Core;
using STTech.BytesIO.Core.Component;

namespace MyProtocol
{
    /// <summary>
    /// 结束符分割解包器（继承自泛型 Unpacker<string>）
    /// </summary>
    public class DelimiterUnpacker : Unpacker<string>
    {
        private static readonly byte[] Delimiter = new byte[] { 0x0D, 0x0A }; // \r\n

        protected override int CalculatePacketLength(ReadOnlySequence<byte> buffer)
        {
            // 1. 使用 IndexOf() 扩展方法在多段序列中搜索结束符，该方法性能优异且完全免分配
            long? index = buffer.IndexOf(Delimiter);

            if (index.HasValue)
            {
                // 找到结束符，总包长度 = 结束符的起始索引位置 + 结束符本身的长度
                return (int)(index.Value + Delimiter.Length);
            }

            // 未检测到结束符，挂起等待后续数据
            return 0;
        }

        protected override string ResponseSerializeHandler(UnpackContext context)
        {
            var sequence = context.Data;
            
            // 2. 去除最后的 \r\n (2字节)，使用 GetString() 扩展方法转换为字符串
            int contentLen = (int)sequence.Length - Delimiter.Length;
            return sequence.GetString(0, contentLen, Encoding.UTF8);
        }
    }
}
```

**使用方式**：
```csharp
var client = new TcpClient() { Host = "127.0.0.1", Port = 6006 };
var unpacker = new DelimiterUnpacker();

client.BindUnpacker(unpacker);

unpacker.OnDataParsed += (sender, e) =>
{
    string command = e.Data;
    Console.WriteLine($"[收到命令]: {command}");
};

await client.ConnectAsync();
```

---

## 5. 开发建议与避坑指南

> [!TIP]
> **绝对避免不必要的 ToArray()**
> 很多开发者在实现 `CalculatePacketLength` 时直接调用 `buffer.ToArray()` 转换为普通字节数组进行逻辑处理。这会彻底抵消 Unpacker 的高性能和零拷贝设计优势。请务必使用 `ReadOnlySequenceExtensions.cs` 中的扩展方法进行轻量级的位移探测与读取。

> [!WARNING]
> **防止脏数据引起死锁**
> 在包头/长度校验中，如果遇到无效数据，如果一直返回 `0` 挂起，缓冲区会一直增长导致内存耗尽。
> - 在不匹配时返回负数（如 `-1`）指示清除当前首个字节；
> - 或者配置 `ErrorOccurHandler`，在发生协议不匹配时主动清空当前缓存：
>   ```csharp
>   unpacker.ErrorOccurHandler = (errorCode) => {
>       // 返回 true 表示清空当前解包器缓存，防止脏数据形成无限死锁
>       return true; 
>   };
>   ```
