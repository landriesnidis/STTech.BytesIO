using System;
using System.Collections.Generic;
using System.Text;

namespace STTech.BytesIO.Core.Exceptions
{
    /// <summary>
    /// 执行回调方法时发生的异常
    /// </summary>
    public class PerformCallbackException : BytesIOException
    {
        private const string errorMessage = "An exception occurred while the callback method was executing.";
        /// <summary>
        /// 初始化 <see cref="PerformCallbackException"/> 类的新实例。
        /// </summary>
        /// <param name="ex">内部异常</param>
        public PerformCallbackException(Exception ex) : base(errorMessage, ex) { }
    }
}
