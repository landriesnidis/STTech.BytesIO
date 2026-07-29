using System;
using System.Buffers;
using System.Text;
using Xunit;
using STTech.BytesIO.Core;
using STTech.BytesIO.Core.Component;

namespace STTech.BytesIO.Tcp.Tests
{
    public class ReadOnlySequenceExtensionsTests
    {
        private static ReadOnlySequence<byte> CreateSingleSegment(byte[] data)
        {
            return new ReadOnlySequence<byte>(data);
        }

        private static ReadOnlySequence<byte> CreateMultiSegment(params byte[][] segments)
        {
            if (segments == null || segments.Length == 0)
            {
                return ReadOnlySequence<byte>.Empty;
            }

            BufferSegment first = null!;
            BufferSegment last = null!;

            foreach (var segmentData in segments)
            {
                var segment = new BufferSegment(segmentData);
                if (first == null)
                {
                    first = segment;
                    last = segment;
                }
                else
                {
                    last.SetNext(segment);
                    last = segment;
                }
            }

            return new ReadOnlySequence<byte>(first, 0, last, last.Memory.Length);
        }

        #region GetByte Tests

        [Fact]
        public void GetByte_SingleSegment_ReturnsCorrectByte()
        {
            var data = new byte[] { 0x01, 0x02, 0x03 };
            var sequence = CreateSingleSegment(data);

            Assert.Equal(0x01, sequence.GetByte(0));
            Assert.Equal(0x02, sequence.GetByte(1));
            Assert.Equal(0x03, sequence.GetByte(2));
        }

        [Fact]
        public void GetByte_MultiSegment_ReturnsCorrectByte()
        {
            var sequence = CreateMultiSegment(new byte[] { 0x01 }, new byte[] { 0x02 }, new byte[] { 0x03 });

            Assert.Equal(0x01, sequence.GetByte(0));
            Assert.Equal(0x02, sequence.GetByte(1));
            Assert.Equal(0x03, sequence.GetByte(2));
        }

        [Fact]
        public void GetByte_OutOfRange_ThrowsException()
        {
            var sequence = CreateSingleSegment(new byte[] { 0x01, 0x02 });
            Assert.ThrowsAny<Exception>(() => sequence.GetByte(-1));
            Assert.ThrowsAny<Exception>(() => sequence.GetByte(2));
        }

        #endregion

        #region Int16 / UInt16 Tests

        [Fact]
        public void ReadInt16_SingleSegment_ReturnsCorrectValue()
        {
            var data = new byte[] { 0x12, 0x34 };
            var sequence = CreateSingleSegment(data);

            Assert.Equal(0x1234, sequence.ReadInt16BigEndian(0));
            Assert.Equal(0x3412, sequence.ReadInt16LittleEndian(0));
        }

        [Fact]
        public void ReadInt16_MultiSegment_ReturnsCorrectValue()
        {
            var sequence = CreateMultiSegment(new byte[] { 0x12 }, new byte[] { 0x34 });

            Assert.Equal(0x1234, sequence.ReadInt16BigEndian(0));
            Assert.Equal(0x3412, sequence.ReadInt16LittleEndian(0));
        }

        [Fact]
        public void ReadUInt16_SingleSegment_ReturnsCorrectValue()
        {
            var data = new byte[] { 0xAB, 0xCD };
            var sequence = CreateSingleSegment(data);

            Assert.Equal(0xABCD, sequence.ReadUInt16BigEndian(0));
            Assert.Equal(0xCDAB, sequence.ReadUInt16LittleEndian(0));
        }

        [Fact]
        public void ReadUInt16_MultiSegment_ReturnsCorrectValue()
        {
            var sequence = CreateMultiSegment(new byte[] { 0xAB }, new byte[] { 0xCD });

            Assert.Equal(0xABCD, sequence.ReadUInt16BigEndian(0));
            Assert.Equal(0xCDAB, sequence.ReadUInt16LittleEndian(0));
        }

        #endregion

        #region Int32 / UInt32 Tests

        [Fact]
        public void ReadInt32_SingleSegment_ReturnsCorrectValue()
        {
            var data = new byte[] { 0x12, 0x34, 0x56, 0x78 };
            var sequence = CreateSingleSegment(data);

            Assert.Equal(0x12345678, sequence.ReadInt32BigEndian(0));
            Assert.Equal(0x78563412, sequence.ReadInt32LittleEndian(0));
        }

        [Fact]
        public void ReadInt32_MultiSegment_ReturnsCorrectValue()
        {
            var sequence = CreateMultiSegment(new byte[] { 0x12, 0x34 }, new byte[] { 0x56, 0x78 });

            Assert.Equal(0x12345678, sequence.ReadInt32BigEndian(0));
            Assert.Equal(0x78563412, sequence.ReadInt32LittleEndian(0));
        }

        [Fact]
        public void ReadUInt32_SingleSegment_ReturnsCorrectValue()
        {
            var data = new byte[] { 0xAB, 0xCD, 0xEF, 0x01 };
            var sequence = CreateSingleSegment(data);

            Assert.Equal(0xABCDEF01u, sequence.ReadUInt32BigEndian(0));
            Assert.Equal(0x01EFCDABu, sequence.ReadUInt32LittleEndian(0));
        }

        [Fact]
        public void ReadUInt32_MultiSegment_ReturnsCorrectValue()
        {
            var sequence = CreateMultiSegment(new byte[] { 0xAB }, new byte[] { 0xCD, 0xEF }, new byte[] { 0x01 });

            Assert.Equal(0xABCDEF01u, sequence.ReadUInt32BigEndian(0));
            Assert.Equal(0x01EFCDABu, sequence.ReadUInt32LittleEndian(0));
        }

        #endregion

        #region Int64 / UInt64 Tests

        [Fact]
        public void ReadInt64_SingleSegment_ReturnsCorrectValue()
        {
            var data = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08 };
            var sequence = CreateSingleSegment(data);

            Assert.Equal(0x0102030405060708, sequence.ReadInt64BigEndian(0));
            Assert.Equal(0x0807060504030201, sequence.ReadInt64LittleEndian(0));
        }

        [Fact]
        public void ReadInt64_MultiSegment_ReturnsCorrectValue()
        {
            var sequence = CreateMultiSegment(new byte[] { 0x01, 0x02 }, new byte[] { 0x03, 0x04, 0x05 }, new byte[] { 0x06, 0x07, 0x08 });

            Assert.Equal(0x0102030405060708, sequence.ReadInt64BigEndian(0));
            Assert.Equal(0x0807060504030201, sequence.ReadInt64LittleEndian(0));
        }

        [Fact]
        public void ReadUInt64_SingleSegment_ReturnsCorrectValue()
        {
            var data = new byte[] { 0x80, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08 };
            var sequence = CreateSingleSegment(data);

            Assert.Equal(0x8002030405060708ul, sequence.ReadUInt64BigEndian(0));
            Assert.Equal(0x0807060504030280ul, sequence.ReadUInt64LittleEndian(0));
        }

        [Fact]
        public void ReadUInt64_MultiSegment_ReturnsCorrectValue()
        {
            var sequence = CreateMultiSegment(new byte[] { 0x80 }, new byte[] { 0x02, 0x03, 0x04, 0x05, 0x06, 0x07 }, new byte[] { 0x08 });

            Assert.Equal(0x8002030405060708ul, sequence.ReadUInt64BigEndian(0));
            Assert.Equal(0x0807060504030280ul, sequence.ReadUInt64LittleEndian(0));
        }

        #endregion

        #region IndexOf Tests

        [Fact]
        public void IndexOf_SingleSegment_ReturnsCorrectIndex()
        {
            var data = new byte[] { 0x01, 0x0A, 0x0B, 0x02, 0x0A, 0x0B };
            var sequence = CreateSingleSegment(data);

            Assert.Equal(1, sequence.IndexOf(new byte[] { 0x0A, 0x0B }));
            Assert.Equal(0, sequence.IndexOf(new byte[] { 0x01 }));
            Assert.Null(sequence.IndexOf(new byte[] { 0x0A, 0x0C }));
            Assert.Equal(0, sequence.IndexOf(ReadOnlySpan<byte>.Empty));
        }

        [Fact]
        public void IndexOf_MultiSegment_ReturnsCorrectIndex()
        {
            // Test delimiter spanning segments, within segments, and not found
            var sequence = CreateMultiSegment(
                new byte[] { 0x01, 0x0A },
                new byte[] { 0x0B, 0x02 },
                new byte[] { 0x0A, 0x0B }
            );

            // Spans first and second segments
            Assert.Equal(1, sequence.IndexOf(new byte[] { 0x0A, 0x0B }));
            // Starts in the third segment (slicing sequence past the first match)
            Assert.Equal(2, sequence.Slice(2).IndexOf(new byte[] { 0x0A, 0x0B }).GetValueOrDefault());
            // Single byte in second segment
            Assert.Equal(3, sequence.IndexOf(new byte[] { 0x02 }));
            // Not found
            Assert.Null(sequence.IndexOf(new byte[] { 0x0A, 0x0C }));
            // Empty delimiter
            Assert.Equal(0, sequence.IndexOf(ReadOnlySpan<byte>.Empty));
        }

        #endregion

        #region GetString Tests

        [Fact]
        public void GetString_SingleSegment_ReturnsCorrectString()
        {
            var data = Encoding.UTF8.GetBytes("Hello, World!");
            var sequence = CreateSingleSegment(data);

            Assert.Equal("Hello", sequence.GetString(0, 5, Encoding.UTF8));
            Assert.Equal("World", sequence.GetString(7, 5, Encoding.UTF8));
        }

        [Fact]
        public void GetString_MultiSegment_ReturnsCorrectString()
        {
            var sequence = CreateMultiSegment(
                Encoding.UTF8.GetBytes("Hello"),
                Encoding.UTF8.GetBytes(", "),
                Encoding.UTF8.GetBytes("World!")
            );

            Assert.Equal("Hello", sequence.GetString(0, 5, Encoding.UTF8));
            Assert.Equal("World", sequence.GetString(7, 5, Encoding.UTF8));
            Assert.Equal("Hello, World!", sequence.GetString(0, 13, Encoding.UTF8));
        }

        #endregion
    }
}
