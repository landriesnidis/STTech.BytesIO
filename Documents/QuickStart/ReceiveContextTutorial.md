# ReceiveContext (接收数据上下文) 快速上手教程

在 `STTech.BytesIO` 框架中，`ReceiveContext` 是核心的数据承载对象。每当客户端或服务端接收到新的网络数据或串口数据时，这些原始数据都会被封装成一个 `ReceiveContext` 对象并向上传递。

本教程将详细介绍 `ReceiveContext` 是什么、如何高效地从中提取数据、使用时的注意事项以及这一设计的核心优势。

---

## 1. 什么是 ReceiveContext？

`ReceiveContext` 是对底层接收缓冲区的安全包装器。它的核心设计理念是**“池化内存 + 零拷贝”**。

- **内存池化（Memory Pooling）**：为了避免频繁分配和回收字节数组导致的 GC（垃圾回收）压力，`ReceiveContext` 底层直接使用 `System.Buffers.ArrayPool<byte>` 租用内存。
- **逻辑区间封装**：从缓冲池中租用的数组大小通常大于实际收到的数据长度。`ReceiveContext` 内部记录了 `_offset`（有效数据的起始偏移）和 `_length`（有效数据的物理长度），对外只暴露这个“有效数据区间”。
- **集合化接口**：它实现了 `IReadOnlyList<byte>` 接口。这意味着对于使用者来说，它可以被当成一个普通的**只读字节数组**，直接支持索引访问（如 `context[i]`）和所有的 LINQ 操作（如 `Where`、`Select`、`Skip`、`Take` 等）。
- **引用计数管理（Reference Counting）**：`ReceiveContext` 内部维护了一个引用计数。当数据在不同的解包处理器或组件之间传递时，可以通过 `IncrRef()` 增加计数；处理完毕后通过 `Dispose()` 减少计数。当计数归零时，底层的字节数组会自动归还到 `ArrayPool` 中。

---

## 2. ReceiveContext 的主要方法与属性

以下是 `ReceiveContext` 暴露的常用 API：

### 属性
- **`Length`**：有效数据的字节长度。
- **`ReceivedTime`**：数据接收到的时间戳（`DateTime`）。
- **`IsDisposed`**：该上下文是否已被释放回收。
- **`Memory`**：返回有效数据区间的只读内存块 `ReadOnlyMemory<byte>`。
- **`Properties`**：一个扩展属性字典（`IDictionary<string, object>`），允许在数据流转链条中附加自定义的元数据。

### 数据转换与提取方法
- **`this[int index]`**：索引器，安全访问有效区间内的第 `index` 个字节。
- **`ToArray()`**：将有效数据拷贝并生成一个新的 `byte[]`（**注意：这会产生内存分配**）。
- **`CopyTo(byte[] destination, int destinationIndex)`**：将有效数据拷贝到已有的目标数组中。
- **`EncodeToString(Encoding encoding)`**：将有效数据按指定编码转换为字符串。
- **`ToHexString()` / `ToHexString(startIndex, length)`**：转换为十六进制字符串（如 `"0A-1B-2C"`）。

### BitConverter 风格的安全提取方法（支持大小端序转换）
- `ToBoolean(int startIndex)`
- `ToChar(int startIndex, bool bigEndian = false)`
- `ToInt16(int startIndex, bool bigEndian = false)`
- `ToUInt16(int startIndex, bool bigEndian = false)`
- `ToInt32(int startIndex, bool bigEndian = false)`
- `ToUInt32(int startIndex, bool bigEndian = false)`
- `ToInt64(int startIndex, bool bigEndian = false)`
- `ToUInt64(int startIndex, bool bigEndian = false)`
- `ToSingle(int startIndex, bool bigEndian = false)` （读取 float）
- `ToDouble(int startIndex, bool bigEndian = false)` （读取 double）

---

## 3. 提取数据的实战示例

下面是几个从 `ReceiveContext` 中提取结构化数据的典型例子。

### 示例一：直接按字节和范围提取（LINQ 与索引）
```csharp
// 假设收到数据: [0xAA, 0x01, 0x02, 0x03, 0xBB]
ReceiveContext context = ...; 

// 1. 获取包头和包尾 (利用 0-based 相对索引)
byte header = context[0];             // 0xAA
byte footer = context[context.Length - 1]; // 0xBB

// 2. 利用 LINQ 过滤出所有大于 0x01 的字节 (无需 ToArray)
IEnumerable<byte> filtered = context.Where(b => b > 0x01);
```

### 示例二：提取大小端序的整型与浮点数
假设协议格式如下：
- 偏移 `0` 处为 `ushort` 类型的设备 ID（大端序）
- 偏移 `2` 处为 `float` 类型的温度值（小端序）
- 偏移 `6` 处为 `int` 类型的状态码（大端序）

```csharp
ReceiveContext context = ...;

// 1. 读取大端序的设备 ID (2 字节)
ushort deviceId = context.ToUInt16(startIndex: 0, bigEndian: true);

// 2. 读取小端序的温度值 (4 字节)
float temperature = context.ToSingle(startIndex: 2, bigEndian: false);

// 3. 读取大端序的状态码 (4 字节)
int statusCode = context.ToInt32(startIndex: 6, bigEndian: true);

Console.WriteLine($"设备ID: {deviceId}, 温度: {temperature}℃, 状态码: {statusCode}");
```

### 示例三：提取字符串与十六进制显示
```csharp
ReceiveContext context = ...;

// 1. 将前 8 字节提取为 ASCII 编码的设备序列号字符串
string sn = context.GetString(0, 8, Encoding.ASCII); // 也可以使用扩展方法

// 2. 将整个数据转换为易读的 Hex 字符串用于日志输出
string hexLog = context.ToHexString();
Console.WriteLine($"收到原始报文: {hexLog}");
```

---

## 4. 核心注意事项（避坑指南）

> [!IMPORTANT]
> **必须手动 Dispose()！**
> `ReceiveContext` 底层使用池化内存，**谁最终消费它，谁就负责释放它**。
> - 如果你只是在客户端的 `OnDataReceived` 事件中简单读取数据，请务必在事件处理函数的末尾调用 `context.Dispose()`。
> - 虽然 `ReceiveContext` 设计了 GC 析构兜底保护，但若不手动释放，内存回收将被延迟到下一次 GC 发生，池化内存的复用效率会大打折扣。

> [!WARNING]
> **切勿在 Dispose() 之后访问数据**
> 一旦 `Dispose()` 触发且引用计数归零，底层的字节数组将被归还给 `ArrayPool` 并将内部引用置为 `null`。此时继续调用 `context[i]` 或 `ToInt32()` 等方法会抛出 `ObjectDisposedException`。

> [!TIP]
> **尽量避免调用 `ToArray()`**
> `ToArray()` 会在托管堆上分配一块全新的 `byte[]` 并进行数据拷贝。如果你提取数据是为了做反序列化或数值计算，请直接使用 `ToInt32` / `ToSingle` 或 `ReadOnlyMemory<byte>` 进行零拷贝转换，只有在必须将数据长久保存在外部（且生命周期超出 `ReceiveContext` 本身）时才使用 `ToArray()`。

---

## 5. 这样做的好处

使用 `ReceiveContext` 统一封装网络接收数据带来以下显著优势：

1. **统一的编程模型**：实现了 `IReadOnlyList<byte>`，让复杂的池化切片对象用起来就像普通数组一样直观，且原生支持强大的 LINQ 语法。
2. **极佳的性能与吞吐**：底层数组在 `ArrayPool` 中被高度复用，大幅减少了小对象（byte array）在托管堆上的频繁分配与回收，有效降低了系统的 GC 抖动。
3. **大小端序无感处理**：内置的 `ToInt32(..., bigEndian)` 等方法会自动识别当前运行系统的字节序（`BitConverter.IsLittleEndian`），在必要时自动反转字节，且在无需反转时实现**零拷贝**，避免了传统反转数组带来的额外分配。
4. **安全的生命周期管理**：通过引用计数（Reference Counting），使得同一个数据包能够在多个解包器、日志组件、监控组件之间安全地传递，只有当所有组件都完成处理并 `Dispose()` 后，底层内存才会被安全回收。
