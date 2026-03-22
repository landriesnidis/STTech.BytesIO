using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace STTech.BytesIO.Core.Component
{
    /// <summary>
    /// 泛型解包器
    /// 继承自基础解包器，使用泛型解包器可以直接获取到强类型对象
    /// 支持通过构造函数注入解析委托体验纯粹的解包组合 API
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class Unpacker<T> : Unpacker
    {
        /// <summary>
        /// 当解析出强类型结果时发生
        /// </summary>
        public new event EventHandler<DataParsedEventArgs<T>> OnDataParsed;

        /// <summary>
        /// 所属的客户端
        /// </summary>
        public BytesClient Client { get; internal set; }

        /// <summary>
        /// 提供给本解包器的包长度计算器
        /// </summary>
        public Func<ReadOnlySequence<byte>, int> LengthCalculator { get; set; }

        /// <summary>
        /// 提供给本解包器的数据转换器
        /// </summary>
        public Func<UnpackContext, T> DataParser { get; set; }

        /// <summary>
        /// 构造解包器（需由派生类提供 CalculatePacketLength 和 ResponseSerializeHandler）
        /// </summary>
        public Unpacker(BytesClient client = null)
        {
            base.OnDataParsed += Unpacker_OnDataParsed;
            Client = client;
        }

        /// <summary>
        /// 采用函数式构建解包器，彻底支持零拷贝和业务上下文传递
        /// </summary>
        /// <param name="lengthCalculator">长度计算</param>
        /// <param name="dataParser">数据解析</param>
        /// <param name="client">关联的 BytesClient</param>
        public Unpacker(Func<ReadOnlySequence<byte>, int> lengthCalculator, Func<UnpackContext, T> dataParser, BytesClient client = null) : this(client)
        {
            LengthCalculator = lengthCalculator;
            DataParser = dataParser;
        }

        private void Unpacker_OnDataParsed(object sender, DataParsedEventArgs e)
        {
            var resp = ResponseSerializeHandler(e.Data);
            OnDataParsed?.Invoke(this, new DataParsedEventArgs<T>(resp));
        }

        protected override int CalculatePacketLength(ReadOnlySequence<byte> buffer)
        {
            if (LengthCalculator != null) return LengthCalculator(buffer);
            throw new NotSupportedException("Unpacker<T> requires setting LengthCalculator or overriding CalculatePacketLength.");
        }

        /// <summary>
        /// 将数据序列化成为 Response 对象的过程
        /// </summary>
        protected virtual T ResponseSerializeHandler(UnpackContext context)
        {
            if (DataParser != null) return DataParser(context);
            throw new NotSupportedException("Unpacker<T> requires setting DataParser or overriding ResponseSerializeHandler.");
        }
    }

    /// <summary>
    /// 解析出结果的抽象事件参数
    /// </summary>
    public class DataParsedEventArgs : DataParsedEventArgs<UnpackContext>
    {
        public DataParsedEventArgs(UnpackContext data) : base(data) { }
    }

    /// <summary>
    /// 解析出强类型结果的事件参数
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
