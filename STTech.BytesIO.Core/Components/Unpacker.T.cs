﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace STTech.BytesIO.Core.Component
{
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
            var resp = ResponseSerializeHandler(e.Data);

            // 反射执行回调
            OnDataParsed?.Invoke(this, new DataParsedEventArgs<T>(resp));
        }

        /// <summary>
        /// 将字节数组序列化称为Response对象的过程
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        protected virtual T ResponseSerializeHandler(byte[] bytes)
        {
            // 反射构造对象
            // 基于Response对象必须拥有一个带参的构造函数且参数类型为IEnumerable<byte>
            T resp = Activator.CreateInstance(typeof(T), [bytes]) as T;

            return resp;
        }

        /// <summary>
        /// 计算数据包长度的处理程序
        /// 输入当前缓存的数据
        /// 输出第一个数据包的长度，若暂无法判断数据包总长度，可返回0
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        protected override int CalculatePacketLength(byte[] bytes)
        {
            return _calculatePacketLengthHandler(bytes);
        }
    }

    /// <summary>
    /// 解析出结果(bytes)的事件参数
    /// </summary>
    public class DataParsedEventArgs : DataParsedEventArgs<byte[]>
    {
        public DataParsedEventArgs(byte[] data) : base(data) { }
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
