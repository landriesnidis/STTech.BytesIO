using System;
using System.Collections.Generic;
using System.Text;

namespace STTech.BytesIO.Core
{
    public abstract class Response
    {
        /// <summary>
        /// 原始数据
        /// </summary>
        protected IEnumerable<byte> OriginalData { get; }

        protected Response(IEnumerable<byte> bytes)
        {
            OriginalData = bytes;
        }

        /// <summary>
        /// 获取原始数据
        /// </summary>
        /// <returns></returns>
        public IEnumerable<byte> GetOriginalData() => OriginalData;
    }

    public interface IRequest
    {
        /// <summary>
        /// 获取字节数组
        /// </summary>
        /// <returns></returns>
        byte[] GetBytes();
    }
}
