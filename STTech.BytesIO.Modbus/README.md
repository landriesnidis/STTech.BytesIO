# STTech.BytesIO.Modbus

**STTech.BytesIO.Modbus** 是一个基于 `STTech.BytesIO` 核心架构实现的全方位 Modbus 协议库。它为提供了统一且易用的接口来处理 Modbus RTU、TCP 及 ASCII 通信。

## ✨ 特性 (Features)

- **全面协议支持**: 覆盖 Modbus RTU, Modbus TCP, Modbus ASCII。
- **标准功能码**: 
  - `ReadCoilRegister` (0x01)
  - `ReadDiscreteInputRegister` (0x02)
  - `ReadHoldingRegister` (0x03)
  - `ReadInputRegister` (0x04)
  - `WriteSingleCoilRegister` (0x05)
  - `WriteSingleHoldingRegister` (0x06)
  - `WriteMultipleCoilRegisters` (0x0F)
  - `WriteMultipleHoldingRegisters` (0x10)
- **灵活的传输层**: 可无缝切换集成的 TCP 或串口 (SerialPort) 传输。
- **高性能解包**: 内置 Modbus 专用解包器，确保数据帧的完整性与准确性。

## 🛠️ 安装 (Installation)

```bash
dotnet add package STTech.BytesIO.Modbus
```

## 📖 快速上手 (Quick Start)

```csharp
// 创建 Modbus TCP 客户端
var client = new ModbusTcpClient { Host = "127.0.0.1", Port = 502 };
await client.ConnectAsync();

// 读取保持寄存器 (地址: 0, 长度: 10)
var response = await client.ReadHoldingRegisterAsync(0, 10);
if (response.IsSuccess) {
    var data = response.Data; // 处理读取到的字节数据
}
```

---
*Powered by KarFans Industrial Co., LTD.*
