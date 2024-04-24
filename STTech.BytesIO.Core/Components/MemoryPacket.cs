using STTech.CodePlus.Algorithm;
using System;
using System.Collections.Generic;
using System.Linq;

namespace STTech.BytesIO.Core.Component
{
    /// <summary>
    /// 内存数据包，由解包器拆解或组合的内存块构成
    /// </summary>
    public class MemoryPacket : IDisposable
    {
        private Queue<MemoryBlock> blocks = new Queue<MemoryBlock>();
        private bool disposedValue;

        /// <summary>
        /// 计算有效数据长度
        /// </summary>
        public int Count() => blocks.Sum(x => x.Segment.Count);

        /// <summary>
        /// 构造解包器数据包
        /// </summary>
        /// <param name="block"></param>
        internal MemoryPacket(MemoryBlock block)
        {
            blocks.Enqueue(block);
        }

        /// <summary>
        /// 读取指定长度的字节数组
        /// </summary>
        /// <param name="count">读取长度</param>
        /// <param name="offset">起始位偏移量</param>
        /// <returns></returns>
        internal byte[] ReadBytes(int count, int offset = 0)
        {
            var totalLen = blocks.Count();
            if (offset + count > totalLen)
            {
                return new byte[0];
            }

            Queue<MemoryBlock> mbs = new Queue<MemoryBlock>(blocks);

            if (offset > 0)
            {
                while (mbs.Any())
                {
                    var block = mbs.Peek();
                    if (block.Segment.Count <= offset)
                    {
                        mbs.Dequeue();
                    }
                    else
                    {
                        break;
                    }
                    offset -= block.Segment.Count;
                }
            }

            List<byte> bytes = new List<byte>();
            while (mbs.Any())
            {
                var block = mbs.Dequeue();

                var arr = block.Segment.Skip(offset).Take(count).ToArray();
                bytes.AddRange(arr);
                if (offset > 0)
                {
                    offset = 0;
                }
                count -= arr.Length;
                if (count == 0)
                {
                    break;
                }
            }

            return bytes.ToArray();
        }

        /// <summary>
        /// 追加数据块
        /// </summary>
        /// <param name="block"></param>
        internal void Append(MemoryBlock block)
        {
            blocks.Enqueue(block);
        }

        /// <summary>
        /// 返回类型化为 IEnumerable<T> 的输入。
        /// </summary>
        /// <returns></returns>
        internal IEnumerable<byte> AsEnumerable()
        {
            return blocks.SelectMany(x => x.Segment.ToArray());
        }

        /// <summary>
        /// 获取有效数据数组
        /// </summary>
        /// <returns></returns>
        public byte[] ToArray()
        {
            return AsEnumerable().ToArray();
        }

        /// <summary>
        /// 在字节数据包中搜索指定字节数组子串，并返回其首个匹配项的索引。
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        public int IndexOf(byte[] target)
        {
            return KMP.IndexOf(blocks.Select(x => x.Segment as IEnumerable<byte>).ToArray(), target);
        }

        internal void Skip(int offset)
        {
            var totalLen = blocks.Count();
            if (totalLen > offset)
            {
                while (blocks.Any())
                {
                    var block = blocks.Peek();
                    if (block.Segment.Count <= offset)
                    {
                        block.Dispose();
                        blocks.Dequeue();
                        offset -= block.Segment.Count;
                    }
                    else
                    {
                        block.Segment = new ArraySegment<byte>(block.Segment.Array, block.Segment.Offset + offset, block.Segment.Count - offset);
                        return;
                    }
                }
            }
            else if (totalLen == offset)
            {
                blocks.ForEach(x => x.Dispose());
                blocks.Clear();
                return;
            }

            throw new IndexOutOfRangeException();
        }


        /// <inheritdoc/>
        public void Dispose()
        {
            // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    blocks.ForEach(x => x.Dispose());
                    blocks.Clear();
                    blocks = null;
                }

                // TODO: 释放未托管的资源(未托管的对象)并重写终结器
                // TODO: 将大型字段设置为 null
                disposedValue = true;
            }
        }
    }
}
