using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace STTech.BytesIO.Core
{
    /// <summary>
    /// 接收数据上下文
    /// 封装从通信接收到的数据，底层使用 ArrayPool 池化内存。
    /// 支持 GC 兜底：忘记调用 Dispose 不会导致内存泄漏，但主动 Dispose 可以更快地归还池化内存。
    /// 
    /// 实现了 <see cref="IReadOnlyList{T}"/> 接口，可以像字节数组一样通过索引访问数据，
    /// 并且支持 LINQ 操作（如 Select、Where、Skip、Take、SequenceEqual 等）。
    /// 所有操作均限定在有效数据范围 [offset, offset+length) 内，用户无需关心底层池化数组的真实大小。
    /// </summary>
    public class ReceiveContext : IDisposable, IReadOnlyList<byte>
    {
        private byte[] _rentedArray;
        private readonly int _offset;
        private readonly int _length;
        private int _referenceCount = 1;

        public ReadOnlyMemory<byte> Memory
        {
            get
            {
                var arr = _rentedArray;
                if (arr == null)
                    throw new ObjectDisposedException(nameof(ReceiveContext));
                return new ReadOnlyMemory<byte>(arr, _offset, _length);
            }
        }

        /// <summary>
        /// 有效数据长度
        /// </summary>
        public int Length => _length;

        public DateTime ReceivedTime { get; }
        public bool IsDisposed => _rentedArray == null;
        public IDictionary<string, object> Properties { get; } = new Dictionary<string, object>();

        internal ReceiveContext(byte[] rentedArray, int offset, int length)
        {
            _rentedArray = rentedArray ?? throw new ArgumentNullException(nameof(rentedArray));
            _offset = offset;
            _length = length;
            ReceivedTime = DateTime.Now;
        }

        public ReceiveContext(byte[] data)
        {
            _rentedArray = data ?? throw new ArgumentNullException(nameof(data));
            _offset = 0;
            _length = data.Length;
            ReceivedTime = DateTime.Now;
            _isPooled = false;
        }

        private readonly bool _isPooled = true;

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
                var arr = _rentedArray;
                if (arr == null)
                    throw new ObjectDisposedException(nameof(ReceiveContext));
                if ((uint)index >= (uint)_length)
                    throw new IndexOutOfRangeException($"Index {index} is out of range. Valid range: [0, {_length}).");
                return arr[_offset + index];
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
            var arr = _rentedArray;
            if (arr == null)
                throw new ObjectDisposedException(nameof(ReceiveContext));
            int end = _offset + _length;
            for (int i = _offset; i < end; i++)
            {
                yield return arr[i];
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
            var arr = _rentedArray;
            if (arr == null)
                throw new ObjectDisposedException(nameof(ReceiveContext));
            if (destinationIndex < 0 || destinationIndex + _length > destination.Length)
                throw new ArgumentOutOfRangeException(nameof(destinationIndex));
            Buffer.BlockCopy(arr, _offset, destination, destinationIndex, _length);
        }

        /// <summary>
        /// 使用指定编码将有效数据转换为字符串
        /// </summary>
        public string ToString(Encoding encoding)
        {
            var arr = _rentedArray;
            if (arr == null)
                throw new ObjectDisposedException(nameof(ReceiveContext));
            return (encoding ?? Encoding.UTF8).GetString(arr, _offset, _length);
        }

        public override string ToString() => ToString(Encoding.UTF8);

        #endregion

        #region BitConverter 风格的类型转换方法

        /// <summary>
        /// 检查指定范围是否在有效数据内
        /// </summary>
        /// <param name="startIndex">相对于有效数据起始位置的偏移</param>
        /// <param name="byteCount">需要读取的字节数</param>
        private byte[] EnsureRange(int startIndex, int byteCount)
        {
            var arr = _rentedArray;
            if (arr == null)
                throw new ObjectDisposedException(nameof(ReceiveContext));
            if (startIndex < 0 || startIndex + byteCount > _length)
                throw new ArgumentOutOfRangeException(nameof(startIndex),
                    $"Cannot read {byteCount} byte(s) at offset {startIndex}. Valid range: [0, {_length}).");
            return arr;
        }

        /// <summary>
        /// 读取指定范围的字节，根据字节序需要决定是否反转。
        /// 当需要反转时，返回反转后的临时数组和起始索引 0；
        /// 当无需反转时，直接返回原始数组和实际偏移位置（零拷贝）。
        /// </summary>
        /// <param name="startIndex">相对于有效数据起始位置的偏移</param>
        /// <param name="byteCount">需要读取的字节数</param>
        /// <param name="bigEndian">数据是否为大端序</param>
        /// <param name="array">输出：用于 BitConverter 的数组</param>
        /// <param name="index">输出：用于 BitConverter 的起始索引</param>
        private void ReadBytesWithEndian(int startIndex, int byteCount, bool bigEndian, out byte[] array, out int index)
        {
            var arr = EnsureRange(startIndex, byteCount);
            int pos = _offset + startIndex;

            // 当请求的字节序与系统字节序不同时，需要反转字节
            bool needReverse = bigEndian == BitConverter.IsLittleEndian;
            if (!needReverse)
            {
                array = arr;
                index = pos;
            }
            else
            {
                byte[] temp = new byte[byteCount];
                for (int i = 0; i < byteCount; i++)
                {
                    temp[i] = arr[pos + byteCount - 1 - i];
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
            var arr = EnsureRange(startIndex, 1);
            return BitConverter.ToBoolean(arr, _offset + startIndex);
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

        /// <summary>
        /// 将有效数据转换为十六进制字符串表示（如 "0A-1B-2C"）
        /// </summary>
        public string ToHexString()
        {
            var arr = _rentedArray;
            if (arr == null)
                throw new ObjectDisposedException(nameof(ReceiveContext));
            if (_length == 0)
                return string.Empty;
            return BitConverter.ToString(arr, _offset, _length);
        }

        /// <summary>
        /// 将有效数据中指定范围转换为十六进制字符串表示（如 "0A-1B-2C"）
        /// </summary>
        /// <param name="startIndex">相对于有效数据起始位置的偏移</param>
        /// <param name="length">要转换的字节数</param>
        public string ToHexString(int startIndex, int length)
        {
            var arr = EnsureRange(startIndex, length);
            if (length == 0)
                return string.Empty;
            return BitConverter.ToString(arr, _offset + startIndex, length);
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
                var arr = Interlocked.Exchange(ref _rentedArray, null);
                if (arr != null && _isPooled)
                {
                    ArrayPool<byte>.Shared.Return(arr);
                }
            }
        }

        #endregion
    }
}
