using System;

namespace STTech.BytesIO.Core.Exceptions
{
    /// <summary>
    /// 字节流通信库异常基类
    /// </summary>
    public class BytesIOException : Exception
    {
        /// <summary>
        /// 初始化 <see cref="BytesIOException"/> 类的新实例。
        /// </summary>
        /// <param name="errorMessage">错误描述</param>
        /// <param name="ex">内部异常</param>
        public BytesIOException(string errorMessage,Exception ex) : base(errorMessage, ex) { }
    }
}
