using STTech.BytesIO.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static STTech.BytesIO.Core.Component.Unpacker;

namespace STTech.BytesIO.Core
{
    /// <summary>
    /// 连接结果
    /// </summary>
    public class ConnectResult
    {
        /// <summary>
        /// 操作是否成功
        /// </summary>
        public bool IsSuccess => ErrorCode == ConnectErrorCode.None;

        /// <summary>
        /// 错误码
        /// </summary>
        public ConnectErrorCode ErrorCode { get; }

        /// <summary>
        /// 异常信息
        /// </summary>
        public Exception Exception { get; }

        public ConnectResult()
        {
            ErrorCode = ConnectErrorCode.None;
        }

        public ConnectResult(ConnectErrorCode errorCode, Exception exception = null)
        {
            ErrorCode = errorCode;
            Exception = exception;
        }
    }

    /// <summary>
    /// 连接操作错误码
    /// </summary>
    public enum ConnectErrorCode
    {
        /// <summary>
        /// 无错误
        /// </summary>
        None,

        /// <summary>
        /// 已存在连接
        /// </summary>
        IsConnected,

        /// <summary>
        /// 连接错误
        /// </summary>
        Error,

        /// <summary>
        /// 连接参数错误
        /// </summary>
        ConnectionParameterError,

        /// <summary>
        /// 连接超时
        /// </summary>
        Timeout,
    }

    /// <summary>
    /// 连接参数
    /// </summary>
    public class ConnectArgument
    {
        /// <summary>
        /// 超时时长
        /// </summary>
        public int Timeout { get; set; } = -1;
    }

    /// <summary>
    /// 断开连接操作结果
    /// </summary>
    public class DisconnectResult
    {
        /// <summary>
        /// 操作是否成功
        /// </summary>
        public bool IsSuccess => ErrorCode == DisconnectErrorCode.None;

        /// <summary>
        /// 错误码
        /// </summary>
        public DisconnectErrorCode ErrorCode { get; }

        /// <summary>
        /// 异常信息
        /// </summary>
        public Exception Exception { get; }

        public DisconnectResult()
        {
            ErrorCode = DisconnectErrorCode.None;
        }

        public DisconnectResult(DisconnectErrorCode errorCode, Exception exception = null)
        {
            ErrorCode = errorCode;
            Exception = exception;
        }
    }

    /// <summary>
    /// 断开连接操作参数
    /// </summary>
    public class DisconnectArgument
    {
        /// <summary>
        /// 原因码
        /// </summary>
        public DisconnectionReasonCode ReasonCode { get; }

        /// <summary>
        /// 异常信息
        /// </summary>
        public Exception Exception { get; }

        /// <summary>
        /// 构造主动断开连接操作参数
        /// </summary>
        public DisconnectArgument()
        {
            ReasonCode = DisconnectionReasonCode.Active;
        }

        /// <summary>
        /// 构造断开连接操作参数
        /// </summary>
        /// <param name="reasonCode">断开连接的原因码</param>
        /// <param name="exception">异常信息</param>
        public DisconnectArgument(DisconnectionReasonCode reasonCode, Exception exception = null)
        {
            ReasonCode = reasonCode;
            Exception = exception;
        }

    }

    /// <summary>
    /// 断开连接操作错误码
    /// </summary>
    public enum DisconnectErrorCode
    {
        /// <summary>
        /// 无错误
        /// </summary>
        None,

        /// <summary>
        /// 当前无连接
        /// </summary>
        NoConnection,

        /// <summary>
        /// 断开连接错误
        /// </summary>
        Error,
    }
}
