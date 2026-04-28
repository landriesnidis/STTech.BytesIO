using System;
using System.Collections.Generic;

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
        /// 缓存数据 (初始化一次，避免频繁 new List)
        /// </summary>
        private readonly List<byte> _UnprocessedDataCache = new List<byte>();

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
        /// 输出第一个数据包的长度，若暂无法判断数据包总长度，可返回0
        /// </summary>
        protected abstract int CalculatePacketLength(byte[] bytes);

        /// <summary>
        /// 起始标记
        /// </summary>
        public byte[] StartMark { get; set; }

        /// <summary>
        /// 中断帧(断包)拼接的超时时长
        /// 单位毫秒
        /// 值为0时不启用超时检查
        /// </summary>
        public int InterruptFrameTimeoutValue { get; set; } = 0;

        /// <summary>
        /// 中断帧(断包头部)的接收时间
        /// </summary>
        private DateTime? _interruptFrameReceivedTime = null;

        /// <summary>
        /// 构造解包器
        /// </summary>
        protected Unpacker()
        {

        }

        /// <summary>
        /// 输入收到的数据
        /// </summary>
        /// <param name="bytes">接收到的数据</param>
        public void Input(IEnumerable<byte> bytes)
        {
            lock (asyncDataCacheLocker)
            {
                // 处理断包拼包的超时限制
                if (InterruptFrameTimeoutValue > 0 &&
                    _interruptFrameReceivedTime != null &&
                    _interruptFrameReceivedTime.Value.AddMilliseconds(InterruptFrameTimeoutValue) < DateTime.Now)
                {
                    // 超时：清空之前的缓存（丢弃断包头部）
                    _UnprocessedDataCache.Clear();
                    _interruptFrameReceivedTime = null;
                }

                // 高效追加数据，替代原有的 .ToList() 和 .Merge() 扩展方法
                _UnprocessedDataCache.AddRange(bytes);

                while (_UnprocessedDataCache.Count > 0)
                {
                    // 当前缓存数据长度 (使用属性而不是 LINQ 的 .Count())
                    var cacheLen = _UnprocessedDataCache.Count;

                    // 判断是否设置了固定的起始标志位
                    if (StartMark != null && StartMark.Length > 0)
                    {
                        // 判断已收到数据的长度是否足够起始标记的长度
                        if (cacheLen < StartMark.Length)
                        {
                            return; // 结束本次处理，继续接收数据
                        }

                        // 查找起始标记的索引
                        int startMarkIndex = FindStartMarkIndex();

                        if (startMarkIndex == -1)
                        {
                            // 未找到起始位则只保留起始标识长度-1位的数据
                            ErrorOccurHandler?.Invoke(ErrorCode.StartMarkNotMatch);
                            int keepLength = StartMark.Length - 1;
                            int removeLength = cacheLen - keepLength;
                            if (removeLength > 0)
                            {
                                _UnprocessedDataCache.RemoveRange(0, removeLength);
                            }
                            return;
                        }
                        else if (startMarkIndex > 0)
                        {
                            // 若找到起始位但不在开头，认为缓冲区前段混入脏数据，高效移除脏数据
                            _UnprocessedDataCache.RemoveRange(0, startMarkIndex);
                            cacheLen = _UnprocessedDataCache.Count;
                        }
                    }

                    // 提取数组供子类计算长度（必须满足 abstract 接口定义）
                    byte[] currentCacheArray = _UnprocessedDataCache.ToArray();
                    int packetLen = CalculatePacketLength(currentCacheArray);

                    // 当返回的数据包长度计算结果小于等于0，或大于缓存长度时
                    if (packetLen <= 0 || packetLen > cacheLen)
                    {
                        // 记录断包接收时间
                        if (InterruptFrameTimeoutValue > 0)
                        {
                            _interruptFrameReceivedTime = DateTime.Now;
                        }
                        return;
                    }

                    // 取出解包数据 (使用 CopyTo 替代 LINQ 的 Take(..).ToArray())
                    byte[] data = new byte[packetLen];
                    _UnprocessedDataCache.CopyTo(0, data, 0, packetLen);

                    // 移除已处理的数据，保留粘包数据 (使用 RemoveRange 替代 Skip(..).ToList())
                    _UnprocessedDataCache.RemoveRange(0, packetLen);

                    // 成功解析出一个包后，清除超时计时器
                    _interruptFrameReceivedTime = null;

                    // 将拆粘包的结果通过回调返回 (加入 null 检查防止未订阅异常)
                    OnDataParsed?.Invoke(this, new DataParsedEventArgs(data));
                }
            }
        }

        /// <summary>
        /// 清空缓存数据
        /// </summary>
        public void ClearCache()
        {
            lock (asyncDataCacheLocker) // 加入锁确保多线程安全
            {
                _UnprocessedDataCache.Clear();
                _interruptFrameReceivedTime = null;
            }
        }

        /// <summary>
        /// 高效查找起始标记在缓存中的位置，避免分配新内存
        /// </summary>
        /// <returns>匹配的起始索引，未找到返回 -1</returns>
        private int FindStartMarkIndex()
        {
            int maxSearchIndex = _UnprocessedDataCache.Count - StartMark.Length;
            for (int i = 0; i <= maxSearchIndex; i++)
            {
                bool match = true;
                for (int j = 0; j < StartMark.Length; j++)
                {
                    if (_UnprocessedDataCache[i + j] != StartMark[j])
                    {
                        match = false;
                        break;
                    }
                }
                if (match) return i;
            }
            return -1;
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
        }
    }
}