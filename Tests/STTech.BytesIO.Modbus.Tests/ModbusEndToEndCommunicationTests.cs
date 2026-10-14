using STTech.BytesIO.Core;
using STTech.BytesIO.Modbus;
using STTech.BytesIO.Modbus.Monitors;
using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Xunit;

namespace STTech.BytesIO.Modbus.Tests
{
    public class ModbusServerFixture : IDisposable
    {
        public int Port { get; } = 50215;
        public ModbusTcpServer Server { get; }
        public VirtualModbusDevice Device { get; }
        public CoilRegisterValueMonitor CoilMonitor { get; }
        public HoldingRegisterValueMonitor HoldingMonitor { get; }

        public ModbusServerFixture()
        {
            Server = new ModbusTcpServer(ModbusProtocolFormat.RTU)
            {
                Port = Port,
                SlaveId = 1
            };

            Device = new VirtualModbusDevice(Server);

            CoilMonitor = new CoilRegisterValueMonitor(0, 100);
            Device.Mount(CoilMonitor);

            HoldingMonitor = new HoldingRegisterValueMonitor(0, 100);
            Device.Mount(HoldingMonitor);

            Server.Start();
            Thread.Sleep(100);
        }

        public void Dispose()
        {
            Server.Stop();
            Server.Dispose();
        }
    }

    [Collection("ModbusEndToEndTests")]
    public class ModbusEndToEndCommunicationTests : IClassFixture<ModbusServerFixture>
    {
        private readonly ModbusServerFixture _fixture;

        public ModbusEndToEndCommunicationTests(ModbusServerFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public void EndToEnd_WriteSingleCoilRegister_True_ShouldCompleteSuccessfully_WithoutTimeout()
        {
            using var client = new ModbusTcpClient(ModbusProtocolFormat.RTU)
            {
                Host = "127.0.0.1",
                Port = _fixture.Port
            };

            var connectResult = client.Connect();
            Assert.True(connectResult.IsSuccess, $"客户端连接应该成功: {connectResult.ErrorCode}");
            Thread.Sleep(50);

            var reply = client.WriteSingleCoilRegister(1, 0x0002, true, timeout: 8000);

            Assert.Equal(ReplyStatus.Completed, reply.Status);
            Assert.True(reply.IsCompleted);
            Assert.True(reply.GetResponse().IsSuccess);
            Assert.True(_fixture.CoilMonitor.Source[2], "虚拟设备中地址 2 的线圈值应被更新为 true");
        }

        [Fact]
        public void EndToEnd_WriteSingleCoilRegister_False_ShouldCompleteSuccessfully()
        {
            using var client = new ModbusTcpClient(ModbusProtocolFormat.RTU)
            {
                Host = "127.0.0.1",
                Port = _fixture.Port
            };

            var connectResult = client.Connect();
            Assert.True(connectResult.IsSuccess, $"客户端连接应该成功: {connectResult.ErrorCode}");
            Thread.Sleep(50);

            _fixture.CoilMonitor.Source[3] = true;

            var reply = client.WriteSingleCoilRegister(1, 0x0003, false, timeout: 8000);

            Assert.Equal(ReplyStatus.Completed, reply.Status);
            Assert.True(reply.IsCompleted);
            Assert.True(reply.GetResponse().IsSuccess);
            Assert.False(_fixture.CoilMonitor.Source[3], "虚拟设备中地址 3 的线圈值应被更新为 false");
        }

        [Fact]
        public void EndToEnd_WriteMultipleCoilRegisters_ShouldCompleteSuccessfully_WithoutTimeout()
        {
            using var client = new ModbusTcpClient(ModbusProtocolFormat.RTU)
            {
                Host = "127.0.0.1",
                Port = _fixture.Port
            };

            var connectResult = client.Connect();
            Assert.True(connectResult.IsSuccess, $"客户端连接应该成功: {connectResult.ErrorCode}");
            Thread.Sleep(50);

            bool[] writeData = new bool[] { true, false, true, true, false, false, true, false, true, true };
            
            var reply = client.WriteMultipleCoilRegisters(1, 10, writeData, timeout: 8000);

            Assert.Equal(ReplyStatus.Completed, reply.Status);
            Assert.True(reply.IsCompleted);
            Assert.True(reply.GetResponse().IsSuccess);

            for (int i = 0; i < writeData.Length; i++)
            {
                Assert.Equal(writeData[i], _fixture.CoilMonitor.Source[10 + i]);
            }
        }

        [Fact]
        public void EndToEnd_ReadCoilRegister_ShouldReturnCorrectData()
        {
            using var client = new ModbusTcpClient(ModbusProtocolFormat.RTU)
            {
                Host = "127.0.0.1",
                Port = _fixture.Port
            };

            var connectResult = client.Connect();
            Assert.True(connectResult.IsSuccess, $"客户端连接应该成功: {connectResult.ErrorCode}");
            Thread.Sleep(50);

            _fixture.CoilMonitor.Source[0] = true;
            _fixture.CoilMonitor.Source[1] = false;
            _fixture.CoilMonitor.Source[2] = true;
            _fixture.CoilMonitor.Source[3] = true;

            var reply = client.ReadCoilRegister(1, 0, 4, timeout: 8000);

            Assert.Equal(ReplyStatus.Completed, reply.Status);
            Assert.True(reply.IsCompleted);
            Assert.True(reply.GetResponse().IsSuccess);
            Assert.NotNull(reply.GetResponse());

            var values = reply.GetResponse().Values;
            Assert.True(values[0]);
            Assert.False(values[1]);
            Assert.True(values[2]);
            Assert.True(values[3]);
        }

        [Fact]
        public void EndToEnd_HoldingRegisters_ReadAndWrite_ShouldWork()
        {
            using var client = new ModbusTcpClient(ModbusProtocolFormat.RTU)
            {
                Host = "127.0.0.1",
                Port = _fixture.Port
            };

            var connectResult = client.Connect();
            Assert.True(connectResult.IsSuccess, $"客户端连接应该成功: {connectResult.ErrorCode}");
            Thread.Sleep(50);

            ushort[] writeValues = new ushort[] { 100, 200, 300 };
            var writeReply = client.WriteMultipleHoldingRegisters(1, 20, writeValues, timeout: 8000);

            Assert.Equal(ReplyStatus.Completed, writeReply.Status);
            Assert.True(writeReply.IsCompleted);
            Assert.True(writeReply.GetResponse().IsSuccess);

            var readReply = client.ReadHoldingRegister(1, 20, 3, timeout: 8000);

            Assert.Equal(ReplyStatus.Completed, readReply.Status);
            Assert.True(readReply.IsCompleted);
            Assert.True(readReply.GetResponse().IsSuccess);

            var readValues = readReply.GetResponse().GetUInt16Array();
            Assert.Equal(writeValues, readValues);
        }
    }
}
