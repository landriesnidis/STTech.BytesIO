# STTech.BytesIO.SerialPortStream

**STTech.BytesIO.SerialPortStream** 是 `STTech.BytesIO` 的增强型串口扩展库。它基于成熟的 `SerialPortStream` 开源库实现，旨在为 .NET 开发者提供比原生 `System.IO.Ports.SerialPort` 更稳定、更高效以及跨平台一致性更强的串口通信体验。

## ✨ 核心优势 (Key Advantages)

- **高性能**: 优化了读写缓冲区管理，减少高频通信下的系统开销。
- **跨平台一致性**: 在不同 Windows 版本及非 Windows 环境下提供更可靠的硬件兼容性。
- **稳定可靠**: 解决了原生串口库在某些虚拟串口或国产串口设备上的异常断开与数据丢失问题。
- **无缝集成**: 完美融入 `STTech.BytesIO` 的统一通信模型。

## 🛠️ 安装 (Installation)

```bash
dotnet add package STTech.BytesIO.SerialPortStream
```

## 📖 使用示例 (Usage)

```csharp
// 使用 SerialPortStreamClient 代替默认的 SerialClient
var client = new SerialPortStreamClient { PortName = "COM1", BaudRate = 115200 };
await client.ConnectAsync();
await client.SendAsync(data);
```

---
*Powered by KarFans Industrial Co., LTD.*
