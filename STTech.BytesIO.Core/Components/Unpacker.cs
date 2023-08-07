using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using STTech.BytesIO.Core;

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
        /// 缓存数据
        /// </summary>
        private IEnumerable<byte> _UnprocessedDataCache;

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
        protected abstract int CalculatePacketLength(IEnumerable<byte> bytes);

        /// <summary>
        /// 起始标记
        /// </summary>
        public IEnumerable<byte> StartMark { get; set; }

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
                // 是否有缓存数据
                if (_UnprocessedDataCache == null)
                {
                    // 若无缓存数据则当前数据即为缓存数据
                    _UnprocessedDataCache = bytes;
                }
                else
                {
                    // 是否启用断包拼包的超时限制
                    // 有收到断包的头部
                    // 收到断包头部的时间 + 超时限时 < 当前时间
                    if (InterruptFrameTimeoutValue > 0 &&
                        _interruptFrameReceivedTime != null &&
                        _interruptFrameReceivedTime.Value.AddMilliseconds(InterruptFrameTimeoutValue) < DateTime.Now)
                    {
                        // 直接替换缓存数据（丢弃断包头部）
                        _UnprocessedDataCache = bytes;
                        // 清空计时
                        _interruptFrameReceivedTime = null;
                    }
                    else
                    {
                        // 若有缓存数据则将原数据与新数据合并
                        _UnprocessedDataCache = _UnprocessedDataCache.Merge(bytes);
                    }
                }

                while (true)
                {
                    // 当前缓存数据长度
                    var cacheLen = _UnprocessedDataCache.Count();

                    // 判断是否设置了固定的起始标志位
                    if (StartMark != null && StartMark.Any())
                    {
                        // 判断已收到数据的长度是否足够起始标记的长度
                        if (cacheLen < StartMark.Count())
                        {
                            // 结束本次处理，继续接收数据
                            return;
                        }

                        // 判断头部是否一致
                        if (!_UnprocessedDataCache.StartWith(StartMark))
                        {
                            // 起始位不匹配时，查看缓存区中是否能找到起始位
                            var index = _UnprocessedDataCache.IndexOf(StartMark);
                            if (index == -1)
                            {
                                // 未找到起始位则只保留起始标识长度-1位的数据
                                ErrorOccurHandler?.Invoke(ErrorCode.StartMarkNotMatch);
                                _UnprocessedDataCache = _UnprocessedDataCache.Skip(_UnprocessedDataCache.Count() - StartMark.Count() + 1).ToArray();
                                return;
                            }
                            else
                            {
                                // 若找到起始位则认为缓冲区前段混入脏数据，跳过这些字节
                                _UnprocessedDataCache = _UnprocessedDataCache.Skip(index);
                            }
                        }
                    }

                    // 通过回调提供的方法计算该包的长度(根据具体协议)
                    int packetLen = CalculatePacketLength(_UnprocessedDataCache);

                    // 当返回的数据包长度计算结果小于等于0时，标识当前缓存中的数据无法判断出数据包的长度，则结束本次处理，继续接收数据
                    // 当返回的数据包长度计算结果小于缓存数据长度时，则结束本次处理，继续接收数据
                    if (packetLen <= 0 || packetLen > cacheLen)
                    {
                        // 是否启用断包拼包的超时限制
                        if (InterruptFrameTimeoutValue > 0)
                        {
                            _interruptFrameReceivedTime = DateTime.Now;
                        }
                        return;
                    }

                    // 将拆粘包的结果通过回调返回
                    OnDataParsed.Invoke(this, new DataParsedEventArgs(_UnprocessedDataCache.Take(packetLen)));

                    // 如果缓存还有粘包，则将剩余数据保存至缓存中
                    if (packetLen < cacheLen)
                    {
                        _UnprocessedDataCache = _UnprocessedDataCache.Skip(packetLen);
                    }
                    else
                    {
                        _UnprocessedDataCache = null;
                        return;
                    }
                }
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
        }
    }


}
