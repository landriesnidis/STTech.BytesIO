using System;

namespace STTech.BytesIO.Core
{
    /// <summary>
    /// 发生异常事件参数
    /// </summary>
    public class ExceptionOccursEventArgs : EventArgs
    {
        /// <summary>
        /// 异常对象
        /// </summary>
        public Exception Exception { get; }

        /// <summary>
        /// 构造发生异常事件参数
        /// </summary>
        /// <param name="ex">异常对象</param>
        public ExceptionOccursEventArgs(Exception ex)
        {
            Exception = ex;
        }
    }
}
