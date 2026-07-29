using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Text;

namespace STTech.BytesIO.Core
{
    /// <summary>
    /// 对 ReadOnlySequence 类型的扩展方法集。
    /// 主要用于简化基于 System.IO.Pipelines 的拆包器（Unpacker）中的数据解析工作。
    /// 包含对常见协议头部数据的零拷贝、无分配读取。
    /// </summary>
    public static class ReadOnlySequenceExtensions
    {
        /// <summary>
        /// 从指定偏移量读取单个字节
        /// </summary>
        public static byte GetByte(this in ReadOnlySequence<byte> buffer, long offset)
        {
            if (buffer.IsSingleSegment)
            {
                return buffer.First.Span[(int)offset];
            }
            return buffer.Slice(offset, 1).First.Span[0];
        }

        /// <summary>
        /// 从指定偏移量读取 16 位有符号整数 (大端序)
        /// </summary>
        public static short ReadInt16BigEndian(this in ReadOnlySequence<byte> buffer, long offset)
        {
            if (buffer.IsSingleSegment)
            {
                return BinaryPrimitives.ReadInt16BigEndian(buffer.First.Span.Slice((int)offset));
            }
            Span<byte> temp = stackalloc byte[2];
            buffer.Slice(offset, 2).CopyTo(temp);
            return BinaryPrimitives.ReadInt16BigEndian(temp);
        }

        /// <summary>
        /// 从指定偏移量读取 16 位有符号整数 (小端序)
        /// </summary>
        public static short ReadInt16LittleEndian(this in ReadOnlySequence<byte> buffer, long offset)
        {
            if (buffer.IsSingleSegment)
            {
                return BinaryPrimitives.ReadInt16LittleEndian(buffer.First.Span.Slice((int)offset));
            }
            Span<byte> temp = stackalloc byte[2];
            buffer.Slice(offset, 2).CopyTo(temp);
            return BinaryPrimitives.ReadInt16LittleEndian(temp);
        }

        /// <summary>
        /// 从指定偏移量读取 16 位无符号整数 (大端序)
        /// </summary>
        public static ushort ReadUInt16BigEndian(this in ReadOnlySequence<byte> buffer, long offset)
        {
            if (buffer.IsSingleSegment)
            {
                return BinaryPrimitives.ReadUInt16BigEndian(buffer.First.Span.Slice((int)offset));
            }
            Span<byte> temp = stackalloc byte[2];
            buffer.Slice(offset, 2).CopyTo(temp);
            return BinaryPrimitives.ReadUInt16BigEndian(temp);
        }

        /// <summary>
        /// 从指定偏移量读取 16 位无符号整数 (小端序)
        /// </summary>
        public static ushort ReadUInt16LittleEndian(this in ReadOnlySequence<byte> buffer, long offset)
        {
            if (buffer.IsSingleSegment)
            {
                return BinaryPrimitives.ReadUInt16LittleEndian(buffer.First.Span.Slice((int)offset));
            }
            Span<byte> temp = stackalloc byte[2];
            buffer.Slice(offset, 2).CopyTo(temp);
            return BinaryPrimitives.ReadUInt16LittleEndian(temp);
        }

        /// <summary>
        /// 从指定偏移量读取 32 位有符号整数 (大端序)
        /// </summary>
        public static int ReadInt32BigEndian(this in ReadOnlySequence<byte> buffer, long offset)
        {
            if (buffer.IsSingleSegment)
            {
                return BinaryPrimitives.ReadInt32BigEndian(buffer.First.Span.Slice((int)offset));
            }
            Span<byte> temp = stackalloc byte[4];
            buffer.Slice(offset, 4).CopyTo(temp);
            return BinaryPrimitives.ReadInt32BigEndian(temp);
        }

        /// <summary>
        /// 从指定偏移量读取 32 位有符号整数 (小端序)
        /// </summary>
        public static int ReadInt32LittleEndian(this in ReadOnlySequence<byte> buffer, long offset)
        {
            if (buffer.IsSingleSegment)
            {
                return BinaryPrimitives.ReadInt32LittleEndian(buffer.First.Span.Slice((int)offset));
            }
            Span<byte> temp = stackalloc byte[4];
            buffer.Slice(offset, 4).CopyTo(temp);
            return BinaryPrimitives.ReadInt32LittleEndian(temp);
        }

        /// <summary>
        /// 从指定偏移量读取 32 位无符号整数 (大端序)
        /// </summary>
        public static uint ReadUInt32BigEndian(this in ReadOnlySequence<byte> buffer, long offset)
        {
            if (buffer.IsSingleSegment)
            {
                return BinaryPrimitives.ReadUInt32BigEndian(buffer.First.Span.Slice((int)offset));
            }
            Span<byte> temp = stackalloc byte[4];
            buffer.Slice(offset, 4).CopyTo(temp);
            return BinaryPrimitives.ReadUInt32BigEndian(temp);
        }

        /// <summary>
        /// 从指定偏移量读取 32 位无符号整数 (小端序)
        /// </summary>
        public static uint ReadUInt32LittleEndian(this in ReadOnlySequence<byte> buffer, long offset)
        {
            if (buffer.IsSingleSegment)
            {
                return BinaryPrimitives.ReadUInt32LittleEndian(buffer.First.Span.Slice((int)offset));
            }
            Span<byte> temp = stackalloc byte[4];
            buffer.Slice(offset, 4).CopyTo(temp);
            return BinaryPrimitives.ReadUInt32LittleEndian(temp);
        }

        /// <summary>
        /// 从指定偏移量读取 64 位有符号整数 (大端序)
        /// </summary>
        public static long ReadInt64BigEndian(this in ReadOnlySequence<byte> buffer, long offset)
        {
            if (buffer.IsSingleSegment)
            {
                return BinaryPrimitives.ReadInt64BigEndian(buffer.First.Span.Slice((int)offset));
            }
            Span<byte> temp = stackalloc byte[8];
            buffer.Slice(offset, 8).CopyTo(temp);
            return BinaryPrimitives.ReadInt64BigEndian(temp);
        }

        /// <summary>
        /// 从指定偏移量读取 64 位有符号整数 (小端序)
        /// </summary>
        public static long ReadInt64LittleEndian(this in ReadOnlySequence<byte> buffer, long offset)
        {
            if (buffer.IsSingleSegment)
            {
                return BinaryPrimitives.ReadInt64LittleEndian(buffer.First.Span.Slice((int)offset));
            }
            Span<byte> temp = stackalloc byte[8];
            buffer.Slice(offset, 8).CopyTo(temp);
            return BinaryPrimitives.ReadInt64LittleEndian(temp);
        }
        
        /// <summary>
        /// 从指定偏移量读取 64 位无符号整数 (大端序)
        /// </summary>
        public static ulong ReadUInt64BigEndian(this in ReadOnlySequence<byte> buffer, long offset)
        {
            if (buffer.IsSingleSegment)
            {
                return BinaryPrimitives.ReadUInt64BigEndian(buffer.First.Span.Slice((int)offset));
            }
            Span<byte> temp = stackalloc byte[8];
            buffer.Slice(offset, 8).CopyTo(temp);
            return BinaryPrimitives.ReadUInt64BigEndian(temp);
        }

        /// <summary>
        /// 从指定偏移量读取 64 位无符号整数 (小端序)
        /// </summary>
        public static ulong ReadUInt64LittleEndian(this in ReadOnlySequence<byte> buffer, long offset)
        {
            if (buffer.IsSingleSegment)
            {
                return BinaryPrimitives.ReadUInt64LittleEndian(buffer.First.Span.Slice((int)offset));
            }
            Span<byte> temp = stackalloc byte[8];
            buffer.Slice(offset, 8).CopyTo(temp);
            return BinaryPrimitives.ReadUInt64LittleEndian(temp);
        }

        /// <summary>
        /// 在数据中搜索特定的结束符(如 \r\n 等)，并返回其相对于起始位置的索引。
        /// 若未找到则返回 null。
        /// </summary>
        public static long? IndexOf(this in ReadOnlySequence<byte> buffer, ReadOnlySpan<byte> delimiter)
        {
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP || NET5_0_OR_GREATER
            var reader = new SequenceReader<byte>(buffer);
            if (reader.TryReadTo(out ReadOnlySequence<byte> _, delimiter))
            {
                return reader.Consumed - delimiter.Length;
            }
            return null;
#else
            if (buffer.IsSingleSegment)
            {
                int index = buffer.First.Span.IndexOf(delimiter);
                return index != -1 ? (long?)index : null;
            }

            // Fallback for multi-segment in netstandard2.0
            if (delimiter.Length == 0) return 0;
            byte firstByte = delimiter[0];
            long currentOffset = 0;
            foreach (var memory in buffer)
            {
                var span = memory.Span;
                int offsetInSpan = 0;
                while (offsetInSpan < span.Length)
                {
                    int idx = span.Slice(offsetInSpan).IndexOf(firstByte);
                    if (idx == -1) break;
                    
                    long matchStart = currentOffset + offsetInSpan + idx;
                    if (buffer.Length - matchStart >= delimiter.Length)
                    {
                        bool match = true;
                        for (int i = 1; i < delimiter.Length; i++)
                        {
                            if (buffer.GetByte(matchStart + i) != delimiter[i])
                            {
                                match = false;
                                break;
                            }
                        }
                        if (match) return matchStart;
                    }
                    offsetInSpan += idx + 1;
                }
                currentOffset += span.Length;
            }
            return null;
#endif
        }

        /// <summary>
        /// 从指定偏移量起，读取指定长度的字节并使用指定的编码转换为字符串
        /// </summary>
        public static string GetString(this in ReadOnlySequence<byte> buffer, long offset, int length, Encoding encoding)
        {
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP || NET5_0_OR_GREATER
            if (buffer.IsSingleSegment)
            {
                return encoding.GetString(buffer.First.Span.Slice((int)offset, length));
            }
#endif
            // 跨段，或者在 netstandard2.0 等不支持 ReadOnlySpan 的环境中，使用 ArrayPool 避免内存分配压力
            byte[] rentArray = ArrayPool<byte>.Shared.Rent(length);
            try
            {
                buffer.Slice(offset, length).CopyTo(rentArray);
                return encoding.GetString(rentArray, 0, length);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(rentArray);
            }
        }
    }
}
