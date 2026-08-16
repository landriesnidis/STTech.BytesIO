using System;
using System.Buffers;
using System.Runtime.InteropServices;

namespace STTech.BytesIO.Core
{
    /// <summary>
    /// 原生(非托管)内存的 MemoryManager 桥接
    /// 将 IntPtr 指向的内存块暴露为 <see cref="Memory{T}"/> / <see cref="Span{T}"/>，
    /// 用于支持共享内存等零拷贝接收场景。
    /// 原生内存由调用方负责生命周期管理，本管理器不负责释放。
    /// </summary>
    internal sealed unsafe class NativeMemoryManager : MemoryManager<byte>
    {
        private readonly IntPtr _pointer;
        private readonly int _length;

        /// <summary>
        /// 构造原生内存管理器
        /// </summary>
        /// <param name="pointer">内存起始地址</param>
        /// <param name="length">内存长度(字节)</param>
        public NativeMemoryManager(IntPtr pointer, int length)
        {
            if (pointer == IntPtr.Zero) throw new ArgumentNullException(nameof(pointer));
            if (length < 0) throw new ArgumentOutOfRangeException(nameof(length));
            _pointer = pointer;
            _length = length;
        }

        /// <inheritdoc/>
        public override Span<byte> GetSpan() => new Span<byte>((void*)_pointer, _length);

        /// <inheritdoc/>
        public override MemoryHandle Pin(int elementIndex = 0)
        {
            if ((uint)elementIndex >= (uint)_length) throw new ArgumentOutOfRangeException(nameof(elementIndex));
            return new MemoryHandle((void*)((byte*)_pointer + elementIndex));
        }

        /// <inheritdoc/>
        public override void Unpin()
        {
        }

        /// <inheritdoc/>
        protected override void Dispose(bool disposing)
        {
        }
    }
}
