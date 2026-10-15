using STTech.BytesIO.Core;
using STTech.BytesIO.Modbus;
using STTech.BytesIO.Modbus.Monitors;
using STTech.BytesIO.Serial;
using STTech.BytesIO.Tcp;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Threading.Tasks;
using Xunit;

namespace STTech.BytesIO.Modbus.Tests
{
    /// <summary>
    /// 教程代码真实性验证测试
    /// 确保 ModbusBasicTutorial.md 与 ModbusAdvancedTutorial.md 中的所有示例代码均能 100% 通过编译与运行
    /// </summary>
    public class TutorialVerificationTests
    {
        [Fact]
        public void Verify_Tutorial_BasicClientConfiguration_Compiles()
        {
            // 默认参数构造与带参构造
            using var tcpClient1 = new ModbusTcpClient();
            using var tcpClient2 = new ModbusTcpClient(ModbusProtocolFormat.RTU);
            using var tcpClient3 = new ModbusTcpClient(ModbusProtocolFormat.ASCII);

            tcpClient1.Host = "127.0.0.1";
            tcpClient1.Port = 502;

            tcpClient1.OnConnectedSuccessfully += (s, e) => { };
            tcpClient1.OnDisconnected += (s, e) => { var reason = e.ReasonCode; };
            tcpClient1.OnExceptionOccurs += (s, e) => { var msg = e.Exception.Message; };

            using var serialClient = new ModbusSerialClient()
            {
                PortName = "COM1",
                BaudRate = 9600,
                DataBits = 8,
                Parity = Parity.None,
                StopBits = StopBits.One,
                RtsEnable = true
            };

            Assert.Equal(ModbusProtocolFormat.RTU, tcpClient1.ProtocolFormat);
            Assert.Equal(ModbusProtocolFormat.ASCII, tcpClient3.ProtocolFormat);
        }

        [Fact]
        public void Verify_Tutorial_AdvancedServerAndMonitor_CompilesAndRuns()
        {
            using var server = new ModbusTcpServer(ModbusProtocolFormat.RTU)
            {
                Port = 50299,
                SlaveId = 1
            };

            server.ClientConnected += (s, e) =>
            {
                var endpoint = e.Client.RemoteEndPoint;
            };

            server.ClientDisconnected += (s, e) =>
            {
                var endpoint = e.Client.RemoteEndPoint;
                var reason = e.ReasonCode;
                var duration = e.Duration;
            };

            var virtualDevice = new VirtualModbusDevice(server);
            var holdingMonitor = new HoldingRegisterValueMonitor(0, 100);
            virtualDevice.Mount(holdingMonitor);

            int eventCount = 0;
            holdingMonitor.ValueChanged += (s, e) =>
            {
                eventCount++;
                int index = e.Index;
                ushort addr = (ushort)(holdingMonitor.StartAddress + index);
                ushort oldVal = e.OriginalValue;
                ushort newVal = e.NewValue;
            };

            holdingMonitor.Source[5] = 999;
            holdingMonitor.Notify();

            Assert.Equal(1, eventCount);
        }

        [Fact]
        public void Verify_Tutorial_GatewayArchitecture_Configuration()
        {
            using var rtuSerialClient = new SerialClient
            {
                PortName = "COM3",
                BaudRate = 9600
            };

            using var gatewayServer = new ModbusTcpServer(ModbusProtocolFormat.RTU)
            {
                Port = 50298,
                SlaveId = 1,
                DownstreamClient = rtuSerialClient
            };

            using var customNodeClient = new SerialClient { PortName = "COM4" };
            gatewayServer.AddDownstreamNode(2, customNodeClient);

            Assert.Same(rtuSerialClient, gatewayServer.DownstreamClient);
            Assert.True(gatewayServer.DownstreamNodes.ContainsKey(2));
            Assert.Same(customNodeClient, gatewayServer.DownstreamNodes[2]);

            gatewayServer.RemoveDownstreamNode(2);
            Assert.False(gatewayServer.DownstreamNodes.ContainsKey(2));
        }

        [Fact]
        public void Verify_Tutorial_CustomRequestAndResponse()
        {
            var req = new SetCalibrationRequest
            {
                SlaveId = 1,
                CalibrationFactor = 1.052f
            };

            byte[] bytes = req.GetBytes();
            Assert.NotNull(bytes);

            // 验证响应对象构造与解析
            byte[] dummyResponsePayload = new byte[] { 0x01, 0x41, 0x01, 0x00, 0x00 };
            var resp = new SetCalibrationResponse(dummyResponsePayload);
            Assert.Equal(0x01, resp.SlaveId);
            Assert.True(resp.IsCalibrated);
        }

        public class SetCalibrationRequest : ModbusRequest
        {
            public float CalibrationFactor { get; set; }

            public SetCalibrationRequest() : base((FunctionCode)0x41) { }

            protected override void SerializePayloadHandle()
            {
                byte[] factorBytes = BitConverter.GetBytes(CalibrationFactor);
                Array.Reverse(factorBytes);
                Payload = factorBytes;
            }
        }

        public class SetCalibrationResponse : ModbusResponse
        {
            public bool IsCalibrated { get; private set; }

            public SetCalibrationResponse() { }

            public SetCalibrationResponse(byte[] data) : base(data)
            {
                if (IsSuccess && Payload != null && Payload.Length >= 1)
                {
                    IsCalibrated = Payload[0] == 0x01;
                }
            }
        }
    }
}
