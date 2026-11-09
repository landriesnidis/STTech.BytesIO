using STTech.BytesIO.Core;
using STTech.BytesIO.Core.Component;
using STTech.BytesIO.Modbus;
using STTech.BytesIO.Modbus.Entity;
using STTech.BytesIO.Modbus.Monitors;
using System;
using System.Buffers;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace STTech.BytesIO.Modbus.Tests
{
    public class AuditRemediationRegressionTests
    {
        /// <summary>
        /// 验证缺陷一：标准 RTU 读请求 (从地址 0 读取 2 个保持寄存器 01 03 00 00 00 02 C4 0B)
        /// 绝不应被误判为 MBAP，必须准确解析为 RTU！
        /// </summary>
        [Fact]
        public void Test_Defect1_RtuReadRequest_NotMisidentifiedAsMbap()
        {
            // 01 03 00 00 00 02 C4 0B (从站 1, 功能码 3, 起始地址 0, 长度 2, CRC C4 0B)
            byte[] rawRtuRequest = new byte[] { 0x01, 0x03, 0x00, 0x00, 0x00, 0x02, 0xC4, 0x0B };

            // 1. 未指定协议格式时反序列化
            var reqDefault = new ReadRegisterRequest(rawRtuRequest);
            Assert.Equal(ModbusProtocolFormat.RTU, reqDefault.ProtocolFormat);
            Assert.Equal(1, reqDefault.SlaveId);
            Assert.Equal(FunctionCode.ReadHoldingRegister, reqDefault.FunctionCode);
            Assert.Equal(0, reqDefault.StartAddress);
            Assert.Equal(2, reqDefault.Length);

            // 2. 显式指定 RTU 格式反序列化
            var reqRtu = new ReadRegisterRequest(rawRtuRequest, ModbusProtocolFormat.RTU);
            Assert.Equal(ModbusProtocolFormat.RTU, reqRtu.ProtocolFormat);
            Assert.Equal(1, reqRtu.SlaveId);
            Assert.Equal(FunctionCode.ReadHoldingRegister, reqRtu.FunctionCode);
            Assert.Equal(0, reqRtu.StartAddress);
            Assert.Equal(2, reqRtu.Length);
        }

        /// <summary>
        /// 验证缺陷八：ModbusRequestUnpacker 对超限 byteCount (> 246) 具备防御拦截自愈能力
        /// </summary>
        [Fact]
        public void Test_Defect8_RequestUnpacker_RejectsExcessiveByteCount()
        {
            var unpacker = new ModbusRequestUnpacker(null, ModbusProtocolFormat.RTU);

            // 构造一个受电磁噪声干扰的写多个保持寄存器请求：
            // head[1]=0x10 (WriteMultipleHoldingRegisters), head[6]=250 (超限 byteCount)
            byte[] corruptedNoise = new byte[] { 0x01, 0x10, 0x00, 0x01, 0x00, 0x7D, 250, 0x00, 0x00 };
            var seq = new ReadOnlySequence<byte>(corruptedNoise);

            // 通过反射调用受保护的 CalculatePacketLength
            var method = typeof(ModbusRequestUnpacker).GetMethod("CalculatePacketLength", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Assert.NotNull(method);

            int result = (int)method.Invoke(unpacker, new object[] { seq });

            // 应该返回 -1 (步进 1 字节丢弃自愈)，绝不返回 0 永久挂起死锁！
            Assert.Equal(-1, result);
        }

        /// <summary>
        /// 验证缺陷九：WriteMultipleCoilRegistersRequest 和 WriteMultipleHoldingRegistersRequest
        /// 零分配高性能序列化与反序列化逻辑
        /// </summary>
        [Fact]
        public void Test_Defect9_WriteMultipleRequests_SerializationAndDeserialization()
        {
            // 1. 写多个保持寄存器
            var writeHolding = new WriteMultipleHoldingRegistersRequest
            {
                SlaveId = 1,
                WriteAddress = 100,
                Data = new byte[] { 0x12, 0x34, 0x56, 0x78 } // 2 个寄存器
            };
            byte[] holdingBytes = writeHolding.GetBytes();
            Assert.NotNull(holdingBytes);

            var deserializedHolding = new WriteMultipleHoldingRegistersRequest(holdingBytes, ModbusProtocolFormat.RTU);
            Assert.Equal(100, deserializedHolding.WriteAddress);
            Assert.Equal(new byte[] { 0x12, 0x34, 0x56, 0x78 }, deserializedHolding.Data);
            Assert.Equal(new ushort[] { 0x1234, 0x5678 }, deserializedHolding.GetUInt16Array());

            // 2. 写多个线圈寄存器
            var writeCoil = new WriteMultipleCoilRegistersRequest
            {
                SlaveId = 2,
                WriteAddress = 20,
                Data = new bool[] { true, false, true, true, false, false, true, false, true, true } // 10 个线圈 (跨 2 字节)
            };
            byte[] coilBytes = writeCoil.GetBytes();
            Assert.NotNull(coilBytes);

            var deserializedCoil = new WriteMultipleCoilRegistersRequest(coilBytes, ModbusProtocolFormat.RTU);
            Assert.Equal(20, deserializedCoil.WriteAddress);
            Assert.Equal(10, deserializedCoil.Data.Length);
            Assert.Equal(writeCoil.Data, deserializedCoil.Data);
        }

        /// <summary>
        /// 验证缺陷二：非 8 倍数线圈监视器更新不会崩溃！
        /// 监视 10 个线圈，从机响应返回 2 字节 (16 bit)，必须成功截断并正常更新！
        /// </summary>
        [Fact]
        public void Test_Defect2_CoilMonitorNon8Multiple_DoesNotCrash()
        {
            var coilMonitor = new CoilRegisterValueMonitor(startAddress: 0, length: 10);
            Assert.Equal(10, coilMonitor.Source.Length);

            // 构造读线圈从机响应报文: SlaveId(1), Func(1), ByteCount(1), Data(2B), CRC(2B)
            // 01 01 02 05 03 [CRC16]
            byte[] rawResp = new byte[7];
            rawResp[0] = 0x01; // SlaveId
            rawResp[1] = 0x01; // Func = ReadCoil
            rawResp[2] = 0x02; // ByteCount = 2
            rawResp[3] = 0x05; // 0b00000101
            rawResp[4] = 0x03; // 0b00000011
            var crc = ModbusCrc16.Compute(rawResp.AsSpan(0, 5));
            rawResp[5] = (byte)(crc & 0xFF);
            rawResp[6] = (byte)(crc >> 8);

            var resp = new ReadCoilRegisterResponse(rawResp, ModbusProtocolFormat.RTU);
            Assert.True(resp.IsSuccess);
            Assert.NotNull(resp.Values);
            Assert.Equal(16, resp.Values.Length); // 2 字节展开为 16 个 bit

            // 手动触发 ApplyResponseToMonitor 更新监视器
            var extType = typeof(ModbusRegisterValueMonitorExtensions);
            var applyMethod = extType.GetMethod("ApplyResponseToMonitor", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            Assert.NotNull(applyMethod);

            var genericMethod = applyMethod.MakeGenericMethod(typeof(bool));
            
            // 执行更新，验证绝无抛出 InvalidOperationException 异常！
            var exception = Record.Exception(() => genericMethod.Invoke(null, new object[] { coilMonitor, resp }));
            Assert.Null(exception);

            // 验证数值正确更新，且前 10 个线圈数值匹配：
            // 0x05 -> bit0=1, bit1=0, bit2=1, bit3=0, bit4=0, bit5=0, bit6=0, bit7=0
            // 0x03 -> bit8=1, bit9=1
            Assert.True(coilMonitor.Source[0]);
            Assert.False(coilMonitor.Source[1]);
            Assert.True(coilMonitor.Source[2]);
            Assert.False(coilMonitor.Source[3]);
            Assert.True(coilMonitor.Source[8]);
            Assert.True(coilMonitor.Source[9]);
        }

        /// <summary>
        /// 验证缺陷五：从机返回故障响应时，ReadFloat / ReadInt32 绝不吞异常与报空指针
        /// </summary>
        [Fact]
        public void Test_Defect5_HighLevelRead_PreservesErrorCode_NoNullReference()
        {
            // 构造一个从站报错的真实 RTU 响应 (01 83 02 C0 F1: Slave=1, ErrFunc=0x83, ErrorCode=0x02 IllegalDataAddress, CRC)
            byte[] errBytes = new byte[] { 0x01, 0x83, 0x02, 0xC0, 0xF1 };
            var errorRaw = new ReadHoldingRegisterResponse(errBytes);
            Assert.False(errorRaw.IsSuccess);
            Assert.Equal(ModbusErrorCode.IllegalDataAddress, errorRaw.ErrorCode);

            var reply = new Reply<ReadHoldingRegisterResponse>(null, errorRaw);

            // 通过 HighLevel 的通用包装方法测试
            var extType = typeof(ModbusClient);
            var wrapMethod = extType.GetMethod("WrapDataResponse", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            Assert.NotNull(wrapMethod);

            var genericWrap = wrapMethod.MakeGenericMethod(typeof(float));
            Func<byte[], float> converter = b => 0.0f;

            var wrappedReply = (Reply<ModbusDataResponse<float>>)genericWrap.Invoke(null, new object[] { reply, 4, converter });

            // 验证：
            Assert.NotNull(wrappedReply);
            Assert.True(wrappedReply.IsCompleted); // 网络通信完成了
            var dataResp = wrappedReply.GetResponse();
            Assert.NotNull(dataResp); // 绝不为 null，杜绝外部 NRE 崩溃！
            Assert.False(dataResp.IsSuccess); // 明确标记业务失败
            Assert.Equal(ModbusErrorCode.IllegalDataAddress, dataResp.ErrorCode); // 原始错误码完整保留！
        }

        /// <summary>
        /// 验证缺陷七：VirtualModbusDevice 在锁外调用 Notify，即使回调中重入查询设备也绝不死锁！
        /// </summary>
        [Fact]
        public void Test_Defect7_VirtualModbusDevice_NoDeadlockOnReentrantQuery()
        {
            var server = new DummyModbusServer();
            var device = new VirtualModbusDevice(server);

            var reg = new HoldingRegisterValueMonitor(0, 10);
            device.Mount(reg);

            bool callbackInvoked = false;
            reg.ValueChanged += (sender, e) =>
            {
                callbackInvoked = true;
                // 在回调中重入访问 device，若在锁内触发则在此必然死锁挂起！
                device.Unmount(reg);
                device.Mount(reg);
            };

            // 模拟收到写入请求
            var writeReq = new WriteSingleHoldingRegisterRequest
            {
                SlaveId = 1,
                WriteAddress = 0,
                Data = new byte[] { 0x00, 0x64 } // 100
            };

            var completedEvent = new ManualResetEventSlim(false);
            Task.Run(() =>
            {
                server.TriggerWriteHolding(writeReq);
                completedEvent.Set();
            });

            // 等待执行完成，若 5000ms 内未完成说明发生死锁！
            bool finished = completedEvent.Wait(5000);
            Assert.True(finished, "发生死锁！VirtualModbusDevice 依然在锁内触发了 Notify 回调！");
            Assert.True(callbackInvoked);
            Assert.Equal(100, reg.Source[0]);
        }

        /// <summary>
        /// 验证缺陷三：ModbusServer 下游网关转发完成协议转译与定向返回，绝不产生广播投毒
        /// </summary>
        [Fact]
        public void Test_Defect3_ModbusServer_GatewayTranslationAndNoBroadcastPoisoning()
        {
            var server = new DummyModbusServer();
            var downstream = new DummyBytesClient();
            server.DownstreamClient = downstream;

            var clientA = new DummyBytesClient();
            var clientB = new DummyBytesClient();

            // 上游客户端 A 通过 MBAP 向从站 2 发起请求 (SlaveId=2, Function=0x03, StartAddress=0, Length=2)
            // MBAP Header: TransactionId=0x1234, ProtocolId=0x0000, Length=0x0006, SlaveId=2, FunctionCode=0x03, Addr=0x0000, Qty=0x0002
            byte[] mbapReq = new byte[] { 0x12, 0x34, 0x00, 0x00, 0x00, 0x06, 0x02, 0x03, 0x00, 0x00, 0x00, 0x02 };
            var forwardReq = new ModbusForwardRequest(new UnpackContext(new ReadOnlySequence<byte>(mbapReq)), ModbusProtocolFormat.MBAP);

            server.DispatchForward(forwardReq, clientA);

            // 1. 验证发给下游的报文被正确转译为 RTU 格式 (剥除了 MBAP 头，带上了 CRC16)
            Assert.NotNull(downstream.LastSentData);
            Assert.Equal(8, downstream.LastSentData.Length); // 2(SlaveId+Func) + 4(PDU) + 2(CRC) = 8
            Assert.Equal(0x02, downstream.LastSentData[0]); // SlaveId = 2
            Assert.Equal(0x03, downstream.LastSentData[1]); // Func = 3
            Assert.True(ModbusCrc16.Check(downstream.LastSentData)); // CRC16 合法！

            // 2. 模拟下游返回 RTU 响应：从站 2 返回 2 个寄存器 (4 字节数据) + CRC16
            // 02 03 04 11 22 33 44 [CRC16]
            byte[] rtuResp = new byte[7 + 2];
            rtuResp[0] = 0x02;
            rtuResp[1] = 0x03;
            rtuResp[2] = 0x04;
            rtuResp[3] = 0x11; rtuResp[4] = 0x22; rtuResp[5] = 0x33; rtuResp[6] = 0x44;
            var crc = ModbusCrc16.Compute(rtuResp.AsSpan(0, 7));
            rtuResp[7] = (byte)(crc & 0xFF);
            rtuResp[8] = (byte)(crc >> 8);

            // 下游接收到响应
            downstream.SimulateReceive(rtuResp);

            // 3. 验证客户端 A 收到了转译后的完整 MBAP 报文
            Assert.NotNull(clientA.LastSentData);
            Assert.Equal(13, clientA.LastSentData.Length); // 6(MBAP Head) + 7(PDU) = 13
            Assert.Equal(0x12, clientA.LastSentData[0]);   // TransactionId 保持一致！
            Assert.Equal(0x34, clientA.LastSentData[1]);
            Assert.Equal(0x02, clientA.LastSentData[6]);   // SlaveId = 2

            // 4. 关键验证：客户端 B 绝不能收到任何数据（无广播投毒）！
            Assert.Null(clientB.LastSentData);
        }

        #region Helper Dummy Classes for Testing

        private class DummyModbusServer : ModbusServer
        {
            public override bool IsRunning => true;

            protected override void BroadcastUpstream(byte[] data)
            {
                // 仅作兜底广播测试
            }

            public void TriggerWriteHolding(WriteSingleHoldingRegisterRequest req)
            {
                HandleRequest(req, new DummyBytesClient());
            }

            public void DispatchForward(ModbusForwardRequest req, BytesClient sender)
            {
                HandleRequest(req, sender);
            }
        }

        private class DummyBytesClient : BytesClient
        {
            public byte[] LastSentData { get; private set; }
            public override bool IsConnected => true;

            public override ConnectResult Connect(ConnectArgument argument = null) => new ConnectResult();
            public override DisconnectResult Disconnect(DisconnectArgument argument = null) => new DisconnectResult();
            public override void Dispose() { }

            protected override Task SendHandlerAsync(SendArgs data)
            {
                LastSentData = data.Data;
                return Task.CompletedTask;
            }

            protected override void ReceiveDataCompletedHandle() { }

            protected override Task ReceiveDataHandleAsync(CancellationToken cancellationToken) => Task.CompletedTask;

            public void SimulateReceive(byte[] data)
            {
                RaiseDataReceived(this, new DataReceivedEventArgs(new ReceiveContext(data), 0));
            }
        }

        #endregion
    }
}
