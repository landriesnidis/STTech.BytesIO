using STTech.CodePlus.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace STTech.BytesIO.Core
{
    /// <summary>
    /// 共享内存池中的共享单元
    /// </summary>
    public struct MemoryBlock : IDisposable
    {
        private byte[] data;
        private ArraySegment<byte> segment;

        /// <summary>
        /// 内存块原始数据
        /// </summary>
        internal byte[] Data
        {
            get
            {
                if (IsDisposed)
                {
                    throw new InvalidOperationException("该内存块已经被释放。");
                }
                return data;
            }
        }

        // 所属内存池
        internal readonly MemoryBlockPool pool;

        /// <summary>
        /// 是否已销毁
        /// </summary>
        public bool IsDisposed { get; private set; } = false;

        /// <summary>
        /// 是否自动销毁
        /// </summary>
        public bool AutoDispose { get; set; } = true;

        /// <summary>
        /// 有效数据
        /// </summary>
        public ArraySegment<byte> Segment
        {
            get => segment;
            internal set
            {
                if (segment == default || segment.Array == value.Array)
                {
                    segment = value;
                }
                else
                {
                    throw new InvalidOperationException("ArraySegment的指向的数组不相同");
                }
            }
        }

        public MemoryBlock(byte[] data)
        {
            this.data = data;
            Segment = new ArraySegment<byte>(data);
        }

        internal MemoryBlock(byte[] data, MemoryBlockPool pool)
        {
            this.data = data;
            Segment = new ArraySegment<byte>(data);
            this.pool = pool;
        }

        internal MemoryBlock(ArraySegment<byte> segment, int offset, int count, MemoryBlockPool pool)
        {
            data = segment.Array;
            Segment = new ArraySegment<byte>(Data, segment.Offset + offset, count);
            this.pool = pool;
        }

        /// <summary>
        /// 裁切数据
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public MemoryBlock Cut(int offset, int count)
        {
            return new MemoryBlock(Segment, offset, count, pool);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (!IsDisposed)
            {
                pool?.Release(this);
                data = null;
                IsDisposed = true;
            }
        }
    }

    /// <summary>
    /// 内存块池
    /// </summary>
    public class MemoryBlockPool : ObjectPool<MemoryBlock>
    {
        private readonly ByteArrayPool ByteArrayPool;

        /// <summary>
        /// 单个内存块的大小
        /// </summary>
        public int BlockSize { get => ByteArrayPool.BlockSize; }

        public MemoryBlockPool(int blockSize, int maxRetainCount = 10) : base(maxRetainCount)
        {
            ByteArrayPool = new ByteArrayPool(blockSize, maxRetainCount);
        }

        /// <inheritdoc/>
        protected override MemoryBlock ConstructObjectHandler()
        {
            return new MemoryBlock(ByteArrayPool.Get(), this);
        }

        /// <inheritdoc/>
        protected override void OnMaxRetainCountChanged()
        {
            if (ByteArrayPool != null)
            {
                ByteArrayPool.MaxRetainCount = MaxRetainCount;
            }
        }

        /// <inheritdoc/>
        public override MemoryBlock Get()
        {
            var block = ConstructObjectHandler();
            Console.WriteLine($"借出 {ByteArrayPool.Count} | {ByteArrayPool.FreeCount}");
            return block;
        }

        /// <inheritdoc/>
        public override void Release(MemoryBlock item)
        {
            ByteArrayPool.Release(item.Data);
            Console.WriteLine($"归还 {ByteArrayPool.Count} | {ByteArrayPool.FreeCount}");
        }
    }

    /// <summary>
    /// 字节数组内存池
    /// </summary>
    public class ByteArrayPool : ObjectPool<byte[]>
    {
        /// <summary>
        /// 每一块字节数组的大小
        /// </summary>
        public readonly int BlockSize;

        public ByteArrayPool(int blockSize, int maxRetainCount) : base(maxRetainCount)
        {
            BlockSize = blockSize;
        }

        /// <inheritdoc/>
        protected override byte[] ConstructObjectHandler()
        {
            return new byte[BlockSize];
        }
    }


    public static class MemoryBlockExtensions
    {
        /// <summary>
        /// 转为指定编码(默认UTF-8)的字符串
        /// </summary>
        /// <param name="block"></param>
        /// <param name="encode"></param>
        /// <returns></returns>
        public static string EncodeToString(this MemoryBlock block, string encode = "utf-8")
        {
            return Encoding.GetEncoding(encode).GetString(block.Segment.ToArray());
        }
    }
}
