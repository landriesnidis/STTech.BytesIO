using System;

namespace STTech.BytesIO.Core.Exceptions
{
    /// <summary>
    /// Reply格式转换时发生的异常
    /// </summary>
    public class ReplyConversionException : BytesIOException
    {
        /// <summary>
        /// 初始化 <see cref="ReplyConversionException"/> 类的新实例。
        /// </summary>
        /// <param name="message">错误消息</param>
        /// <param name="ex">内部异常</param>
        public ReplyConversionException(string message, Exception ex) : base(message, ex) { }
    }
}
