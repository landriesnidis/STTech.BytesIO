# Unpacker (粘粘包隔离解包框架) API 手册

<a name="Unpacker"></a>

在纯 TCP 裸流通讯或者高速串口中，发生“粘包”或“半包断裂现象”是无法躲避的一环。`STTech.BytesIO` 的粘包解析器框架从 `Unpacker` 开始构建了一片依托于高速 `System.IO.Pipelines`与完全零内存拷贝分配的绿洲。不管多长的网络流在这里进行协议切分和拆解，所有的过程**只利用指针位移**（`Span<T> / ReadOnlySequence<byte>`）。

## Unpacker 的泛型无侵入用法

现有的重构使得使用解包器极其简洁。你甚至不再需要去定义那些冗长的强类型类。直接依托函数的内置重载与委托即可完成解析隔离：

```csharp
using STTech.BytesIO.Tcp;
using STTech.BytesIO.Core;
using STTech.BytesIO.Core.Components;

var client = new TcpClient() { Host = "127.0.0.1", Port = 6006 };

// 这个解包器做了一件事情：自动检测对方发来的包的“前两个字节组成的 ushort 内容”
// 并推算出这一条包真正的完整物理长度，以此进行严密的整块分割抛吐！
var myProtocolUnpacker = new Unpacker<string>(client,
    lenHandler: (ReadOnlySequence<byte> unparsedSeq) => 
    {
        if (unparsedSeq.Length < 2) return 0; // 头没接到，挂起继续等
        
        // 用 Slice 切割而绝不是 ToArray。这就是绝对零引用的极速转换典范：
        Span<byte> head = stackalloc byte[2];
        unparsedSeq.Slice(0, 2).CopyTo(head);
        
        // 头两个字节代表的是包体长度
        var bodyLength = BitConverter.ToUInt16(head); 
        
        // 告诉底层我需要的总切包长度
        return bodyLength + 2;
    },
    parseHandler: (ReadOnlySequence<byte> packetSeq) => 
    {
        // 到了这一步！经过上面函数的控制，现在这个 packetSeq 必定是极度合规且被切出来的一包完整的协议字节总成！
        // 在这里将其转化为 string
        return Encoding.UTF8.GetString(packetSeq.Slice(2).ToArray());
    }
);

// 此时解包器就会直接拦截原生 tcp 并且经过处理发向你。你会一直并且仅仅会收到完美验证的泛型！
myProtocolUnpacker.OnDataParsed += (s, e) =>
{
    // 这个 e.Data 拿到的就是被你的函数过滤加工出来的 typeof(string) 类型：
    Console.WriteLine("收到独立隔离包: " + e.Data);
};

await client.ConnectAsync();
```

## `UnpackContext` 处理流境

所有的不依托具体类型的核心基类解包器，其发起的拆解完成事件包裹的载具均为：`UnpackContext`。在获取它的时候你实际上并没有对该包段内存拥有“拥有权”。

```csharp
public class UnpackContext
{
    // 提供只读序列段以避免创建任何的垃圾回收惩罚：
    public ReadOnlySequence<byte> Data { get; }
    
    // 如果你在解包层切出数据时额外拿出了包号等凭证并需要传递给上层使用，你应当把它丢进 Properties 扩展字典中
    public Dictionary<string, object> Properties { get; }
}
```

请记住对于底层 API 的唯一信条：在任何你不需要对内存进行驻留保留的判断逻辑中，绝不要轻易在一个高速的上下文中调用 `Data.ToArray()` 进行低效解分配！
