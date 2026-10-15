using STTech.BytesIO.Modbus;
using System.Text;
using Xunit;

namespace STTech.BytesIO.Modbus.Tests
{
    public class ModbusEntityAndHelperTests
    {
        [Fact]
        public void ModbusRequest_Ascii_SlaveIdFormatting_ShouldBeHex()
        {
            var req = new ReadHoldingRegisterRequest
            {
                SlaveId = 10, // 0x0A
                StartAddress = 0,
                Length = 1,
                ProtocolFormat = ModbusProtocolFormat.ASCII
            };

            byte[] bytes = req.GetBytes();
            string asciiStr = Encoding.ASCII.GetString(bytes);

            // 必须以 :0A 开头，以前写成 :10 是十进制错误
            Assert.StartsWith(":0A", asciiStr);
        }

        [Fact]
        public void ModbusRequest_LRC_ShouldAlwaysReturnTwoHexChars()
        {
            // 测试各种输入确保 LRC 长度恒等于 2
            string[] testMessages = new[]
            {
                ":01030000000A",
                ":010100000001",
                ":0A0F0001000801FF",
                ":010600010001"
            };

            foreach (var msg in testMessages)
            {
                string lrc = ModbusRequest.LRC(msg);
                Assert.NotNull(lrc);
                Assert.Equal(2, lrc.Length);
            }
        }

        [Fact]
        public void ModbusResponse_Rtu_ChecksumExtraction_ShouldExtractLastTwoBytes()
        {
            // 构造一个 8 字节的 RTU 响应报文:
            // SlaveId(1), Func(0x03), ByteCount(2), Val(0x12, 0x34), CRC_L(0xAA), CRC_H(0xBB)
            byte[] data = new byte[] { 0x01, 0x03, 0x02, 0x12, 0x34, 0xAA, 0xBB };

            var resp = new ReadHoldingRegisterResponse(data);
            Assert.Equal(0x01, resp.SlaveId);
            Assert.Equal(FunctionCode.ReadHoldingRegister, resp.FunctionCode);
            // CRC 应为 (0xBB << 8) | 0xAA = 0xBBAA
            Assert.Equal((ushort)0xBBAA, resp.Checksum);
        }

        [Fact]
        public void ModbusRequest_Rtu_ChecksumExtraction_ShouldExtractLastTwoBytes()
        {
            // 构造一个 8 字节的 RTU 请求报文:
            // SlaveId(1), Func(0x03), Addr(0x00, 0x01), Quantity(0x00, 0x02), CRC_L(0x55), CRC_H(0x66)
            byte[] data = new byte[] { 0x01, 0x03, 0x00, 0x01, 0x00, 0x02, 0x55, 0x66 };

            var req = new ReadRegisterRequest(data);
            Assert.Equal(0x01, req.SlaveId);
            Assert.Equal(FunctionCode.ReadHoldingRegister, req.FunctionCode);
            Assert.Equal((ushort)0x6655, req.Checksum);
        }

        [Fact]
        public void ReadCoilRegisterResponse_BitParsing_ShouldBeAccurate()
        {
            // 构造测试数据：ByteCount = 2, Byte1 = 0x05 (二进制 00000101 -> bit0=1, bit1=0, bit2=1, bit3~7=0),
            // Byte2 = 0x80 (二进制 10000000 -> bit0~6=0, bit7=1)
            byte[] payload = new byte[] { 2, 0x05, 0x80 };

            // 模拟构造
            // 利用反射调用私有 ParseBits
            var method = typeof(ReadCoilRegisterResponse).GetMethod("ParseBits", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            Assert.NotNull(method);

            var values = (bool[])method.Invoke(null, new object[] { payload, 2 });
            Assert.NotNull(values);
            Assert.Equal(16, values.Length);

            // Byte 1
            Assert.True(values[0]);
            Assert.False(values[1]);
            Assert.True(values[2]);
            Assert.False(values[3]);
            Assert.False(values[4]);
            Assert.False(values[5]);
            Assert.False(values[6]);
            Assert.False(values[7]);

            // Byte 2
            Assert.False(values[8]);
            Assert.False(values[9]);
            Assert.False(values[10]);
            Assert.False(values[11]);
            Assert.False(values[12]);
            Assert.False(values[13]);
            Assert.False(values[14]);
            Assert.True(values[15]);
        }

        [Fact]
        public void ReadInputRegisterResponse_ShouldTruncateToSpecifiedLength()
        {
            // 构造数据：前导 1 字节 ByteCount = 2，但后面跟了 4 个字节
            byte[] payload = new byte[] { 0x01, 0x04, 2, 0x11, 0x22, 0x99, 0x99, 0x00, 0x00 };

            var resp = new ReadInputRegisterResponse(payload);
            Assert.Equal(2, resp.Length);
            // 修复前 Values 会包含多余的 0x99, 0x99，修复后应严格截断为 2 字节
            Assert.Equal(2, resp.Values.Length);
            Assert.Equal(new byte[] { 0x11, 0x22 }, resp.Values);
        }

        [Fact]
        public void ModbusSerialClient_DtrEnable_ShouldOperateIndependently()
        {
            var client = new ModbusSerialClient(ModbusProtocolFormat.RTU);

            // 验证 DtrEnable 设置不影响 DiscardNull
            client.DiscardNull = false;
            client.DtrEnable = true;

            Assert.True(client.DtrEnable);
            Assert.False(client.DiscardNull);

            client.DtrEnable = false;
            Assert.False(client.DtrEnable);
        }

        [Fact]
        public void HoldingRegisterValueMonitor_ValueChanged_ShouldFire()
        {
            var monitor = new STTech.BytesIO.Modbus.Monitors.HoldingRegisterValueMonitor(0, 10);
            int eventFiredCount = 0;
            int changedIndex = -1;
            ushort oldVal = 0;
            ushort newVal = 0;

            monitor.ValueChanged += (s, e) =>
            {
                eventFiredCount++;
                changedIndex = e.Index;
                oldVal = e.OriginalValue;
                newVal = e.NewValue;
            };

            monitor.Source[3] = 1234;
            monitor.Notify();

            Assert.Equal(1, eventFiredCount);
            Assert.Equal(3, changedIndex);
            Assert.Equal(0, oldVal);
            Assert.Equal(1234, newVal);
        }
    }
}
