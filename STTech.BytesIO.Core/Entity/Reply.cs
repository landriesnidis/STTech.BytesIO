using System;

namespace STTech.BytesIO.Core.Entity
{
    /// <summary>
    /// 单次发送数据的远端响应
    /// </summary>
    /// <typeparam name="T">响应数据的类型</typeparam>
    public class Reply<T>
    {
        /// <summary>
        /// 触发客户端
        /// </summary>
        public IBytesClient Client { get; }

        /// <summary>
        /// 响应状态
        /// </summary>
        public ReplyStatus Status { get; }

        /// <summary>
        /// 内部错误
        /// </summary>
        public Exception Exception { get; }

        /// <summary>
        /// 响应数据
        /// </summary>
        public T Data { get; }

        /// <summary>
        /// 构造失败的响应
        /// </summary>
        /// <param name="client"></param>
        /// <param name="status"></param>
        /// <param name="exception"></param>
        public Reply(IBytesClient client, ReplyStatus status, Exception exception)
        {
            Client = client;
            Exception = exception;
        }

        /// <summary>
        /// 构造成功的响应
        /// </summary>
        /// <param name="client"></param>
        /// <param name="status"></param>
        /// <param name="data"></param>
        public Reply(IBytesClient client, ReplyStatus status, T data = default(T))
        {
            Client = client;
            Status = status;
            Data = data;
        }
    }

    /// <summary>
    /// 响应状态
    /// </summary>
    public enum ReplyStatus
    {
        /// <summary>
        /// 完成
        /// </summary>
        Completed,
        /// <summary>
        /// 超时
        /// </summary>
        Timeout,
        /// <summary>
        /// 错误
        /// </summary>
        Error,
    }
}
