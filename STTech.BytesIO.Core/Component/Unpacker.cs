using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using STTech.BytesIO.Core.Entity;

namespace STTech.BytesIO.Core.Component
{
    /// <summary>
    /// 解析出结果的事件委托
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    // public delegate void DataParsedHandler(object sender, DataParsedEventArgs e);

    /// <summary>
    /// 解析出结果的事件委托
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    // public delegate void DataParsedHandler<T>(object sender, DataParsedEventArgs<T> e);

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
                            // 起始位不匹配时 一定会清空缓存
                            ErrorOccurHandler?.Invoke(ErrorCode.StartMarkNotMatch);
                            _UnprocessedDataCache = null;
                            return;
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

    /// <summary>
    /// 泛型解包器
    /// 继承字节数组的解包器，使用泛型解包器可以直接获取到泛型对象
    /// 注意：基于Response对象必须拥有一个带参的构造函数且参数类型为IEnumerable&lt;byte&gt;
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class Unpacker<T> : Unpacker where T : Response
    {
        /// <summary>
        /// 当解析出结果时发生
        /// </summary>
        public new event EventHandler<DataParsedEventArgs<T>> OnDataParsed;

        /// <summary>
        /// 所属的客户端
        /// </summary>
        public BytesClient Client { get; internal set; }

        /// <summary>
        /// 计算包长度回调方法
        /// </summary>
        private readonly Func<IEnumerable<byte>, int> _calculatePacketLengthHandler;

        /// <summary>
        /// 构造解包器
        /// </summary>
        public Unpacker(BytesClient client, Func<IEnumerable<byte>, int> calculatePacketLengthHandler = null)
        {
            base.OnDataParsed += Unpacker_OnDataParsed;
            Client = client;
            _calculatePacketLengthHandler = calculatePacketLengthHandler;
        }

        private void Unpacker_OnDataParsed(object sender, DataParsedEventArgs e)
        {
            // 反射构造对象
            // 基于Response对象必须拥有一个带参的构造函数且参数类型为IEnumerable<byte>
            T data = Activator.CreateInstance(typeof(T), new object[] { e.Data }) as T;

            // 反射执行回调
            OnDataParsed?.Invoke(this, new DataParsedEventArgs<T>(data));
        }

        /// <summary>
        /// 计算数据包长度的处理程序
        /// 输入当前缓存的数据
        /// 输出第一个数据包的长度，若暂无法判断数据包总长度，可返回0
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        protected override int CalculatePacketLength(IEnumerable<byte> bytes)
        {
            return _calculatePacketLengthHandler(bytes);
        }
    }


    /// <summary>
    /// 解析出结果(bytes)的事件参数
    /// </summary>
    public class DataParsedEventArgs : DataParsedEventArgs<IEnumerable<byte>>
    {
        public DataParsedEventArgs(IEnumerable<byte> data) : base(data) { }
    }

    /// <summary>
    /// 解析出结果的事件参数
    /// </summary>
    public class DataParsedEventArgs<T> : EventArgs
    {
        /// <summary>
        /// 解析出的数据
        /// </summary>
        public T Data { get; }

        public DataParsedEventArgs(T data)
        {
            Data = data;
        }
    }
}
