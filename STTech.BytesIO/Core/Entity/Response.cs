using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text;

namespace STTech.BytesIO.Core
{
    /// <summary>
    /// 响应基类
    /// </summary>
    public abstract class Response
    {
        private STTech.BytesIO.Core.Component.UnpackContext _unpackContext;

        /// <summary>
        /// 原始数据
        /// </summary>
        protected byte[] OriginalData { get; private set; }

        /// <summary>
        /// 构造响应
        /// </summary>
        /// <param name="bytes">字节数组数据</param>
        protected Response(byte[] bytes)
        {
            OriginalData = bytes;
        }

        /// <summary>
        /// 构造响应
        /// </summary>
        /// <param name="context">解包上下文</param>
        protected Response(STTech.BytesIO.Core.Component.UnpackContext context)
        {
            _unpackContext = context;
        }

        /// <summary>
        /// 构造响应
        /// </summary>
        protected Response()
        {
        }

        /// <summary>
        /// 获取原始数据
        /// </summary>
        /// <returns></returns>
        public byte[] GetOriginalData()
        {
            if (OriginalData != null) return OriginalData;
            if (_unpackContext != null)
            {
                OriginalData = _unpackContext.Data.ToArray();
                return OriginalData;
            }
            return Array.Empty<byte>();
        }
    }

    /// <summary>
    /// 请求接口
    /// </summary>
    public interface IRequest
    {
        /// <summary>
        /// 获取字节数组
        /// </summary>
        /// <returns></returns>
        byte[] GetBytes();
    }
}
