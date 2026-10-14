using STTech.BytesIO.Core;
using STTech.BytesIO.Core.Component;
using STTech.BytesIO.Modbus;
using STTech.BytesIO.Tcp;
using System;
using System.Buffers;
using System.Text;
using Xunit;

namespace STTech.BytesIO.Modbus.Tests
{
    public class ModbusUnpackerTests
    {
        [Fact]
        public void Unpacker_Ascii_FunctionCode0F_ShouldParseSuccessfully_WithoutFormatException()
        {
            var unpacker = new ModbusUnpacker(new ModbusTcpClient(ModbusProtocolFormat.ASCII), ModbusProtocolFormat.ASCII);

            // 构造一个 ASCII 写多个线圈响应报文:
            // : 01 0F 0010 000A LRC CRLF
            // :010F0010000A + LRC + \r\n
            string msg = ":010F0010000A";
            string fullLine = msg + ModbusRequest.LRC(msg) + "\r\n";
            byte[] bytes = Encoding.ASCII.GetBytes(fullLine);

            // 验证长度计算
            var seq = new ReadOnlySequence<byte>(bytes);
            var calcMethod = typeof(ModbusUnpacker).GetMethod("CalculateAsciiPacketLengthHandler", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            Assert.NotNull(calcMethod);

            object[] args = new object[] { seq };
            int len = (int)calcMethod.Invoke(null, args);
            Assert.Equal(17, len);

            // 验证反序列化
            var context = new UnpackContext(seq);
            var respMethod = typeof(ModbusUnpacker).GetMethod("ResponseSerializeHandler", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Assert.NotNull(respMethod);

            var resp = (ModbusResponse)respMethod.Invoke(unpacker, new object[] { context });
            Assert.NotNull(resp);
            Assert.IsType<WriteRegisterResponse>(resp);
            Assert.Equal(FunctionCode.WriteMultipleCoilRegisters, resp.FunctionCode);
            Assert.Equal(1, resp.SlaveId);
        }

        [Fact]
        public void Unpacker_Ascii_FunctionCode10_ShouldRecognizeWriteMultipleHoldingRegisters()
        {
            var unpacker = new ModbusUnpacker(new ModbusTcpClient(ModbusProtocolFormat.ASCII), ModbusProtocolFormat.ASCII);

            // 构造写多个保持寄存器响应报文: :011000010002 + LRC + \r\n
            string msg = ":011000010002";
            string fullLine = msg + ModbusRequest.LRC(msg) + "\r\n";
            byte[] bytes = Encoding.ASCII.GetBytes(fullLine);

            var seq = new ReadOnlySequence<byte>(bytes);
            var context = new UnpackContext(seq);
            var respMethod = typeof(ModbusUnpacker).GetMethod("ResponseSerializeHandler", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Assert.NotNull(respMethod);

            var resp = (ModbusResponse)respMethod.Invoke(unpacker, new object[] { context });
            Assert.NotNull(resp);
            Assert.IsType<WriteRegisterResponse>(resp);
            Assert.Equal(FunctionCode.WriteMultipleHoldingRegisters, resp.FunctionCode);
        }

        [Fact]
        public void Unpacker_Ascii_ExceptionResponse_ShouldParseCorrectly()
        {
            var unpacker = new ModbusUnpacker(new ModbusTcpClient(ModbusProtocolFormat.ASCII), ModbusProtocolFormat.ASCII);

            // 从机 1，读保持寄存器 0x03 异常应答 (0x83)，错误码 0x02 (IllegalDataAddress)
            // : 01 83 02 LRC CRLF (总长 11)
            string msg = ":018302";
            string fullLine = msg + ModbusRequest.LRC(msg) + "\r\n";
            byte[] bytes = Encoding.ASCII.GetBytes(fullLine);

            var seq = new ReadOnlySequence<byte>(bytes);
            var context = new UnpackContext(seq);
            var respMethod = typeof(ModbusUnpacker).GetMethod("ResponseSerializeHandler", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Assert.NotNull(respMethod);

            var resp = (ModbusResponse)respMethod.Invoke(unpacker, new object[] { context });
            Assert.NotNull(resp);
            Assert.False(resp.IsSuccess);
            Assert.Equal(FunctionCode.ReadHoldingRegister, resp.FunctionCode);
            Assert.Equal(ModbusErrorCode.IllegalDataAddress, resp.ErrorCode);
        }

        [Fact]
        public void RequestUnpacker_Ascii_HexParsing_ShouldSucceed()
        {
            var client = new TcpClient();
            var unpacker = new ModbusRequestUnpacker(client, ModbusProtocolFormat.ASCII);

            // 从机 0x0A (10), 功能码 0x0F (15), 地址 0x0001, 数量 0x0008, 字节数 0x01, 数据 0xFF
            // : 0A 0F 0001 0008 01 FF LRC CRLF
            string msg = ":0A0F0001000801FF";
            string fullLine = msg + ModbusRequest.LRC(msg) + "\r\n";
            byte[] bytes = Encoding.ASCII.GetBytes(fullLine);

            var seq = new ReadOnlySequence<byte>(bytes);

            // 测试长度计算
            var calcMethod = typeof(ModbusRequestUnpacker).GetMethod("CalculateAsciiPacketLengthHandler", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            Assert.NotNull(calcMethod);

            object[] args = new object[] { seq };
            int len = (int)calcMethod.Invoke(null, args);
            Assert.Equal(bytes.Length, len);

            // 测试反序列化
            var context = new UnpackContext(seq);
            var respMethod = typeof(ModbusRequestUnpacker).GetMethod("ResponseSerializeHandler", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Assert.NotNull(respMethod);

            var req = (ModbusRequest)respMethod.Invoke(unpacker, new object[] { context });
            Assert.NotNull(req);
            Assert.Equal(10, req.SlaveId);
            Assert.Equal(FunctionCode.WriteMultipleCoilRegisters, req.FunctionCode);
            Assert.IsType<WriteMultipleCoilRegistersRequest>(req);
        }

        [Fact]
        public void ModbusClient_ProtocolFormatChange_ShouldUpdateStartMark()
        {
            var client = new ModbusSerialClient(ModbusProtocolFormat.ASCII);

            // 初始 ASCII 模式下应该有 StartMark ':'
            Assert.NotNull(client.Unpacker.StartMark);
            Assert.Equal((byte)':', client.Unpacker.StartMark[0]);

            // 切换为 RTU 模式后，StartMark 应为 null
            client.ProtocolFormat = ModbusProtocolFormat.RTU;
            Assert.Null(client.Unpacker.StartMark);

            // 再次切换回 ASCII 模式，StartMark 应该恢复为 ':'
            client.ProtocolFormat = ModbusProtocolFormat.ASCII;
            Assert.NotNull(client.Unpacker.StartMark);
            Assert.Equal((byte)':', client.Unpacker.StartMark[0]);
        }
    }
}
