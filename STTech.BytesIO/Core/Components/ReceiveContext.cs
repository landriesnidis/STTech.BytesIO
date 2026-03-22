using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace STTech.BytesIO.Core
{
    /// <summary>
    /// 接收数据上下文
    /// 封装从通信接收到的数据，底层使用 ArrayPool 池化内存。
    /// 支持 GC 兜底：忘记调用 Dispose 不会导致内存泄漏，但主动 Dispose 可以更快地归还池化内存。
    /// </summary>
    public class ReceiveContext : IDisposable
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

        public byte[] ToArray() => Memory.ToArray();

        public string ToString(Encoding encoding)
        {
            var arr = _rentedArray;
            if (arr == null)
                throw new ObjectDisposedException(nameof(ReceiveContext));
            return (encoding ?? Encoding.UTF8).GetString(arr, _offset, _length);
        }

        public override string ToString() => ToString(Encoding.UTF8);

        /// <summary>
        /// 增加引用计数
        /// </summary>
        public void IncrRef()
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
    }
}
