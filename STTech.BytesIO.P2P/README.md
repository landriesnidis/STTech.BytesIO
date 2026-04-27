# STTech.BytesIO.P2P

`STTech.BytesIO.P2P` 是基于 `Nethermind.Libp2p` 实现的 P2P（点对点）网络通信库。作为 `STTech.BytesIO` 的扩展模块，它提供了统一的 `BytesClient` 编程模型，同时解锁了去中心化网络的强大能力。

## 特性

*   **完全兼容 `STTech.BytesIO`：** 继承自 `BytesClient`，享有数据解包、事件驱动等基础架构。
*   **多传输协议支持：** 开箱即用支持 TCP 和 QUIC。
*   **去中心化网络：** 
    *   Kademlia DHT 节点发现
    *   mDNS 局域网发现
    *   Relay 中继穿透 (NAT 穿透)
    *   PubSub 消息发布/订阅机制
*   **高安全性：** 内置 Noise 和 TLS 协议握手加密。
*   **灵活配置：** 提供 `P2PNodeOptions` 通过布尔开关一键开启或关闭网络特性。

## 主要组件

*   `PeerClient`: P2P 通信客户端，用于连接远端 Multiaddr 或 PeerId，进行数据收发。
*   `BootstrapServer`: P2P 引导节点（种子节点），用于搭建私有 P2P 网络的入口节点，也可作为 Relay 中继服务或 DHT 路由节点。

## 快速上手

P2P 通信的核心在于 **Multiaddr**（多层地址）。它不仅包含 IP 和端口，还包含传输协议和节点的身份标识（PeerId）。

### 1. 搭建私有网络入口（种子节点）

种子节点（BootstrapServer）作为网络的“介绍人”，帮助其他客户端互相发现。

```csharp
var bootstrapServer = new BootstrapServer();
bootstrapServer.Options.TcpPort = 4001; // 设定固定端口
await bootstrapServer.StartAsync();

// 打印本地节点的完整 Multiaddr 地址，供客户端连接使用
// 格式通常为：/ip4/127.0.0.1/tcp/4001/p2p/12D3KooWL9pP...
Console.WriteLine($"种子节点已启动: {bootstrapServer.ListenAddresses.FirstOrDefault()}/p2p/{bootstrapServer.LocalPeerId}");
```

### 2. 客户端连接并通信

客户端需要知道种子节点的地址才能加入网络。

```csharp
var client = new PeerClient();
// 填写种子节点的完整地址 (包含其 PeerId)
client.RemoteAddress = "/ip4/127.0.0.1/tcp/4001/p2p/12D3KooWL9pPZ9qA..."; 

client.OnConnectedSuccessfully += (s, e) => Console.WriteLine("连接成功");
client.OnDataReceived += (s, e) => Console.WriteLine($"收到数据: {Encoding.UTF8.GetString(e.Data.ToArray())}");

await client.ConnectAsync();
await client.SendAsync(Encoding.UTF8.GetBytes("Hello P2P!"));
```

## 参数填写指南 (Multiaddr)

在 Demo 或代码中，`RemoteAddress` 必须遵循 **Multiaddr** 格式。以下是常用参数的含义：

| 组成部分 | 示例 | 含义 |
| :--- | :--- | :--- |
| **IP层** | `/ip4/127.0.0.1` | 目标主机的 IPv4 地址 |
| **传输层** | `/tcp/4001` | 使用 TCP 协议及 4001 端口 |
| **传输层(可选)** | `/udp/4002/quic-v1` | 使用 QUIC 协议及 4002 端口 |
| **身份层** | `/p2p/QmXoyp...` | 目标节点的 **PeerId** (必须填写，用于安全握手) |

**组合示例：**
`/ip4/192.168.1.100/tcp/4001/p2p/12D3KooWL9pPZ9qA...`

> [!IMPORTANT]
> **如何获取 PeerId？**
> 在 `BootstrapServer` 或 `PeerClient` 启动后，通过 `LocalPeerId` 属性即可获取当前节点的身份标识。没有 PeerId，P2P 连接将无法建立。

## 注意事项

底层依赖于 `Nethermind.Libp2p` 的 Prerelease 版本，适用于需要极高连通性和 P2P 架构的物联网或去中心化项目。
