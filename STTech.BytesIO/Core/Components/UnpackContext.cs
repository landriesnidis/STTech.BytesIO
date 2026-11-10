using System;
using System.Buffers;
using System.Collections.Generic;
using System.Threading;

namespace STTech.BytesIO.Core.Component
{
    /// <summary>
    /// 解包数据上下文
    /// 封装了网络包解析后的只读内存序列以及关联的上下文数据，支持段生命周期引用计数管理以彻底杜绝异步消费踩内存 (Use-After-Free)
    /// </summary>
    public class UnpackContext : IDisposable
    {
        private List<ReceiveContext> _referencedContexts;
        private int _isDisposed;

        /// <summary>
        /// 完整数据包的内存视图（零拷贝序列）
        /// </summary>
        public ReadOnlySequence<byte> Data { get; }

        /// <summary>
        /// 附加属性字典，方便在解析和管道传递过程中携带额外信息
        /// </summary>
        public IDictionary<string, object> Properties { get; } = new Dictionary<string, object>();

        /// <summary>
        /// 关联的客户端对象 (若为独立解包器可能为 null)
        /// </summary>
        public BytesClient Client { get; internal set; }

        /// <summary>
        /// 是否已被释放
        /// </summary>
        public bool IsDisposed => _isDisposed != 0;

        /// <summary>
        /// 初始化 <see cref="UnpackContext"/> 类的新实例。
        /// </summary>
        /// <param name="data">只读字节序列数据</param>
        public UnpackContext(ReadOnlySequence<byte> data)
        {
            Data = data;
        }

        internal UnpackContext(ReadOnlySequence<byte> data, List<ReceiveContext> referencedContexts) : this(data)
        {
            if (referencedContexts != null && referencedContexts.Count > 0)
            {
                _referencedContexts = referencedContexts;
                // 对所有涉及的分片段增加引用计数，确保即便 Unpacker 自身消费跳过了该段，底层池化内存也不会被提前归还覆盖
                foreach (var ctx in _referencedContexts)
                {
                    ctx?.IncrRef();
                }
            }
        }

        /// <summary>
        /// 释放解包上下文所持有的段引用
        /// </summary>
        public void Dispose()
        {
            if (Interlocked.Exchange(ref _isDisposed, 1) != 0) return;

            if (_referencedContexts != null)
            {
                foreach (var ctx in _referencedContexts)
                {
                    ctx?.Dispose();
                }
                _referencedContexts = null;
            }

            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// 终结器，确保非托管和池化引用得以释放
        /// </summary>
        ~UnpackContext()
        {
            Dispose();
        }
    }
}