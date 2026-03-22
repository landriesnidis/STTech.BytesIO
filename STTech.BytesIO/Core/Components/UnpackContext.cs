using System;
using System.Buffers;
using System.Collections.Generic;

namespace STTech.BytesIO.Core.Component
{
    /// <summary>
    /// 解包数据上下文
    /// 封装了网络包解析后的只读内存序列以及关联的上下文数据
    /// </summary>
    public class UnpackContext
    {
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

        public UnpackContext(ReadOnlySequence<byte> data)
        {
            Data = data;
        }
    }
}
