# STTech.BytesIO.Mqtt

[![NuGet](https://img.shields.io/nuget/v/STTech.BytesIO.Mqtt.svg?style=flat-square)](https://www.nuget.org/packages/STTech.BytesIO.Mqtt)
[![License](https://img.shields.io/badge/license-Apache%202.0-blue.svg)](LICENSE)

**STTech.BytesIO.Mqtt** 是基于 `STTech.BytesIO` 核心架构实现的工业物联网 MQTT v5.0 客户端通信库。参考 `STTech.BytesIO.Modbus` 架构，将应用层协议与底层网络通信彻底解耦，底层由 BytesIO 高性能 `TcpClient` 提供驱动，同时支持扩展任何自定义 `BytesClient`。

---

## 🌟 核心特性

- 🎯 **MQTT v5.0 标准协议**：原生支持 MQTT 5.0 标准规范，提供强类型属性系统（User Properties、Reason String、Session Expiry 等）。
- 🚀 **通信与协议深度解耦**：
  - `MqttUnpacker : Unpacker<MqttPacket>`：负责基于 `ReadOnlySequence<byte>` 的高性能报文切片与反序列化。
  - `MqttClient : VirtualClient`：解耦底层传输协议，支持 TCP/TLS 或任意 `BytesClient` 通信。
- 🛡️ **服务质量 (QoS) 全面支持**：
  - **QoS 0**：最多交付一次（即发即弃）。
  - **QoS 1**：至少交付一次（自动处理 PUBACK 与报文重试/跟踪）。
  - **QoS 2**：精确交付一次（PUBREC -> PUBREL -> PUBCOMP 严格四步握手状态机）。
- 🔀 **智能通配符主题路由 (Topic Router)**：
  - 支持单层通配符 `+` 与多层通配符 `#`。
  - 支持双层消息消费模式：全局 `OnApplicationMessageReceived` 事件 + `SubscribeAsync` 绑定独立委托处理器。
- 💓 **内置自动保活与心跳 (Keep-Alive)**：
  - 内部根据 CONNECT/CONNACK 协商时长自动管理 `PINGREQ`/`PINGRESP` 定时调度与超时断开。
- 🔄 **断线重连与自动补订 (Auto-Resubscribe)**：
  - 配合 BytesIO 重连机制自动重新执行 MQTT 握手，并提供 `AutoResubscribe` 开关自动补发历史已订阅主题。
- 📐 **严格遵守 .NET 框架设计准则**：
  - 规范的 PascalCase/camelCase 命名规范、强类型枚举、统一 TAP 异步模式与同步便捷重载。

---

## 📦 安装

```bash
dotnet add package STTech.BytesIO.Mqtt
```

---

## 🚀 快速上手

### 1. 创建客户端并连接

```csharp
using System;
using System.Threading.Tasks;
using STTech.BytesIO.Mqtt;

// 1. 创建 MQTT TCP 客户端
using var client = new MqttTcpClient("127.0.0.1", 1883);

// 2. 配置连接选项
client.Options.ClientId = "Device_001";
client.Options.UserName = "admin";
client.Options.Password = "123456";
client.Options.KeepAlive = 60;
client.Options.CleanStart = true;

// 3. 异步连接并完成 MQTT v5 CONNECT 握手
var connectResult = await client.ConnectMqttAsync();
if (connectResult.IsSuccess)
{
    Console.WriteLine($"连接成功！服务端会话恢复: {connectResult.SessionPresent}");
}
else
{
    Console.WriteLine($"连接失败: {connectResult.ReasonCode} - {connectResult.ReasonString}");
}
```

### 2. 发布消息 (Publish)

```csharp
// 发布 QoS 0 消息
await client.PublishAsync("factory/device1/status", "online");

// 发布 QoS 1 消息并附加 MQTT v5 用户属性
var message = new MqttApplicationMessage("factory/device1/data", "{\"temp\": 25.4}")
{
    QualityOfServiceLevel = MqttQualityOfServiceLevel.AtLeastOnce,
    Retain = false
};
message.Properties.UserProperties.Add(new MqttUserProperty("traceId", Guid.NewGuid().ToString("N")));

var publishResult = await client.PublishAsync(message);
if (publishResult.IsSuccess)
{
    Console.WriteLine($"消息发布成功，报文标识符: {publishResult.PacketId}");
}
```

### 3. 订阅主题并接收消息 (Subscribe)

```csharp
// 方式 A：在订阅时直接绑定通配符路由处理器
await client.SubscribeAsync("factory/+/data", MqttQualityOfServiceLevel.AtLeastOnce, async (msg) =>
{
    Console.WriteLine($"[局部路由] 收到主题 {msg.Topic} 消息: {msg.ConvertPayloadToString()}");
    await Task.CompletedTask;
});

// 方式 B：监听全局消息到达事件
client.OnApplicationMessageReceived += (sender, e) =>
{
    Console.WriteLine($"[全局事件] 主题: {e.ApplicationMessage.Topic}, 内容: {e.ApplicationMessage.ConvertPayloadToString()}");
};
```

### 4. 保活与断开

```csharp
// 手动主动检测心跳 (后台亦会自动定时调度)
await client.PingAsync();

// 优雅断开 MQTT 会话
await client.DisconnectMqttAsync();
```

---

## 📄 开源许可证

本项目遵循 [Apache-2.0](LICENSE) 开源许可证。
