using STTech.BytesIO.Core;
using STTech.BytesIO.Modbus;
using STTech.BytesIO.Tcp;
using System.Reflection;
using Xunit;

namespace STTech.BytesIO.Modbus.Tests
{
    /// <summary>
    /// 测试 ModbusClient 中 ReplyMatchHandle 的匹配逻辑
    /// </summary>
    public class MatchHandlerTests
    {
        private class TestModbusClient : ModbusClient<TcpClient>
        {
            public TestModbusClient(ModbusProtocolFormat format = ModbusProtocolFormat.RTU)
                : base(new TcpClient(), format)
            {
            }

            public bool InvokeReplyMatchHandle(ModbusRequest req, ModbusResponse resp)
            {
                var method = typeof(ModbusClient).GetMethod(
                    "ReplyMatchHandle",
                    BindingFlags.NonPublic | BindingFlags.Instance,
                    null,
                    new[] { typeof(ModbusRequest), typeof(ModbusResponse) },
                    null
                );
                Assert.NotNull(method);
                return (bool)method.Invoke(this, new object[] { req, resp });
            }
        }

        [Fact]
        public void Match_WriteMultipleCoilRegisters_ShouldMatch_WhenQuantityMatches()
        {
            var client = new TestModbusClient();

            // 请求：从站 1，写入地址 0x0010，写入 10 个线圈 (bool[10])
            var request = new WriteMultipleCoilRegistersRequest
            {
                SlaveId = 1,
                WriteAddress = 0x0010,
                Data = new bool[10]
            };

            // 响应：从站 1，功能码 0x0F，写入地址 0x0010，回显写入线圈数量 10 (0x000A)
            var values = new byte[] { 0x00, 0x0A }; // 大端序 10
            var response = new WriteRegisterResponse(
                1,
                FunctionCode.WriteMultipleCoilRegisters,
                ModbusProtocolFormat.RTU,
                0x0010,
                values
            );

            // 以前的 Bug 会执行 10 / 2 == 10 (false)
            // 修复后应为 10 == 10 (true)
            bool isMatch = client.InvokeReplyMatchHandle(request, response);
            Assert.True(isMatch, "写多个线圈时，应正确比对线圈数量并返回 true");
        }

        [Fact]
        public void Match_WriteMultipleCoilRegisters_ShouldNotMatch_WhenQuantityMismatches()
        {
            var client = new TestModbusClient();

            var request = new WriteMultipleCoilRegistersRequest
            {
                SlaveId = 1,
                WriteAddress = 0x0010,
                Data = new bool[10]
            };

            // 回显线圈数量为 5，不匹配
            var values = new byte[] { 0x00, 0x05 };
            var response = new WriteRegisterResponse(
                1,
                FunctionCode.WriteMultipleCoilRegisters,
                ModbusProtocolFormat.RTU,
                0x0010,
                values
            );

            bool isMatch = client.InvokeReplyMatchHandle(request, response);
            Assert.False(isMatch, "写多个线圈数量不一致时应返回 false");
        }

        [Fact]
        public void Match_WriteSingleCoilRegister_WhenTrue_ShouldMatch()
        {
            var client = new TestModbusClient();

            // 请求：写入 true (ON -> 0xFF00)
            var request = new WriteSingleCoilRegisterRequest
            {
                SlaveId = 1,
                WriteAddress = 0x0005,
                Data = true
            };

            // 响应：回显 0xFF00
            var response = new WriteRegisterResponse(
                1,
                FunctionCode.WriteSingleCoilRegister,
                ModbusProtocolFormat.RTU,
                0x0005,
                new byte[] { 0xFF, 0x00 }
            );

            // 以前的 Bug：rd.GetUInt16() (0xFF00=65280) == (Data ? 1 : 0) -> 65280 == 1 为 false
            // 修复后应比较 0xFF00，返回 true
            bool isMatch = client.InvokeReplyMatchHandle(request, response);
            Assert.True(isMatch, "写单个线圈置 true 时，应正确比对 0xFF00 并返回 true");
        }

        [Fact]
        public void Match_WriteSingleCoilRegister_WhenFalse_ShouldMatch()
        {
            var client = new TestModbusClient();

            // 请求：写入 false (OFF -> 0x0000)
            var request = new WriteSingleCoilRegisterRequest
            {
                SlaveId = 1,
                WriteAddress = 0x0005,
                Data = false
            };

            // 响应：回显 0x0000
            var response = new WriteRegisterResponse(
                1,
                FunctionCode.WriteSingleCoilRegister,
                ModbusProtocolFormat.RTU,
                0x0005,
                new byte[] { 0x00, 0x00 }
            );

            bool isMatch = client.InvokeReplyMatchHandle(request, response);
            Assert.True(isMatch, "写单个线圈置 false 时，应正确比对 0x0000 并返回 true");
        }

        [Fact]
        public void Match_WriteSingleCoilRegister_WhenValueMismatches_ShouldNotMatch()
        {
            var client = new TestModbusClient();

            // 请求写入 true (期望 0xFF00)，但从机回复 0x0000
            var request = new WriteSingleCoilRegisterRequest
            {
                SlaveId = 1,
                WriteAddress = 0x0005,
                Data = true
            };

            var response = new WriteRegisterResponse(
                1,
                FunctionCode.WriteSingleCoilRegister,
                ModbusProtocolFormat.RTU,
                0x0005,
                new byte[] { 0x00, 0x00 }
            );

            bool isMatch = client.InvokeReplyMatchHandle(request, response);
            Assert.False(isMatch, "写入值与响应回显不一致时不应匹配");
        }

        [Fact]
        public void Match_ReadCoilRegister_LengthCheck_ShouldValidateByteCount()
        {
            var client = new TestModbusClient();

            // 请求读取 10 个线圈 (需要 2 个字节)
            var request = new ReadCoilRegisterRequest
            {
                SlaveId = 1,
                StartAddress = 0,
                Length = 10
            };

            // 响应包含 2 个字节的线圈数据
            var responseSuccess = new ReadCoilRegisterResponse(
                1,
                ModbusProtocolFormat.RTU,
                new bool[10] // Length 将自动计算为 2 字节
            );

            Assert.True(client.InvokeReplyMatchHandle(request, responseSuccess), "字节数相符时应匹配成功");

            // 模拟另一个长度不符的响应 (例如读取了 20 个线圈，3 字节)
            var responseMismatched = new ReadCoilRegisterResponse(
                1,
                ModbusProtocolFormat.RTU,
                new bool[20] // Length 将计算为 3 字节
            );

            Assert.False(client.InvokeReplyMatchHandle(request, responseMismatched), "字节数不相符时应拒绝匹配");
        }

        [Fact]
        public void Match_ReadDiscreteInputRegister_LengthCheck_ShouldValidateByteCount()
        {
            var client = new TestModbusClient();

            // 请求读取 8 个离散输入 (需要 1 个字节)
            var request = new ReadDiscreteInputRegisterRequest
            {
                SlaveId = 1,
                StartAddress = 0,
                Length = 8
            };

            var responseSuccess = new ReadDiscreteInputRegisterResponse(
                1,
                ModbusProtocolFormat.RTU,
                new bool[8] // 1 字节
            );

            Assert.True(client.InvokeReplyMatchHandle(request, responseSuccess));

            var responseMismatched = new ReadDiscreteInputRegisterResponse(
                1,
                ModbusProtocolFormat.RTU,
                new bool[16] // 2 字节
            );

            Assert.False(client.InvokeReplyMatchHandle(request, responseMismatched));
        }

        [Fact]
        public void Match_WhenSlaveError_ShouldMatch_WithoutCheckingData()
        {
            var client = new TestModbusClient();

            var request = new ReadHoldingRegisterRequest
            {
                SlaveId = 1,
                StartAddress = 100,
                Length = 10
            };

            // 从机返回异常码 (例如 IllegalDataAddress)
            var errorResponse = new ModbusResponse(1, FunctionCode.ReadHoldingRegister, ModbusProtocolFormat.RTU, ModbusErrorCode.IllegalDataAddress);

            Assert.True(client.InvokeReplyMatchHandle(request, errorResponse), "从机报错时应提前返回匹配成功以便上层捕获错误码");
        }

        [Fact]
        public void Match_DifferentSlaveIdOrFunctionCode_ShouldNotMatch()
        {
            var client = new TestModbusClient();

            var request = new ReadHoldingRegisterRequest
            {
                SlaveId = 1,
                StartAddress = 100,
                Length = 2
            };

            var diffSlaveResp = new ReadHoldingRegisterResponse(
                2, // 不同的从站ID
                ModbusProtocolFormat.RTU,
                new byte[] { 0, 1, 0, 2 }
            );

            Assert.False(client.InvokeReplyMatchHandle(request, diffSlaveResp), "从站ID不同时不应匹配");

            var diffFuncResp = new ModbusResponse(1, FunctionCode.ReadInputRegister, ModbusProtocolFormat.RTU);
            Assert.False(client.InvokeReplyMatchHandle(request, diffFuncResp), "功能码不同时不应匹配");
        }
    }
}
