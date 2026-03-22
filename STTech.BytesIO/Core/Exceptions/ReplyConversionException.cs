using System;

namespace STTech.BytesIO.Core.Exceptions
{
    /// <summary>
    /// Reply格式转换时发生的异常
    /// </summary>
    public class ReplyConversionException : BytesIOException
    {
        public ReplyConversionException(string message, Exception ex) : base(message, ex) { }
    }
}
