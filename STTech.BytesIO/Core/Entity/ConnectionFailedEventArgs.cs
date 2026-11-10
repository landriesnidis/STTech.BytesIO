using System;

namespace STTech.BytesIO.Core
{
    /// <summary>
    /// 客户端连接失败事件参数
    /// </summary>
    public class ConnectionFailedEventArgs : EventArgs
    {
        private string message;

        /// <summary>
        /// 连接失败提示信息
        /// </summary>
        public string Message => message ?? Exception?.Message;

        /// <summary>
        /// 内部异常
        /// </summary>
        public Exception Exception { get; }

        /// <summary>
        /// 连接错误码
        /// </summary>
        public ConnectErrorCode ErrorCode { get; }

        /// <summary>
        /// 尝试建立物理链接直到失败所耗费的时长
        /// </summary>
        public TimeSpan CostTime { get; set; }

        /// <summary>
        /// 触发连接时传入的使用者附加内容
        /// </summary>
        public object State { get; set; }

        /// <summary>
        /// 初始化 <see cref="ConnectionFailedEventArgs"/> 类的新实例。
        /// </summary>
        /// <param name="errorCode">连接错误码</param>
        /// <param name="exception">异常对象</param>
        public ConnectionFailedEventArgs(ConnectErrorCode errorCode, Exception exception = null)
        {
            ErrorCode = errorCode;
            switch (errorCode)
            {
                case ConnectErrorCode.None:
                    break;
                case ConnectErrorCode.IsConnected:
                    Exception = exception ?? new InvalidOperationException("The client is already connected.");
                    break;
                case ConnectErrorCode.Error:
                    message = "An error occurred while connecting.";
                    Exception = exception;
                    break;
                case ConnectErrorCode.ConnectionParameterError:
                    message = "Connection parameter error.";
                    Exception = exception;
                    break;
                case ConnectErrorCode.Timeout:
                    Exception = exception ?? new TimeoutException("Connection timeout.");
                    break;
            }
        }

        /// <summary>
        /// 初始化 <see cref="ConnectionFailedEventArgs"/> 类的新实例。
        /// </summary>
        /// <param name="msg">错误消息</param>
        public ConnectionFailedEventArgs(string msg)
        {
            message = msg;
        }

        /// <summary>
        /// 初始化 <see cref="ConnectionFailedEventArgs"/> 类的新实例。
        /// </summary>
        /// <param name="exception">异常对象</param>
        public ConnectionFailedEventArgs(Exception exception) : this(exception.Message)
        {
            Exception = exception;
        }

        /// <summary>
        /// 初始化 <see cref="ConnectionFailedEventArgs"/> 类的新实例。
        /// </summary>
        /// <param name="message">错误消息</param>
        /// <param name="exception">异常对象</param>
        public ConnectionFailedEventArgs(string message, Exception exception) : this(message)
        {
            Exception = exception;
        }
    }
}
