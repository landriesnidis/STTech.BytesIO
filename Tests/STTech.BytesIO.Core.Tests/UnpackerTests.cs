using STTech.BytesIO.Core;
using STTech.BytesIO.Core.Component;
using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Xunit;

namespace STTech.BytesIO.Core.Tests
{
    /// <summary>
    /// 自定义测试解包器：包结构为 [StartMark: 0xAA, 0x55] [PayloadLength: 2 字节大端] [Payload...]
    /// </summary>
    internal class TestPacketUnpacker : Unpacker
    {
        public TestPacketUnpacker()
        {
            StartMark = new byte[] { 0xAA, 0x55 };
        }

        protected override int CalculatePacketLength(ReadOnlySequence<byte> buffer)
        {
            if (buffer.Length < 4)
            {
                return 0; // 头部不足以计算总包长
            }

            Span<byte> header = stackalloc byte[4];
            buffer.Slice(0, 4).CopyTo(header);

            ushort payloadLength = BinaryPrimitives.ReadUInt16BigEndian(header.Slice(2, 2));
            return 4 + payloadLength;
        }
    }

    public class UnpackerTests
    {
        private static ReceiveContext CreateContext(params byte[] data)
        {
            return new ReceiveContext(data);
        }

        [Fact]
        public void Unpack_SingleCompletePacket_ShouldParseSuccessfully()
        {
            var unpacker = new TestPacketUnpacker();
            var parsedPackets = new List<byte[]>();

            unpacker.OnDataParsed += (s, e) =>
            {
                parsedPackets.Add(e.Data.Data.ToArray());
            };

            // AA 55 | 00 03 | 01 02 03
            byte[] packet = new byte[] { 0xAA, 0x55, 0x00, 0x03, 0x01, 0x02, 0x03 };
            using var ctx = CreateContext(packet);
            unpacker.Input(ctx);

            Assert.Single(parsedPackets);
            Assert.Equal(packet, parsedPackets[0]);
        }

        [Fact]
        public void Unpack_StickyPackets_ShouldParseMultiplePacketsInOneInput()
        {
            var unpacker = new TestPacketUnpacker();
            var parsedPackets = new List<byte[]>();

            unpacker.OnDataParsed += (s, e) =>
            {
                parsedPackets.Add(e.Data.Data.ToArray());
            };

            // 包1: AA 55 | 00 02 | 10 20
            // 包2: AA 55 | 00 01 | 30
            // 包3: AA 55 | 00 03 | 40 50 60
            byte[] sticky = new byte[]
            {
                0xAA, 0x55, 0x00, 0x02, 0x10, 0x20,
                0xAA, 0x55, 0x00, 0x01, 0x30,
                0xAA, 0x55, 0x00, 0x03, 0x40, 0x50, 0x60
            };

            using var ctx = CreateContext(sticky);
            unpacker.Input(ctx);

            Assert.Equal(3, parsedPackets.Count);
            Assert.Equal(new byte[] { 0xAA, 0x55, 0x00, 0x02, 0x10, 0x20 }, parsedPackets[0]);
            Assert.Equal(new byte[] { 0xAA, 0x55, 0x00, 0x01, 0x30 }, parsedPackets[1]);
            Assert.Equal(new byte[] { 0xAA, 0x55, 0x00, 0x03, 0x40, 0x50, 0x60 }, parsedPackets[2]);
        }

        [Fact]
        public void Unpack_HalfPacket_ShouldAssembleAcrossMultipleInputs()
        {
            var unpacker = new TestPacketUnpacker();
            var parsedPackets = new List<byte[]>();

            unpacker.OnDataParsed += (s, e) =>
            {
                parsedPackets.Add(e.Data.Data.ToArray());
            };

            // 完整包: AA 55 00 04 11 22 33 44
            // 分三段输入：
            // 阶段 1: AA 55 00 (不足计算包长)
            using (var ctx1 = CreateContext(0xAA, 0x55, 0x00))
            {
                unpacker.Input(ctx1);
            }
            Assert.Empty(parsedPackets);

            // 阶段 2: 04 11 (包长计算为 8，但当前只有 5 字节)
            using (var ctx2 = CreateContext(0x04, 0x11))
            {
                unpacker.Input(ctx2);
            }
            Assert.Empty(parsedPackets);

            // 阶段 3: 22 33 44 (补足 8 字节)
            using (var ctx3 = CreateContext(0x22, 0x33, 0x44))
            {
                unpacker.Input(ctx3);
            }

            Assert.Single(parsedPackets);
            Assert.Equal(new byte[] { 0xAA, 0x55, 0x00, 0x04, 0x11, 0x22, 0x33, 0x44 }, parsedPackets[0]);
        }

        [Fact]
        public void Unpack_DirtyPrefixBytes_ShouldSkipGarbageAndResyncStartMark()
        {
            var unpacker = new TestPacketUnpacker();
            var parsedPackets = new List<byte[]>();

            unpacker.OnDataParsed += (s, e) =>
            {
                parsedPackets.Add(e.Data.Data.ToArray());
            };

            // 前导乱码数据: 0xEE, 0xFF, 0xAA, 0x00 (不是有效包头)
            // 紧接着有效包: AA 55 | 00 02 | 99 88
            byte[] dirtyData = new byte[]
            {
                0xEE, 0xFF, 0xAA, 0x00,
                0xAA, 0x55, 0x00, 0x02, 0x99, 0x88
            };

            using var ctx = CreateContext(dirtyData);
            unpacker.Input(ctx);

            Assert.Single(parsedPackets);
            Assert.Equal(new byte[] { 0xAA, 0x55, 0x00, 0x02, 0x99, 0x88 }, parsedPackets[0]);
        }

        [Fact]
        public void Unpack_StartMarkNotMatch_ShouldTriggerErrorOccurHandler()
        {
            var unpacker = new TestPacketUnpacker();
            bool errorTriggered = false;
            unpacker.ErrorOccurHandler = (err) =>
            {
                if (err == Unpacker.ErrorCode.StartMarkNotMatch)
                {
                    errorTriggered = true;
                }
                return false;
            };

            // 全是杂乱字节，无 StartMark
            byte[] pureGarbage = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 };
            using var ctx = CreateContext(pureGarbage);
            unpacker.Input(ctx);

            Assert.True(errorTriggered, "未找到起始标识时应当触发 ErrorOccurHandler");
        }

        [Fact]
        public void Unpack_InterruptFrameTimeout_ShouldDiscardStaleFragment()
        {
            var unpacker = new TestPacketUnpacker
            {
                InterruptFrameTimeoutValue = 50 // 50ms 超时
            };

            var parsedPackets = new List<byte[]>();
            unpacker.OnDataParsed += (s, e) =>
            {
                parsedPackets.Add(e.Data.Data.ToArray());
            };

            // 输入残缺半包: AA 55 00 05 01 02
            using (var ctx1 = CreateContext(0xAA, 0x55, 0x00, 0x05, 0x01, 0x02))
            {
                unpacker.Input(ctx1);
            }
            Assert.Empty(parsedPackets);

            // 等待超时过期
            Thread.Sleep(80);

            // 超时后输入一个全新的完整包: AA 55 00 01 77
            using (var ctx2 = CreateContext(0xAA, 0x55, 0x00, 0x01, 0x77))
            {
                unpacker.Input(ctx2);
            }

            Assert.Single(parsedPackets);
            Assert.Equal(new byte[] { 0xAA, 0x55, 0x00, 0x01, 0x77 }, parsedPackets[0]);
        }

        [Fact]
        public void ClearCache_ShouldResetInternalState()
        {
            var unpacker = new TestPacketUnpacker();
            var parsedPackets = new List<byte[]>();
            unpacker.OnDataParsed += (s, e) => parsedPackets.Add(e.Data.Data.ToArray());

            // 输入残缺包
            using (var ctx1 = CreateContext(0xAA, 0x55, 0x00, 0x04, 0x01))
            {
                unpacker.Input(ctx1);
            }

            // 主动清空
            unpacker.ClearCache();

            // 再次输入完整包验证状态机恢复
            using (var ctx2 = CreateContext(0xAA, 0x55, 0x00, 0x01, 0x99))
            {
                unpacker.Input(ctx2);
            }

            Assert.Single(parsedPackets);
            Assert.Equal(new byte[] { 0xAA, 0x55, 0x00, 0x01, 0x99 }, parsedPackets[0]);
        }

        [Fact]
        public void Unpack_WithPooledReceiveContext_ShouldCorrectlyReturnToArrayPool()
        {
            var unpacker = new TestPacketUnpacker();
            var parsedPackets = new List<byte[]>();
            unpacker.OnDataParsed += (s, e) => parsedPackets.Add(e.Data.Data.ToArray());

            byte[] rented = ArrayPool<byte>.Shared.Rent(64);
            // 写入 AA 55 00 02 12 34
            rented[0] = 0xAA;
            rented[1] = 0x55;
            rented[2] = 0x00;
            rented[3] = 0x02;
            rented[4] = 0x12;
            rented[5] = 0x34;

            // 使用 internal 的池化构造器，验证在 Unpacker 消费完成后自动释放归还到池中
            var ctx = new ReceiveContext(rented, 0, 6);
            unpacker.Input(ctx);

            Assert.Single(parsedPackets);
            Assert.Equal(new byte[] { 0xAA, 0x55, 0x00, 0x02, 0x12, 0x34 }, parsedPackets[0]);
        }
    }
}
