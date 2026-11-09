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
        public void Unpacker_Ascii_FunctionCode0F_ShouldParseSuccessfully_ViaPublicContract()
        {
            var unpacker = new ModbusUnpacker(new ModbusTcpClient(ModbusProtocolFormat.ASCII), ModbusProtocolFormat.ASCII);
            ModbusResponse parsedResponse = null;

            unpacker.OnDataParsed += (s, e) =>
            {
                parsedResponse = e.Data;
            };

            // 构造一个 ASCII 写多个线圈响应报文:
            // : 01 0F 0010 000A LRC CRLF
            string msg = ":010F0010000A";
            string fullLine = msg + ModbusRequest.LRC(msg) + "\r\n";
            byte[] bytes = Encoding.ASCII.GetBytes(fullLine);

            // 通过公有契约 Input 灌入数据
            using var ctx = new ReceiveContext(bytes);
            unpacker.Input(ctx);

            Assert.NotNull(parsedResponse);
            Assert.IsType<WriteRegisterResponse>(parsedResponse);
            Assert.Equal(FunctionCode.WriteMultipleCoilRegisters, parsedResponse.FunctionCode);
            Assert.Equal(1, parsedResponse.SlaveId);
        }

        [Fact]
        public void Unpacker_Ascii_FunctionCode10_ShouldRecognizeWriteMultipleHoldingRegisters()
        {
            var unpacker = new ModbusUnpacker(new ModbusTcpClient(ModbusProtocolFormat.ASCII), ModbusProtocolFormat.ASCII);
            ModbusResponse parsedResponse = null;

            unpacker.OnDataParsed += (s, e) =>
            {
                parsedResponse = e.Data;
            };

            // 构造写多个保持寄存器响应报文: :011000010002 + LRC + \r\n
            string msg = ":011000010002";
            string fullLine = msg + ModbusRequest.LRC(msg) + "\r\n";
            byte[] bytes = Encoding.ASCII.GetBytes(fullLine);

            using var ctx = new ReceiveContext(bytes);
            unpacker.Input(ctx);

            Assert.NotNull(parsedResponse);
            Assert.IsType<WriteRegisterResponse>(parsedResponse);
            Assert.Equal(FunctionCode.WriteMultipleHoldingRegisters, parsedResponse.FunctionCode);
        }

        [Fact]
        public void Unpacker_Ascii_ExceptionResponse_ShouldParseCorrectly()
        {
            var unpacker = new ModbusUnpacker(new ModbusTcpClient(ModbusProtocolFormat.ASCII), ModbusProtocolFormat.ASCII);
            ModbusResponse parsedResponse = null;

            unpacker.OnDataParsed += (s, e) =>
            {
                parsedResponse = e.Data;
            };

            // 从机 1，读保持寄存器 0x03 异常应答 (0x83)，错误码 0x02 (IllegalDataAddress)
            string msg = ":018302";
            string fullLine = msg + ModbusRequest.LRC(msg) + "\r\n";
            byte[] bytes = Encoding.ASCII.GetBytes(fullLine);

            using var ctx = new ReceiveContext(bytes);
            unpacker.Input(ctx);

            Assert.NotNull(parsedResponse);
            Assert.False(parsedResponse.IsSuccess);
            Assert.Equal(FunctionCode.ReadHoldingRegister, parsedResponse.FunctionCode);
            Assert.Equal(ModbusErrorCode.IllegalDataAddress, parsedResponse.ErrorCode);
        }

        [Fact]
        public void RequestUnpacker_Ascii_HexParsing_ShouldSucceed()
        {
            var client = new TcpClient();
            var unpacker = new ModbusRequestUnpacker(client, ModbusProtocolFormat.ASCII);
            ModbusRequest parsedRequest = null;

            unpacker.OnDataParsed += (s, e) =>
            {
                parsedRequest = e.Data;
            };

            // 从机 0x0A (10), 功能码 0x0F (15), 地址 0x0001, 数量 0x0008, 字节数 0x01, 数据 0xFF
            string msg = ":0A0F0001000801FF";
            string fullLine = msg + ModbusRequest.LRC(msg) + "\r\n";
            byte[] bytes = Encoding.ASCII.GetBytes(fullLine);

            using var ctx = new ReceiveContext(bytes);
            unpacker.Input(ctx);

            Assert.NotNull(parsedRequest);
            Assert.Equal(10, parsedRequest.SlaveId);
            Assert.Equal(FunctionCode.WriteMultipleCoilRegisters, parsedRequest.FunctionCode);
            Assert.IsType<WriteMultipleCoilRegistersRequest>(parsedRequest);
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
