# STTech.BytesIO.P2P

[![NuGet](https://img.shields.io/nuget/v/STTech.BytesIO.P2P.svg?style=flat-square)](https://www.nuget.org/packages/STTech.BytesIO.P2P)
[![License](https://img.shields.io/badge/license-Apache%202.0-blue.svg)](LICENSE)

**STTech.BytesIO.P2P** 是基于 `Nethermind.Libp2p` 实现的点对点（P2P）字节流通信扩展库。在继承 `STTech.BytesIO` 统一 `BytesClient` 编程体验的同时，提供基于 Libp2p 协议栈的传输、加密与寻址能力。

---

## 🌟 核心特性

- 🌐 **多协议传输支持**：
  - 支持 TCP 基础传输
  - 支持 QUIC 传输（适用于弱网与低延迟场景）
- 🔒 **高安全性**：内置 Noise 与 TLS 协议握手加密，保护点对点传输隐私与数据安全。
- 📦 **流式传输与零丢失**：针对大数据包采用流式切片分发机制，杜绝数据截断。
- 🔄 **统一架构**：继承 `BytesClient` 与 `IBytesServer`，保持统一的事件驱动与异步通信模型。

---

## 🗺️ 路线图（规划中特性）

- [ ] 完整对等节点抽象（统一 Listen 与 Dial 双向直连模型）
- [ ] Kademlia DHT 分布式节点发现与 PeerId 动态寻址
- [ ] Circuit Relay v2 中继与 NAT 穿透打洞
- [ ] PubSub 消息发布/订阅系统

---

## 📦 安装

```bash
dotnet add package STTech.BytesIO.P2P
```

---

## 🚀 快速上手

### 1. 服务端（引导节点）

```csharp
using STTech.BytesIO.P2P;

var server = new BootstrapServer();
server.Options.ListenAddress = "127.0.0.1";
server.Options.TcpPort = 4001;

server.PeerConnected += (s, e) =>
{
    Console.WriteLine($"对等节点已接入: {e.Client.RemotePeerId}");
    e.Client.OnDataReceived += (cs, ce) =>
    {
        Console.WriteLine($"收到客户端消息: {ce.Data.ToHexString()}");
    };
};

await server.StartAsync();
Console.WriteLine($"引导节点已启动，监听地址: {string.Join(", ", server.ListenAddresses)}");
```

### 2. 客户端（Peer 节点）

```csharp
using STTech.BytesIO.P2P;

var client = new PeerClient();

client.OnDataReceived += (s, e) =>
{
    Console.WriteLine($"收到节点消息: {e.Data.ToHexString()}");
};

// 通过 Multiaddr 连接目标对等节点
await client.ConnectAsync("/ip4/127.0.0.1/tcp/4001/p2p/12D3KooW...");

// 发送数据
await client.SendAsync(new byte[] { 0x01, 0x02, 0x03 });
```

---

## 📄 开源许可证

本项目基于 [Apache-2.0](https://www.apache.org/licenses/LICENSE-2.0) 协议开源。
