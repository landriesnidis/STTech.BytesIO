using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace STTech.BytesIO.Core
{
    /// <summary>
    /// 接收数据上下文
    /// 底层支持两种内存后端：
    /// 1. 托管数组 (ArrayPool 池化内存或用户数组) —— 释放时归还 ArrayPool；
    /// 2. 原生内存 (共享内存映射视图等) —— 零拷贝，由调用方提供的释放回调负责解锁。
    /// 支持 GC 兜底：忘记调用 Dispose 不会导致内存泄漏，但主动 Dispose 可以更快地归还资源。
    /// 
    /// 实现了 <see cref="IReadOnlyList{T}"/> 接口，可以像字节数组一样通过索引访问数据，
    /// 并且支持 LINQ 操作（如 Select、Where、Skip、Take、SequenceEqual 等）。
    /// 所有操作均限定在有效数据范围内，用户无需关心底层真实内存的大小。
    /// </summary>
    public class ReceiveContext : IDisposable, IReadOnlyList<byte>
    {
        /// <summary>
        /// 底层内存后端：byte[] (托管数组) 或 NativeMemoryManager (原生内存)
        /// </summary>
        private object _backing;

        /// <summary>
        /// 有效数据的起始偏移(仅托管数组后端使用)
        /// </summary>
        private readonly int _offset;

        /// <summary>
        /// 有效数据长度
        /// </summary>
        private readonly int _length;

        /// <summary>
        /// 资源释放回调(归还ArrayPool / 发送ACK解锁共享槽位等)
        /// </summary>
        private Action _release;

        private int _referenceCount = 1;

        public ReadOnlyMemory<byte> Memory
        {
            get
            {
                var backing = _backing;
                if (backing is byte[] arr)
                {
                    return new ReadOnlyMemory<byte>(arr, _offset, _length);
                }
                if (backing is NativeMemoryManager manager)
                {
                    return manager.Memory;
                }
                throw new ObjectDisposedException(nameof(ReceiveContext));
            }
        }

        /// <summary>
        /// 有效数据长度
        /// </summary>
        public int Length => _length;

        public DateTime ReceivedTime { get; }
        public bool IsDisposed => _backing == null;
        public IDictionary<string, object> Properties { get; } = new Dictionary<string, object>();

        /// <summary>
        /// 构造接收上下文 (池化数组后端)
        /// 释放时自动将数组归还 ArrayPool
        /// </summary>
        internal ReceiveContext(byte[] rentedArray, int offset, int length)
        {
            if (rentedArray == null) throw new ArgumentNullException(nameof(rentedArray));
            _backing = rentedArray;
            _offset = offset;
            _length = length;
            _release = () => ArrayPool<byte>.Shared.Return(rentedArray);
            ReceivedTime = DateTime.Now;
        }

        /// <summary>
        /// 构造接收上下文 (用户数组后端)
        /// 释放时不归还任何资源
        /// </summary>
        public ReceiveContext(byte[] data)
        {
            _backing = data ?? throw new ArgumentNullException(nameof(data));
            _offset = 0;
            _length = data.Length;
            ReceivedTime = DateTime.Now;
        }

        /// <summary>
        /// 构造接收上下文 (原生内存后端)
        /// 零拷贝引用外部原生内存，释放时调用 <paramref name="release"/> 回调
        /// </summary>
        internal ReceiveContext(NativeMemoryManager manager, Action release)
        {
            if (manager == null) throw new ArgumentNullException(nameof(manager));
            _backing = manager;
            _offset = 0;
            _length = manager.GetSpan().Length;
            _release = release;
            ReceivedTime = DateTime.Now;
        }

        #region IReadOnlyList<byte> 实现 —— 索引器与枚举器

        /// <summary>
        /// 通过索引访问有效数据范围内的字节。
        /// 索引基于有效数据起始位置（即 index=0 对应有效数据的第一个字节）。
        /// </summary>
        /// <param name="index">相对于有效数据起始位置的索引</param>
        /// <returns>指定位置的字节值</returns>
        /// <exception cref="ObjectDisposedException">对象已释放</exception>
        /// <exception cref="IndexOutOfRangeException">索引超出有效数据范围</exception>
        public byte this[int index]
        {
            get
            {
                var mem = Memory;
                if ((uint)index >= (uint)mem.Length)
                    throw new IndexOutOfRangeException($"Index {index} is out of range. Valid range: [0, {mem.Length}).");
                return mem.Span[index];
            }
        }

        /// <summary>
        /// 有效数据的元素数量（等同于 <see cref="Length"/>）
        /// </summary>
        int IReadOnlyCollection<byte>.Count => _length;

        /// <summary>
        /// 返回遍历有效数据范围的枚举器，支持 foreach 和 LINQ 操作。
        /// </summary>
        public IEnumerator<byte> GetEnumerator()
        {
            var mem = Memory;
            for (int i = 0; i < mem.Length; i++)
            {
                yield return mem.Span[i];
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        #endregion

        #region 数据转换方法 —— 替代 ToArray() + BitConverter 的零拷贝方案

        /// <summary>
        /// 将有效数据复制为新的字节数组
        /// </summary>
        public byte[] ToArray() => Memory.ToArray();

        /// <summary>
        /// 将有效数据中指定位置的字节复制到目标数组
        /// </summary>
        /// <param name="destination">目标数组</param>
        /// <param name="destinationIndex">目标数组中的起始写入位置</param>
        /// <exception cref="ObjectDisposedException">对象已释放</exception>
        /// <exception cref="ArgumentNullException">目标数组为 null</exception>
        /// <exception cref="ArgumentOutOfRangeException">目标数组空间不足</exception>
        public void CopyTo(byte[] destination, int destinationIndex)
        {
            if (destination == null)
                throw new ArgumentNullException(nameof(destination));
            var mem = Memory;
            if (destinationIndex < 0 || destinationIndex + mem.Length > destination.Length)
                throw new ArgumentOutOfRangeException(nameof(destinationIndex));
            mem.Span.CopyTo(destination.AsSpan(destinationIndex));
        }

        /// <summary>
        /// 使用指定编码将有效数据转换为字符串
        /// </summary>
        public string EncodeToString(Encoding encoding)
        {
            var mem = Memory;
            var enc = encoding ?? Encoding.UTF8;
            if (MemoryMarshal.TryGetArray(mem, out var segment))
            {
                return enc.GetString(segment.Array, segment.Offset, segment.Count);
            }
#if NETSTANDARD2_0
            return enc.GetString(mem.ToArray());
#else
            return enc.GetString(mem.Span);
#endif
        }

        #endregion

        #region BitConverter 风格的类型转换方法

        /// <summary>
        /// 检查指定范围是否在有效数据内
        /// </summary>
        /// <param name="startIndex">相对于有效数据起始位置的偏移</param>
        /// <param name="byteCount">需要读取的字节数</param>
        /// <returns>有效数据视图</returns>
        private ReadOnlyMemory<byte> EnsureRange(int startIndex, int byteCount)
        {
            var mem = Memory;
            if (startIndex < 0 || startIndex + byteCount > mem.Length)
                throw new ArgumentOutOfRangeException(nameof(startIndex),
                    $"Cannot read {byteCount} byte(s) at offset {startIndex}. Valid range: [0, {mem.Length}).");
            return mem;
        }

        /// <summary>
        /// 读取指定范围的字节，根据字节序决定是否反转。
        /// 托管数组后端：无需反转时直接引用原始数组（零拷贝）；
        /// 原生内存后端：复制不超过 8 字节到临时数组。
        /// </summary>
        /// <param name="startIndex">相对于有效数据起始位置的偏移</param>
        /// <param name="byteCount">需要读取的字节数</param>
        /// <param name="bigEndian">数据是否为大端序</param>
        /// <param name="array">输出：用于 BitConverter 的数组</param>
        /// <param name="index">输出：用于 BitConverter 的起始索引</param>
        private void ReadBytesWithEndian(int startIndex, int byteCount, bool bigEndian, out byte[] array, out int index)
        {
            var mem = EnsureRange(startIndex, byteCount);
            var span = mem.Span.Slice(startIndex, byteCount);

            // 当请求的字节序与系统字节序不同时，需要反转字节
            bool needReverse = bigEndian == BitConverter.IsLittleEndian;
            if (!needReverse)
            {
                if (MemoryMarshal.TryGetArray(mem, out var segment))
                {
                    array = segment.Array;
                    index = segment.Offset + startIndex;
                }
                else
                {
                    // 原生内存后端：复制小段到临时数组
                    byte[] temp = new byte[byteCount];
                    span.CopyTo(temp);
                    array = temp;
                    index = 0;
                }
            }
            else
            {
                byte[] temp = new byte[byteCount];
                for (int i = 0; i < byteCount; i++)
                {
                    temp[i] = span[byteCount - 1 - i];
                }
                array = temp;
                index = 0;
            }
        }

        /// <summary>
        /// 从有效数据的指定位置读取一个 Boolean 值（1 字节）。
        /// 单字节值无字节序问题。
        /// </summary>
        /// <param name="startIndex">相对于有效数据起始位置的偏移</param>
        public bool ToBoolean(int startIndex)
        {
            var mem = EnsureRange(startIndex, 1);
            if (MemoryMarshal.TryGetArray(mem, out var segment))
            {
                return BitConverter.ToBoolean(segment.Array, segment.Offset + startIndex);
            }
            return mem.Span[startIndex] != 0;
        }

        /// <summary>
        /// 从有效数据的指定位置读取一个 Char 值（2 字节）
        /// </summary>
        /// <param name="startIndex">相对于有效数据起始位置的偏移</param>
        /// <param name="bigEndian">数据是否为大端序，默认 false（小端序）</param>
        public char ToChar(int startIndex, bool bigEndian = false)
        {
            ReadBytesWithEndian(startIndex, 2, bigEndian, out var arr, out var pos);
            return BitConverter.ToChar(arr, pos);
        }

        /// <summary>
        /// 从有效数据的指定位置读取一个 Int16 值（2 字节）
        /// </summary>
        /// <param name="startIndex">相对于有效数据起始位置的偏移</param>
        /// <param name="bigEndian">数据是否为大端序，默认 false（小端序）</param>
        public short ToInt16(int startIndex, bool bigEndian = false)
        {
            ReadBytesWithEndian(startIndex, 2, bigEndian, out var arr, out var pos);
            return BitConverter.ToInt16(arr, pos);
        }

        /// <summary>
        /// 从有效数据的指定位置读取一个 UInt16 值（2 字节）
        /// </summary>
        /// <param name="startIndex">相对于有效数据起始位置的偏移</param>
        /// <param name="bigEndian">数据是否为大端序，默认 false（小端序）</param>
        public ushort ToUInt16(int startIndex, bool bigEndian = false)
        {
            ReadBytesWithEndian(startIndex, 2, bigEndian, out var arr, out var pos);
            return BitConverter.ToUInt16(arr, pos);
        }

        /// <summary>
        /// 从有效数据的指定位置读取一个 Int32 值（4 字节）
        /// </summary>
        /// <param name="startIndex">相对于有效数据起始位置的偏移</param>
        /// <param name="bigEndian">数据是否为大端序，默认 false（小端序）</param>
        public int ToInt32(int startIndex, bool bigEndian = false)
        {
            ReadBytesWithEndian(startIndex, 4, bigEndian, out var arr, out var pos);
            return BitConverter.ToInt32(arr, pos);
        }

        /// <summary>
        /// 从有效数据的指定位置读取一个 UInt32 值（4 字节）
        /// </summary>
        /// <param name="startIndex">相对于有效数据起始位置的偏移</param>
        /// <param name="bigEndian">数据是否为大端序，默认 false（小端序）</param>
        public uint ToUInt32(int startIndex, bool bigEndian = false)
        {
            ReadBytesWithEndian(startIndex, 4, bigEndian, out var arr, out var pos);
            return BitConverter.ToUInt32(arr, pos);
        }

        /// <summary>
        /// 从有效数据的指定位置读取一个 Int64 值（8 字节）
        /// </summary>
        /// <param name="startIndex">相对于有效数据起始位置的偏移</param>
        /// <param name="bigEndian">数据是否为大端序，默认 false（小端序）</param>
        public long ToInt64(int startIndex, bool bigEndian = false)
        {
            ReadBytesWithEndian(startIndex, 8, bigEndian, out var arr, out var pos);
            return BitConverter.ToInt64(arr, pos);
        }

        /// <summary>
        /// 从有效数据的指定位置读取一个 UInt64 值（8 字节）
        /// </summary>
        /// <param name="startIndex">相对于有效数据起始位置的偏移</param>
        /// <param name="bigEndian">数据是否为大端序，默认 false（小端序）</param>
        public ulong ToUInt64(int startIndex, bool bigEndian = false)
        {
            ReadBytesWithEndian(startIndex, 8, bigEndian, out var arr, out var pos);
            return BitConverter.ToUInt64(arr, pos);
        }

        /// <summary>
        /// 从有效数据的指定位置读取一个 Single/float 值（4 字节）
        /// </summary>
        /// <param name="startIndex">相对于有效数据起始位置的偏移</param>
        /// <param name="bigEndian">数据是否为大端序，默认 false（小端序）</param>
        public float ToSingle(int startIndex, bool bigEndian = false)
        {
            ReadBytesWithEndian(startIndex, 4, bigEndian, out var arr, out var pos);
            return BitConverter.ToSingle(arr, pos);
        }

        /// <summary>
        /// 从有效数据的指定位置读取一个 Double 值（8 字节）
        /// </summary>
        /// <param name="startIndex">相对于有效数据起始位置的偏移</param>
        /// <param name="bigEndian">数据是否为大端序，默认 false（小端序）</param>
        public double ToDouble(int startIndex, bool bigEndian = false)
        {
            ReadBytesWithEndian(startIndex, 8, bigEndian, out var arr, out var pos);
            return BitConverter.ToDouble(arr, pos);
        }

        #endregion

        #region ToHexString

        /// <summary>
        /// 十六进制查表 (性能优化, 避免逐字节 ToString)
        /// </summary>
        private static readonly string[] HexByteTable = BuildHexByteTable();

        private static string[] BuildHexByteTable()
        {
            var table = new string[256];
            for (int i = 0; i < 256; i++)
            {
                table[i] = i.ToString("X2");
            }
            return table;
        }

        /// <summary>
        /// 将字节序列转换为 "XX-XX-XX" 格式的十六进制字符串
        /// </summary>
        private static string ConvertToHexString(ReadOnlySpan<byte> span)
        {
            if (span.Length == 0)
                return string.Empty;

            var sb = new StringBuilder(span.Length * 3 - 1);
            for (int i = 0; i < span.Length; i++)
            {
                if (i > 0)
                {
                    sb.Append('-');
                }
                sb.Append(HexByteTable[span[i]]);
            }
            return sb.ToString();
        }

        /// <summary>
        /// 将有效数据转换为十六进制字符串表示（如 "0A-1B-2C"）
        /// </summary>
        public string ToHexString()
        {
            var mem = Memory;
            if (MemoryMarshal.TryGetArray(mem, out var segment))
            {
                return BitConverter.ToString(segment.Array, segment.Offset, segment.Count);
            }
            return ConvertToHexString(mem.Span);
        }

        /// <summary>
        /// 将有效数据中指定范围转换为十六进制字符串表示（如 "0A-1B-2C"）
        /// </summary>
        /// <param name="startIndex">相对于有效数据起始位置的偏移</param>
        /// <param name="length">要转换的字节数</param>
        public string ToHexString(int startIndex, int length)
        {
            var mem = EnsureRange(startIndex, length);
            if (length == 0)
                return string.Empty;
            if (MemoryMarshal.TryGetArray(mem, out var segment))
            {
                return BitConverter.ToString(segment.Array, segment.Offset + startIndex, length);
            }
            return ConvertToHexString(mem.Span.Slice(startIndex, length));
        }

        #endregion

        #region 引用计数与资源释放

        /// <summary>
        /// 增加引用计数
        /// </summary>
        internal void IncrRef()
        {
            Interlocked.Increment(ref _referenceCount);
        }

        /// <summary>
        /// 减少引用计数，当计数为 0 时释放资源
        /// </summary>
        public void Dispose()
        {
            if (Interlocked.Decrement(ref _referenceCount) == 0)
            {
                var release = Interlocked.Exchange(ref _release, null);
                Interlocked.Exchange(ref _backing, null);
                release?.Invoke();
            }
        }

        #endregion
    }
}
