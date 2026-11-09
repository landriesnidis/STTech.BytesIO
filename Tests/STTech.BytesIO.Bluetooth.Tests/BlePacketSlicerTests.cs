using STTech.BytesIO.Bluetooth.Ble;
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Xunit;

namespace STTech.BytesIO.Bluetooth.Tests
{
    public class BlePacketSlicerTests
    {
        [Fact]
        public void Slice_NullOrEmptyData_ShouldYieldNothing()
        {
            var res1 = BlePacketSlicer.Slice(null!, 20).ToList();
            Assert.Empty(res1);

            var res2 = BlePacketSlicer.Slice(Array.Empty<byte>(), 20).ToList();
            Assert.Empty(res2);
        }

        [Fact]
        public void Slice_PayloadSmallerThanMax_ShouldYieldSingleChunk()
        {
            byte[] data = new byte[] { 0x01, 0x02, 0x03, 0x04 };
            var chunks = BlePacketSlicer.Slice(data, 20).ToList();

            Assert.Single(chunks);
            Assert.Equal(4, chunks[0].Count);
            Assert.Equal(data, chunks[0].ToArray());
        }

        [Fact]
        public void Slice_PayloadExactMax_ShouldYieldSingleChunk()
        {
            byte[] data = new byte[20];
            for (int i = 0; i < 20; i++) data[i] = (byte)i;

            var chunks = BlePacketSlicer.Slice(data, 20).ToList();

            Assert.Single(chunks);
            Assert.Equal(20, chunks[0].Count);
            Assert.Equal(data, chunks[0].ToArray());
        }

        [Fact]
        public void Slice_PayloadSlightlyExceedingMax_ShouldYieldTwoChunks()
        {
            byte[] data = new byte[21];
            for (int i = 0; i < 21; i++) data[i] = (byte)i;

            var chunks = BlePacketSlicer.Slice(data, 20).ToList();

            Assert.Equal(2, chunks.Count);
            Assert.Equal(20, chunks[0].Count);
            Assert.Equal(1, chunks[1].Count);
            Assert.Equal(20, chunks[1].Array![chunks[1].Offset]);
        }

        [Theory]
        [InlineData(100, 20, 5)]
        [InlineData(105, 20, 6)]
        [InlineData(1024, 244, 5)]
        [InlineData(4096, 509, 9)]
        public void Slice_LargeData_ShouldChunkAccuratelyAndReassembleIdentically(int dataLength, int maxChunkSize, int expectedChunkCount)
        {
            byte[] rawData = new byte[dataLength];
            new Random(42).NextBytes(rawData);

            var chunks = BlePacketSlicer.Slice(rawData, maxChunkSize).ToList();

            Assert.Equal(expectedChunkCount, chunks.Count);

            // 拼装还原切片并验证内容完全一致
            using var ms = new MemoryStream();
            foreach (var chunk in chunks)
            {
                ms.Write(chunk.Array!, chunk.Offset, chunk.Count);
            }

            byte[] reassembled = ms.ToArray();
            Assert.Equal(rawData.Length, reassembled.Length);
            Assert.Equal(rawData, reassembled);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(-100)]
        public void Slice_InvalidChunkSize_ShouldFallbackToDefault20Bytes(int invalidChunkSize)
        {
            byte[] data = new byte[45];
            var chunks = BlePacketSlicer.Slice(data, invalidChunkSize).ToList();

            // 45 字节按 20 字节切分：20 + 20 + 5 = 3 包
            Assert.Equal(3, chunks.Count);
            Assert.Equal(20, chunks[0].Count);
            Assert.Equal(20, chunks[1].Count);
            Assert.Equal(5, chunks[2].Count);
        }
    }
}
