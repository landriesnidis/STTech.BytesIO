using STTech.BytesIO.Core;
using System;

namespace STTech.BytesIO.Modbus
{
    /// <summary>
    /// 监视器更新结果
    /// </summary>
    /// <typeparam name="T">响应类型</typeparam>
    public class MonitorUpdateResult<T> where T : Response
    {
        /// <summary>
        /// 更新是否成功
        /// </summary>
        public bool IsSuccess => ReplyStatus == ReplyStatus.Completed && Exception == null;

        /// <summary>
        /// 响应状态
        /// </summary>
        public ReplyStatus ReplyStatus => Reply.Status;

        /// <summary>
        /// 响应
        /// </summary>
        public Reply<T> Reply { get; internal set; }

        /// <summary>
        /// 异常信息
        /// </summary>
        public Exception Exception
        {
            get
            {
                if (Reply.Exception != null)
                {
                    return Reply.Exception;
                }

                if (ErrorCode != ModbusErrorCode.NoError)
                {
                    return new ModbusErrorCodeException(ErrorCode);
                }

                return null;
            }
        }

        /// <summary>
        /// 故障码
        /// </summary>
        public ModbusErrorCode ErrorCode { get; internal set; } = ModbusErrorCode.NoError;
    }
}