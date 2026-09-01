# STTech.BytesIO.P2P

[![NuGet](https://img.shields.io/nuget/v/STTech.BytesIO.P2P.svg?style=flat-square)](https://www.nuget.org/packages/STTech.BytesIO.P2P)
[![License](https://img.shields.io/badge/license-Apache%202.0-blue.svg)](LICENSE)

**STTech.BytesIO.P2P** 是基于 `Nethermind.Libp2p` 实现的点对点（P2P）去中心化通信扩展库。在继承 `STTech.BytesIO` 统一 `BytesClient` 编程体验的同时，赋予应用去中心化节点发现、NAT 穿透及安全通信能力。

---

## 🌟 核心特性

- 🌐 **去中心化网络**：
  - Kademlia DHT 分布式节点发现与路由
  - mDNS 局域网节点快速发现
  - Relay 中继节点与 NAT 穿透支持
  - PubSub 消息发布/订阅机制
- 🔒 **高安全性**：内置 Noise 与 TLS 协议握手加密，保护点对点传输隐私。
- 🔄 **统一架构**：继承 `BytesClient`，与常规 TCP/串口保持相同的事件与数据流模型。

---

## 📦 安装

```bash
dotnet add package STTech.BytesIO.P2P
```

---

## 🚀 快速上手

```csharp
using STTech.BytesIO.P2P;

// 1. 创建 P2P 通信客户端
var client = new PeerClient();

client.OnDataReceived += (s, e) =>
{
    Console.WriteLine($"收到 P2P 节点消息: {e.Data.ToHexString()}");
};

// 2. 通过 Multiaddr 连接目标对等节点
await client.ConnectAsync("/ip4/192.168.1.100/tcp/4001/p2p/12D3KooW...");

// 3. 发送数据
await client.SendAsync(new byte[] { 0x01, 0x02 });
```

---

## 📄 开源许可证

本项目基于 [Apache-2.0](https://www.apache.org/licenses/LICENSE-2.0) 协议开源。