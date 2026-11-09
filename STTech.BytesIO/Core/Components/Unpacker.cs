using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;

namespace STTech.BytesIO.Core.Component
{
    /// <summary>
    /// 数据解包器
    /// </summary>
    public abstract class Unpacker
    {
        /// <summary>
        /// 当解析出结果时发生
        /// </summary>
        public event EventHandler<DataParsedEventArgs> OnDataParsed;

        /// <summary>
        /// 缓存的接收上下文列表
        /// </summary>
        private List<ReceiveContext> _cachedContexts;

        /// <summary>
        /// 缓存有效数据总长度
        /// </summary>
        private int _cachedTotalLength = 0;

        /// <summary>
        /// 缓存中已跳过的偏移量（第一个块的起始偏移）
        /// </summary>
        private int _cachedStartOffset = 0;

        /// <summary>
        /// 异步锁
        /// </summary>
        private readonly object asyncDataCacheLocker = new object();

        /// <summary>
        /// 错误发生时处理回调
        /// in : 错误类型
        /// out: 是否清空缓存
        /// </summary>
        public Func<ErrorCode, bool> ErrorOccurHandler { get; set; }

        /// <summary>
        /// 计算数据包长度的处理程序
        /// 输入当前缓存的数据
        /// 输出第一个数据包的长度：
        /// > 0: 数据包总长度
        /// == 0: 暂无法判断或数据不足，等待更多数据
        /// &lt; 0: 检测到坏数据，跳过指定长度的字节进行滑动自愈探测 (如 -1 表示跳过 1 字节)
        /// </summary>
        protected abstract int CalculatePacketLength(ReadOnlySequence<byte> buffer);

        /// <summary>
        /// 起始标记
        /// </summary>
        public byte[] StartMark { get; set; }

        /// <summary>
        /// 最大允许缓存的数据长度（字节），默认 8MB。
        /// 当缓存堆积超过此上限时触发缓冲区溢出防御，避免恶意输入或坏包导致内存无限增长(OOM)。
        /// </summary>
        public int MaxBufferSize { get; set; } = 8 * 1024 * 1024;

        /// <summary>
        /// 中断帧(断包)拼接的超时时长
        /// 单位毫秒
        /// 值为0时不启用超时检查
        /// </summary>
        public int InterruptFrameTimeoutValue { get; set; } = 0;

        /// <summary>
        /// 中断帧(断包头部)的接收单调时间刻度 (Environment.TickCount64)
        /// </summary>
        private long? _interruptFrameTimestamp = null;

        /// <summary>
        /// 构造解包器
        /// </summary>
        protected Unpacker()
        {

        }

        /// <summary>
        /// 获取指定长度所覆盖的所有底层段上下文，用于对段建立生命周期引用保护
        /// </summary>
        private List<ReceiveContext> GetReferencedContexts(int length)
        {
            var list = new List<ReceiveContext>();
            if (_cachedContexts == null || _cachedContexts.Count == 0 || length <= 0)
                return list;

            int remaining = length;
            int offset = _cachedStartOffset;

            foreach (var ctx in _cachedContexts)
            {
                list.Add(ctx);
                int available = ctx.Length - offset;
                if (remaining <= available)
                {
                    break;
                }
                remaining -= available;
                offset = 0;
            }

            return list;
        }

        /// <summary>
        /// 从缓存构建 ReadOnlySequence
        /// </summary>
        private ReadOnlySequence<byte> BuildSequence()
        {
            if (_cachedContexts == null || _cachedContexts.Count == 0)
                return ReadOnlySequence<byte>.Empty;

            if (_cachedContexts.Count == 1)
            {
                var ctx = _cachedContexts[0];
                var mem = ctx.Memory;
                return new ReadOnlySequence<byte>(mem.Slice(_cachedStartOffset));
            }

            // 多块情况：构建链式 ReadOnlySequenceSegment
            BufferSegment first = null;
            BufferSegment last = null;

            for (int i = 0; i < _cachedContexts.Count; i++)
            {
                var ctx = _cachedContexts[i];
                var mem = ctx.Memory;

                // 第一个块需要跳过已消费的偏移
                if (i == 0)
                {
                    mem = mem.Slice(_cachedStartOffset);
                }

                var segment = new BufferSegment(mem);
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

        /// <summary>
        /// 在序列中搜索指定字节数组，返回其首个匹配项的索引（零堆分配跨段匹配）
        /// </summary>
        private static int IndexOf(ReadOnlySequence<byte> sequence, byte[] target)
        {
            if (target == null || target.Length == 0)
                return -1;

            long totalLength = sequence.Length;
            if (totalLength < target.Length)
                return -1;

            if (sequence.IsSingleSegment)
            {
                return IndexOfInSpan(sequence.First.Span, target);
            }

            byte firstByte = target[0];
            int targetLen = target.Length;
            long maxSearchIndex = totalLength - targetLen;

            long segmentGlobalOffset = 0;
            var position = sequence.Start;

            while (sequence.TryGet(ref position, out ReadOnlyMemory<byte> memory, advance: true))
            {
                var span = memory.Span;
                int spanOffset = 0;

                while (spanOffset < span.Length)
                {
                    int foundInSpan = span.Slice(spanOffset).IndexOf(firstByte);
                    if (foundInSpan < 0)
                    {
                        break;
                    }

                    int candInSpan = spanOffset + foundInSpan;
                    long candGlobal = segmentGlobalOffset + candInSpan;

                    if (candGlobal > maxSearchIndex)
                    {
                        return -1;
                    }

                    // 若目标完全落在当前段内，直接利用 Span 比对
                    if (candInSpan + targetLen <= span.Length)
                    {
                        if (span.Slice(candInSpan, targetLen).SequenceEqual(target))
                        {
                            return (int)candGlobal;
                        }
                    }
                    else
                    {
                        // 跨段边界时进行切片序列比对
                        var slice = sequence.Slice(candGlobal, targetLen);
                        if (Matches(slice, target))
                        {
                            return (int)candGlobal;
                        }
                    }

                    spanOffset = candInSpan + 1;
                }

                segmentGlobalOffset += span.Length;
                if (segmentGlobalOffset > maxSearchIndex)
                {
                    break;
                }
            }

            return -1;
        }

        private static bool Matches(ReadOnlySequence<byte> slice, byte[] target)
        {
            if (slice.IsSingleSegment)
            {
                return slice.First.Span.SequenceEqual(target);
            }

            int idx = 0;
            foreach (var memory in slice)
            {
                var span = memory.Span;
                for (int j = 0; j < span.Length; j++)
                {
                    if (span[j] != target[idx++])
                        return false;
                }
            }
            return true;
        }

        /// <summary>
        /// 在 byte 数组/Span 中搜索目标字节数组（硬件加速首字节探测）
        /// </summary>
        private static int IndexOfInSpan(ReadOnlySpan<byte> span, byte[] target)
        {
            if (target.Length == 0) return 0;
            if (span.Length < target.Length) return -1;

            byte first = target[0];
            int max = span.Length - target.Length;
            int offset = 0;

            while (offset <= max)
            {
                int idx = span.Slice(offset, max - offset + 1).IndexOf(first);
                if (idx < 0) return -1;

                int cand = offset + idx;
                if (span.Slice(cand, target.Length).SequenceEqual(target))
                {
                    return cand;
                }
                offset = cand + 1;
            }

            return -1;
        }

        /// <summary>
        /// 跳过缓存中指定长度的数据
        /// </summary>
        protected internal void SkipCachedBytes(int count)
        {
            _cachedTotalLength -= count;

            while (count > 0 && _cachedContexts.Count > 0)
            {
                var first = _cachedContexts[0];
                int availableInFirst = first.Length - _cachedStartOffset;

                if (count >= availableInFirst)
                {
                    // 消费完整个块
                    count -= availableInFirst;
                    _cachedStartOffset = 0;
                    _cachedContexts.RemoveAt(0);
                    first.Dispose();
                }
                else
                {
                    // 部分消费
                    _cachedStartOffset += count;
                    count = 0;
                }
            }
        }

        /// <summary>
        /// 清空缓存
        /// </summary>
        private void ClearCachedContexts()
        {
            if (_cachedContexts != null)
            {
                foreach (var ctx in _cachedContexts)
                {
                    ctx.Dispose();
                }
                _cachedContexts.Clear();
            }
            _cachedTotalLength = 0;
            _cachedStartOffset = 0;
        }

        /// <summary>
        /// 输入收到的数据
        /// </summary>
        /// <param name="context">接收到的数据上下文</param>
        public void Input(ReceiveContext context)
        {
            lock (asyncDataCacheLocker)
            {
                // 是否有缓存数据
                if (_cachedContexts == null || _cachedContexts.Count == 0)
                {
                    // 若无缓存数据则当前数据即为缓存数据
                    context.IncrRef();
                    _cachedContexts = new List<ReceiveContext> { context };
                    _cachedTotalLength = context.Length;
                    _cachedStartOffset = 0;
                }
                else
                {
                    // 是否启用断包拼包的超时限制 (基于单调时钟 Environment.TickCount64 判定)
                    if (InterruptFrameTimeoutValue > 0 &&
                        _interruptFrameTimestamp != null &&
                        MonotonicClock.NowMs - _interruptFrameTimestamp.Value > InterruptFrameTimeoutValue)
                    {
                        // 直接替换缓存数据（丢弃断包头部）
                        ClearCachedContexts();
                        context.IncrRef();
                        _cachedContexts = new List<ReceiveContext> { context };
                        _cachedTotalLength = context.Length;
                        _cachedStartOffset = 0;
                        _interruptFrameTimestamp = null;
                    }
                    else
                    {
                        // 若有缓存数据则将原数据与新数据合并
                        context.IncrRef();
                        _cachedContexts.Add(context);
                        _cachedTotalLength += context.Length;
                    }
                }

                // 缓冲区溢出防御：防止恶意或脏数据流导致无界缓存增长(OOM)
                if (_cachedTotalLength > MaxBufferSize)
                {
                    if (ErrorOccurHandler?.Invoke(ErrorCode.BufferOverflow) == true)
                    {
                        ClearCachedContexts();
                        return;
                    }
                    ClearCachedContexts();
                    return;
                }

                while (_cachedContexts != null && _cachedContexts.Count > 0)
                {
                    // 当前缓存数据长度
                    var cacheLen = _cachedTotalLength;

                    // 构建 ReadOnlySequence
                    var sequence = BuildSequence();

                    // 判断是否设置了固定的起始标志位
                    if (StartMark != null && StartMark.Length > 0)
                    {
                        // 判断已收到数据的长度是否足够起始标记的长度
                        if (cacheLen < StartMark.Length)
                        {
                            return;
                        }

                        // 计算起始标识在数据中的位置
                        var idx = IndexOf(sequence, StartMark);

                        if (idx != 0)
                        {
                            if (idx == -1)
                            {
                                // 未找到起始位则只保留起始标识长度-1位的数据
                                if (ErrorOccurHandler?.Invoke(ErrorCode.StartMarkNotMatch) == true)
                                {
                                    ClearCachedContexts();
                                    return;
                                }

                                int keepLen = StartMark.Length - 1;
                                int skipLen = cacheLen - keepLen;
                                if (skipLen > 0)
                                {
                                    SkipCachedBytes(skipLen);
                                }
                                return;
                            }
                            else
                            {
                                // 若找到起始位则认为缓冲区前段混入脏数据，跳过这些字节
                                SkipCachedBytes(idx);
                                continue;
                            }
                        }
                    }

                    // 重新构建序列（跳过操作可能改变了缓存）
                    sequence = BuildSequence();

                    // 通过回调提供的方法计算该包的长度(根据具体协议)
                    int packetLen = CalculatePacketLength(sequence);

                    // 当返回的数据包长度小于0时，表示检测到坏数据，跳过指定长度的字节进行滑动自愈探测
                    if (packetLen < 0)
                    {
                        if (ErrorOccurHandler?.Invoke(ErrorCode.InvalidPacket) == true)
                        {
                            ClearCachedContexts();
                            return;
                        }
                        int discardCount = -packetLen;
                        if (discardCount > cacheLen) discardCount = cacheLen;
                        SkipCachedBytes(discardCount);
                        continue;
                    }

                    // 当返回的数据包长度等于0时，表示当前缓存中的数据无法判断出数据包的长度
                    // 当返回的数据包长度计算结果大于缓存数据长度时，则结束本次处理
                    if (packetLen == 0 || packetLen > cacheLen)
                    {
                        if (InterruptFrameTimeoutValue > 0)
                        {
                            _interruptFrameTimestamp = MonotonicClock.NowMs;
                        }
                        return;
                    }

                    // 取出解包数据（零拷贝切片）
                    var data = sequence.Slice(0, packetLen);
                    var referenced = GetReferencedContexts(packetLen);
                    var unpackContext = new UnpackContext(data, referenced);

                    // 将拆粘包的结果通过回调同步返回（通过具备引用保护的上下文包装）
                    OnDataParsed?.Invoke(this, new DataParsedEventArgs(unpackContext));

                    // 处理结束后跳过已消费字节（此时即使废弃段被解包器 Dispose，由于 UnpackContext 保持了引用，底层内存绝对安全）
                    SkipCachedBytes(packetLen);

                    // 如果缓存已清空
                    if (_cachedTotalLength <= 0)
                    {
                        ClearCachedContexts();
                        _cachedContexts = null;
                    }
                }
            }
        }

        /// <summary>
        /// 清空缓存数据
        /// </summary>
        public void ClearCache()
        {
            lock (asyncDataCacheLocker)
            {
                ClearCachedContexts();
                _cachedContexts = null;
            }
        }

        /// <summary>
        /// 错误类型
        /// </summary>
        public enum ErrorCode
        {
            /// <summary>
            /// 起始标记不匹配
            /// </summary>
            StartMarkNotMatch,

            /// <summary>
            /// 数据包无效或校验失败
            /// </summary>
            InvalidPacket,

            /// <summary>
            /// 缓冲区超出最大长度限制
            /// </summary>
            BufferOverflow,
        }
    }

    /// <summary>
    /// 用于构建 ReadOnlySequence 的内部段
    /// </summary>
    internal class BufferSegment : ReadOnlySequenceSegment<byte>
    {
        public BufferSegment(ReadOnlyMemory<byte> memory)
        {
            Memory = memory;
        }

        public void SetNext(BufferSegment next)
        {
            next.RunningIndex = RunningIndex + Memory.Length;
            Next = next;
        }
    }
}
