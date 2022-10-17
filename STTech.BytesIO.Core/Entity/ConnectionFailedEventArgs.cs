using System;

namespace STTech.BytesIO.Core
{
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

        public ConnectionFailedEventArgs(string msg)
        {
            message = msg;
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
