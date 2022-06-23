using System;

namespace STTech.BytesIO.Core.Entity
{
    public class ConnectionFailedEventArgs : EventArgs
    {
        /// <summary>
        /// 连接失败提示信息
        /// </summary>
        public string Message { get; }

        /// <summary>
        /// 内部异常
        /// </summary>
        public Exception Exception { get; }

        public ConnectionFailedEventArgs(string msg)
        {
            Message = msg;
        }

        public ConnectionFailedEventArgs(Exception exception) : this(exception.Message)
        {
            Exception = exception;
        }

        public ConnectionFailedEventArgs(string message, Exception exception) : this(message)
        {
            Exception = exception;
        }
    }
}
