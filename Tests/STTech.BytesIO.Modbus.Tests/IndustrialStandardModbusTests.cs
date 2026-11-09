using STTech.BytesIO.Core;
using STTech.BytesIO.Core.Component;
using STTech.BytesIO.Modbus;
using STTech.BytesIO.Tcp;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace STTech.BytesIO.Modbus.Tests
{
    /// <summary>
    /// 工业级产品标准核验测试
    /// 涵盖标准 MBAP 规范、严格 CRC 双向校验、滑动丢弃自愈、高阶端序变换与半双工并发安全
    /// </summary>
    public class IndustrialStandardModbusTests
    {
        #region 1. 标准 Modbus TCP (MBAP) 报文规范与无 CRC 核验

        [Fact]
        public void Mbap_RequestAndResponse_ShouldConformToStandard_WithoutCrc()
        {
            // 构造读保持寄存器请求 (MBAP 模式)
            var req = new ReadHoldingRegisterRequest()
            {
                ProtocolFormat = ModbusProtocolFormat.MBAP,
                TransactionId = 0x1234,
                ProtocolId = 0x0000,
                SlaveId = 1,
                StartAddress = 0x0064, // 100
                Length = 2
            };

            byte[] reqBytes = req.GetBytes();
            // MBAP: TransactionId(2B) + ProtocolId(2B) + Length(2B) + UnitId(1B) + Func(1B) + Addr(2B) + Qty(2B) = 12 字节
            Assert.Equal(12, reqBytes.Length);
            Assert.Equal(0x12, reqBytes[0]);
            Assert.Equal(0x34, reqBytes[1]);
            Assert.Equal(0x00, reqBytes[2]); // Protocol ID
            Assert.Equal(0x00, reqBytes[3]);
            Assert.Equal(0x00, reqBytes[4]); // Length = 6 (UnitId + Func + Addr + Qty)
            Assert.Equal(0x06, reqBytes[5]);
            Assert.Equal(1, reqBytes[6]);    // Unit ID
            Assert.Equal((byte)FunctionCode.ReadHoldingRegister, reqBytes[7]);

            // 构造对应的读保持寄存器应答 (MBAP 模式，2 个寄存器共 4 字节数据)
            var resp = new ReadHoldingRegisterResponse(1, ModbusProtocolFormat.MBAP, new byte[] { 0x41, 0x45, 0x85, 0x1F })
            {
                TransactionId = 0x1234,
                ProtocolId = 0x0000
            };

            byte[] respBytes = resp.GetBytes();
            // MBAP: TransactionId(2B) + ProtocolId(2B) + Length(2B) + UnitId(1B) + Func(1B) + ByteCount(1B) + Data(4B) = 13 字节
            // 绝对没有末尾 2 字节 CRC！
            Assert.Equal(13, respBytes.Length);
            Assert.Equal(0x12, respBytes[0]);
            Assert.Equal(0x34, respBytes[1]);
            Assert.Equal(0x00, respBytes[4]);
            Assert.Equal(0x07, respBytes[5]); // Length = 1(UnitId) + 1(Func) + 1(ByteCount) + 4(Data) = 7
            Assert.Equal(1, respBytes[6]);
            Assert.Equal(0x03, respBytes[7]);
            Assert.Equal(4, respBytes[8]); // ByteCount
        }

        #endregion

        #region 2. 严格 CRC16 双向校验核验 (误码拦截)

        [Fact]
        public void RtuUnpacker_CorruptedCrc_ShouldBeRejected()
        {
            var client = new ModbusTcpClient(ModbusProtocolFormat.RTU);
            var unpacker = new ModbusUnpacker(client, ModbusProtocolFormat.RTU);

            // 正确的响应帧: 站号 1, 读保持寄存器 03, 字节数 2, 数据 00 0A, CRC16 (38 43)
            // 01 03 02 00 0A 38 43
            byte[] validBytes = new byte[] { 0x01, 0x03, 0x02, 0x00, 0x0A, 0x38, 0x43 };
            var validSeq = new ReadOnlySequence<byte>(validBytes);

            var calcMethod = typeof(ModbusUnpacker).GetMethod("CalculateRtuPacketLengthHandler", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            Assert.NotNull(calcMethod);

            int validLen = (int)calcMethod.Invoke(null, new object[] { validSeq });
            Assert.Equal(7, validLen); // 正确帧成功识别出 7 字节

            // 模拟工业现场 RS-485 强电磁干扰导致的 1 位翻转误码 (把 0x0A 变成 0x0B)
            byte[] corruptedBytes = new byte[] { 0x01, 0x03, 0x02, 0x00, 0x0B, 0x38, 0x43 };
            var corruptedSeq = new ReadOnlySequence<byte>(corruptedBytes);

            int corruptLen = (int)calcMethod.Invoke(null, new object[] { corruptedSeq });
            // CRC 不匹配，解包器严格拦截，返回 -1 触发单字节滑动丢弃，坚决不把脏数据注入业务层！
            Assert.Equal(-1, corruptLen);
        }

        #endregion

        #region 3. 解包器未知字节与杂波滑动自愈测试 (杜绝死锁与通道毒化)

        [Fact]
        public void RtuUnpacker_JunkBytesBeforeValidPacket_ShouldSelfHealViaSlidingWindow()
        {
            var client = new ModbusTcpClient(ModbusProtocolFormat.RTU);
            var unpacker = new ModbusUnpacker(client, ModbusProtocolFormat.RTU);

            List<ModbusResponse> parsedResponses = new List<ModbusResponse>();
            unpacker.OnDataParsed += (s, e) =>
            {
                parsedResponses.Add(e.Data);
            };

            // 在合法报文前面混入 3 个杂波噪声字节: 0xFF, 0xEE, 0x99
            // 随后紧跟合法应答: 01 03 02 00 0A 38 43
            byte[] streamWithNoise = new byte[]
            {
                0xFF, 0xEE, 0x99,                    // 工业噪声杂波
                0x01, 0x03, 0x02, 0x00, 0x0A, 0x38, 0x43 // 真实合法 Modbus 响应
            };

            // 喂入解包器
            var context = new ReceiveContext(streamWithNoise);
            unpacker.Input(context);

            // 验证：解包器自动滑过 3 字节杂波，完美自愈并成功解析出合法响应，绝不死锁！
            Assert.Single(parsedResponses);
            Assert.Equal(1, parsedResponses[0].SlaveId);
            Assert.Equal(FunctionCode.ReadHoldingRegister, parsedResponses[0].FunctionCode);
            Assert.True(parsedResponses[0].IsSuccess);
        }

        #endregion

        #region 4. 工业四种端序 (ABCD, CDAB, BADC, DCBA) 浮点与整型全覆盖测试

        [Fact]
        public void EndianFormat_FloatConversion_ShouldMatchIndustrialStandards()
        {
            // 测试 IEEE 754 浮点数: 123.456f
            // 对应的原始 4 字节十六进制为:
            // Big-Endian (ABCD, 西门子): 42 F6 E9 79
            // Word-Swap  (CDAB, 施耐德): E9 79 42 F6
            // Byte-Swap  (BADC):         F6 42 79 E9
            // Little     (DCBA, x86):   79 E9 F6 42

            float original = 123.456f;

            // 1. ABCD
            byte[] abcdBytes = ModbusByteConverter.GetBytes(original, EndianFormat.ABCD);
            Assert.Equal(new byte[] { 0x42, 0xF6, 0xE9, 0x79 }, abcdBytes);
            float parsedAbcd = ModbusByteConverter.ToSingle(abcdBytes, EndianFormat.ABCD);
            Assert.Equal(original, parsedAbcd, precision: 3);

            // 2. CDAB (施耐德标准)
            byte[] cdabBytes = ModbusByteConverter.GetBytes(original, EndianFormat.CDAB);
            Assert.Equal(new byte[] { 0xE9, 0x79, 0x42, 0xF6 }, cdabBytes);
            float parsedCdab = ModbusByteConverter.ToSingle(cdabBytes, EndianFormat.CDAB);
            Assert.Equal(original, parsedCdab, precision: 3);

            // 3. BADC
            byte[] badcBytes = ModbusByteConverter.GetBytes(original, EndianFormat.BADC);
            Assert.Equal(new byte[] { 0xF6, 0x42, 0x79, 0xE9 }, badcBytes);
            float parsedBadc = ModbusByteConverter.ToSingle(badcBytes, EndianFormat.BADC);
            Assert.Equal(original, parsedBadc, precision: 3);

            // 4. DCBA
            byte[] dcbaBytes = ModbusByteConverter.GetBytes(original, EndianFormat.DCBA);
            Assert.Equal(new byte[] { 0x79, 0xE9, 0xF6, 0x42 }, dcbaBytes);
            float parsedDcba = ModbusByteConverter.ToSingle(dcbaBytes, EndianFormat.DCBA);
            Assert.Equal(original, parsedDcba, precision: 3);
        }

        [Fact]
        public void EndianFormat_Int32Conversion_ShouldMatch()
        {
            int val = 0x12345678;

            byte[] abcd = ModbusByteConverter.GetBytes(val, EndianFormat.ABCD);
            Assert.Equal(new byte[] { 0x12, 0x34, 0x56, 0x78 }, abcd);
            Assert.Equal(val, ModbusByteConverter.ToInt32(abcd, EndianFormat.ABCD));

            byte[] cdab = ModbusByteConverter.GetBytes(val, EndianFormat.CDAB);
            Assert.Equal(new byte[] { 0x56, 0x78, 0x12, 0x34 }, cdab);
            Assert.Equal(val, ModbusByteConverter.ToInt32(cdab, EndianFormat.CDAB));
        }

        #endregion

        #region 5. 广播地址 (SlaveId = 0) 读写语义核验

        [Fact]
        public void Broadcast_ReadOperation_ShouldThrowArgumentException()
        {
            using var client = new ModbusTcpClient(ModbusProtocolFormat.MBAP);
            // 对从站 0 发起读保持寄存器，必须被前置拦截，抛出 ArgumentException
            Assert.Throws<ArgumentException>(() =>
            {
                client.ReadHoldingRegister(0, 0, 10);
            });
        }

        #endregion

        #region 6. 完整 Modbus TCP (MBAP) 客户端与服务端端到端互通测试

        [Fact]
        public async Task ModbusTcp_ClientAndServer_EndToEnd_HighLevelTypes_Success()
        {
            int port = 50288;
            using var server = new ModbusTcpServer(ModbusProtocolFormat.MBAP)
            {
                Port = port,
                SlaveId = 1
            };
            var vDevice = new VirtualModbusDevice(server);
            var holdingReg = new Monitors.HoldingRegisterValueMonitor(0, 20);
            vDevice.Mount(holdingReg);

            await server.StartAsync();

            using var client = new ModbusTcpClient(ModbusProtocolFormat.MBAP)
            {
                Host = "127.0.0.1",
                Port = port
            };
            var connReply = await client.ConnectAsync();
            Assert.True(connReply.IsSuccess);

            // 1. 写入浮点数 3.14159f (施耐德 CDAB 端序) 到地址 2
            var writeFloatReply = client.WriteFloat(1, 2, 3.14159f, EndianFormat.CDAB);
            Assert.True(writeFloatReply.IsCompleted);

            // 2. 读取浮点数核验
            var readFloatReply = client.ReadFloat(1, 2, EndianFormat.CDAB);
            Assert.True(readFloatReply.IsCompleted);
            Assert.NotNull(readFloatReply.GetResponse());
            Assert.Equal(3.14159f, readFloatReply.GetResponse().Value, precision: 4);

            // 3. 写入并读取 Int32
            var writeIntReply = client.WriteInt32(1, 10, 987654321, EndianFormat.ABCD);
            Assert.True(writeIntReply.IsCompleted);

            var readIntReply = client.ReadInt32(1, 10, EndianFormat.ABCD);
            Assert.True(readIntReply.IsCompleted);
            Assert.Equal(987654321, readIntReply.GetResponse().Value);

            // 4. 写入并读取字符串
            var writeStrReply = client.WriteString(1, 14, "BytesIO");
            Assert.True(writeStrReply.IsCompleted);

            var readStrReply = client.ReadString(1, 14, 4);
            Assert.True(readStrReply.IsCompleted);
            Assert.Equal("BytesIO", readStrReply.GetResponse().Value);

            await client.DisconnectAsync();
            await server.StopAsync();
        }

        #endregion

        #region 7. RTU 写多个寄存器 (0x10) 起始地址 0 绝不误判为 MBAP 模式

        [Fact]
        public void Rtu_WriteMultipleRegisters_ZeroAddress_ShouldParseAsRtu_NotMbap()
        {
            // 从站 1，功能码 0x10，写起始地址 0，数量 2 个寄存器
            // 真实标准 RTU 应答报文：01 10 00 00 00 02 41 C8
            byte[] rtuResponseBytes = new byte[] { 0x01, 0x10, 0x00, 0x00, 0x00, 0x02, 0x41, 0xC8 };

            var resp = new WriteRegisterResponse(rtuResponseBytes, ModbusProtocolFormat.RTU);

            // 严禁被误判为 MBAP 报文！
            Assert.Equal(ModbusProtocolFormat.RTU, resp.ProtocolFormat);
            Assert.True(resp.IsSuccess);
            Assert.Equal(1, resp.SlaveId);
            Assert.Equal(FunctionCode.WriteMultipleHoldingRegisters, resp.FunctionCode);
            Assert.Equal(0, resp.WriteAddress);
            Assert.Equal(2, resp.GetUInt16());

            // 验证解包器通过 UnpackContext 反序列化后也准确为 RTU
            var unpacker = new ModbusUnpacker(new ModbusTcpClient(ModbusProtocolFormat.RTU), ModbusProtocolFormat.RTU);
            var seq = new ReadOnlySequence<byte>(rtuResponseBytes);
            var context = new UnpackContext(seq);

            var respMethod = typeof(ModbusUnpacker).GetMethod("ResponseSerializeHandler", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Assert.NotNull(respMethod);
            var parsed = (WriteRegisterResponse)respMethod.Invoke(unpacker, new object[] { context });

            Assert.NotNull(parsed);
            Assert.Equal(ModbusProtocolFormat.RTU, parsed.ProtocolFormat);
            Assert.True(parsed.IsSuccess);
            Assert.Equal(1, parsed.SlaveId);
            Assert.Equal(0, parsed.WriteAddress);
            Assert.Equal(2, parsed.GetUInt16());
        }

        #endregion

        #region 8. RTU 解包器 ByteCount 严格上下限防御过滤 (防杂波长等待假死)

        [Fact]
        public void Rtu_Unpacker_ByteCount_BoundsCheck_ShouldRejectBogusByteCounts()
        {
            var calcMethod = typeof(ModbusUnpacker).GetMethod("CalculateRtuPacketLengthHandler", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            Assert.NotNull(calcMethod);

            // 1. 杂波导致 ByteCount = 255 (超过 Modbus 单包上限 250)
            // 报文：01 03 FF 00 01 (只有 5 字节，若无防御会计算 1+1+1+255+2 = 260 字节，导致返回 0 永久等待长报文假死)
            byte[] noiseHighBytes = new byte[] { 0x01, 0x03, 0xFF, 0x00, 0x01 };
            var seq1 = new ReadOnlySequence<byte>(noiseHighBytes);
            int len1 = (int)calcMethod.Invoke(null, new object[] { seq1 });
            // 必须立即返回 -1，滑动跳过该杂波自愈
            Assert.Equal(-1, len1);

            // 2. 杂波导致 ByteCount = 0 (读寄存器/线圈应答数据字节数不可能为 0)
            byte[] noiseZeroBytes = new byte[] { 0x01, 0x03, 0x00, 0x00, 0x01 };
            var seq2 = new ReadOnlySequence<byte>(noiseZeroBytes);
            int len2 = (int)calcMethod.Invoke(null, new object[] { seq2 });
            Assert.Equal(-1, len2);
        }

        #endregion

        #region 9. VirtualModbusDevice 写入触发 reg.Notify() 与 RegisterValueMonitor.ValueChanged

        [Fact]
        public void VirtualModbusDevice_WriteHoldingRegister_ShouldTriggerRegisterNotify()
        {
            using var server = new ModbusTcpServer(ModbusProtocolFormat.RTU) { Port = 50299, SlaveId = 1 };
            var vDevice = new VirtualModbusDevice(server);
            var holdingReg = new Monitors.HoldingRegisterValueMonitor(0, 10);
            vDevice.Mount(holdingReg);

            bool eventFired = false;
            holdingReg.ValueChanged += (sender, e) =>
            {
                eventFired = true;
            };

            // 模拟向服务端触发写入单寄存器请求
            var writeSingleReq = new WriteSingleHoldingRegisterRequest()
            {
                SlaveId = 1,
                WriteAddress = 3,
                Data = new byte[] { 0x12, 0x34 }
            };

            var handleMethod = typeof(ModbusServer).GetMethod("HandleRequest", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Assert.NotNull(handleMethod);

            using var fakeClient = new TcpClient();
            handleMethod.Invoke(server, new object[] { writeSingleReq, fakeClient });

            // 必须成功触发 ValueChanged 事件
            Assert.True(eventFired);
            Assert.Equal(0x1234, holdingReg.Source[3]);
        }

        #endregion

        #region 10. ModbusClient 默认禁用流水线开启通道排队与剩余超时计算

        [Fact]
        public void ModbusClient_DefaultPipelining_ShouldBeFalse_ForIndustrialSafety()
        {
            using var tcpClient = new ModbusTcpClient();
            // 工业安全防范默认值必须为 false
            Assert.False(tcpClient.AllowPipelining);
        }

        #endregion
    }
}
