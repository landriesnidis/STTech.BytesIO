using STTech.BytesIO.Core.Component;
using STTech.BytesIO.Modbus;
using STTech.BytesIO.Modbus.Entity;
using System;
using System.Buffers;
using System.Text;
using Xunit;

namespace STTech.BytesIO.Modbus.Tests
{
    public class ModbusFunctionCodeAndExceptionTests
    {
        [Fact]
        public void FunctionCode04_ReadInputRegisterRequest_ShouldSerializeCorrectly()
        {
            var req = new ReadInputRegisterRequest
            {
                SlaveId = 1,
                StartAddress = 0x000A,
                Length = 2,
                ProtocolFormat = ModbusProtocolFormat.RTU
            };

            Assert.Equal(FunctionCode.ReadInputRegister, req.FunctionCode);
            byte[] bytes = req.GetBytes();
            Assert.Equal(8, bytes.Length); // Slave(1) + Func(1) + Addr(2) + Len(2) + CRC(2)
            Assert.Equal(1, bytes[0]);
            Assert.Equal(0x04, bytes[1]);
            Assert.Equal(0x00, bytes[2]);
            Assert.Equal(0x0A, bytes[3]);
            Assert.Equal(0x00, bytes[4]);
            Assert.Equal(0x02, bytes[5]);
        }

        [Fact]
        public void FunctionCode06_WriteSingleHoldingRegisterRequest_ShouldSerializeAndDeserialize()
        {
            var req = new WriteSingleHoldingRegisterRequest
            {
                SlaveId = 2,
                WriteAddress = 0x0015,
                Data = new byte[] { 0x12, 0x34 },
                ProtocolFormat = ModbusProtocolFormat.RTU
            };

            Assert.Equal(FunctionCode.WriteSingleHoldingRegister, req.FunctionCode);
            byte[] bytes = req.GetBytes();
            Assert.Equal(8, bytes.Length);
            Assert.Equal(2, bytes[0]);
            Assert.Equal(0x06, bytes[1]);
            Assert.Equal(0x00, bytes[2]);
            Assert.Equal(0x15, bytes[3]);
            Assert.Equal(0x12, bytes[4]);
            Assert.Equal(0x34, bytes[5]);

            // 反序列化还原
            var restored = new WriteSingleHoldingRegisterRequest(bytes);
            Assert.Equal(2, restored.SlaveId);
            Assert.Equal(0x0015, restored.WriteAddress);
            Assert.Equal(new byte[] { 0x12, 0x34 }, restored.Data);
        }

        [Fact]
        public void ModbusRtu_ExceptionResponse_ShouldParseFailureAndErrorCode()
        {
            // 从站 1，功能码 0x03 异常（0x83），错误码 0x02（非法数据地址），CRC(0xC0, 0xF1)
            byte[] errFrame = new byte[] { 0x01, 0x83, 0x02, 0xC0, 0xF1 };
            var resp = new ModbusResponse(errFrame);

            Assert.False(resp.IsSuccess);
            Assert.Equal(1, resp.SlaveId);
            Assert.Equal(FunctionCode.ReadHoldingRegister, resp.FunctionCode);
            Assert.Equal(ModbusErrorCode.IllegalDataAddress, resp.ErrorCode);
            Assert.Equal(ModbusProtocolFormat.RTU, resp.ProtocolFormat);

            // 验证错误码扩展描述
            string desc = resp.ErrorCode.GetErrorDescription();
            Assert.False(string.IsNullOrEmpty(desc));

            // 验证 ModbusErrorCodeException 包装
            var ex = new ModbusErrorCodeException(resp.ErrorCode);
            Assert.Equal(ModbusErrorCode.IllegalDataAddress, ex.ErrorCode);
            Assert.Equal(desc, ex.Message);
        }

        [Fact]
        public void ModbusAscii_ExceptionResponse_ShouldParseFailureAndErrorCode()
        {
            // ASCII 异常帧：: 01 81 01 7D \r\n (从站1，功能码01异常，错误码01非法功能)
            string asciiFrame = ":0181017D\r\n";
            byte[] bytes = Encoding.ASCII.GetBytes(asciiFrame);
            var resp = new ModbusResponse(bytes);

            Assert.False(resp.IsSuccess);
            Assert.Equal(1, resp.SlaveId);
            Assert.Equal(FunctionCode.ReadCoilRegister, resp.FunctionCode);
            Assert.Equal(ModbusErrorCode.IllegalFunction, resp.ErrorCode);
            Assert.Equal(ModbusProtocolFormat.ASCII, resp.ProtocolFormat);
        }

        [Fact]
        public void ModbusForwardRequest_RtuAndAscii_ShouldPreserveRawPayload()
        {
            // 1. RTU 透传转发
            byte[] rtuBytes = new byte[] { 0x05, 0x03, 0x00, 0x00, 0x00, 0x01, 0x85, 0x8E };
            var rtuSeq = new ReadOnlySequence<byte>(rtuBytes);
            var rtuContext = new UnpackContext(rtuSeq);
            var forwardRtu = new ModbusForwardRequest(rtuContext);

            Assert.Equal(5, forwardRtu.SlaveId);
            Assert.Equal(ModbusProtocolFormat.RTU, forwardRtu.ProtocolFormat);
            Assert.Equal(rtuBytes, forwardRtu.RawBytes);

            // 2. ASCII 透传转发
            byte[] asciiBytes = Encoding.ASCII.GetBytes(":020300000001F8\r\n");
            var asciiSeq = new ReadOnlySequence<byte>(asciiBytes);
            var asciiContext = new UnpackContext(asciiSeq);
            var forwardAscii = new ModbusForwardRequest(asciiContext);

            Assert.Equal(2, forwardAscii.SlaveId);
            Assert.Equal(ModbusProtocolFormat.ASCII, forwardAscii.ProtocolFormat);
            Assert.Equal(asciiBytes, forwardAscii.RawBytes);
        }
    }
}
