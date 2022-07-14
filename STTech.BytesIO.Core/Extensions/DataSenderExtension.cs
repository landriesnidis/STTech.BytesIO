using STTech.BytesIO.Core.Entity;
using System.Threading.Tasks;

namespace STTech.BytesIO.Core
{
    /// <summary>
    /// 数据发送器接口扩展
    /// 提供数据发送器接口中指定的类型的发送方法
    /// </summary>
    public static class DataSenderExtension
    {
        ///// <summary>
        ///// 发送请求实体
        ///// </summary>
        ///// <typeparam name="TRequest"></typeparam>
        ///// <param name="client"></param>
        ///// <param name="request"></param>
        //public static void Send<TRequest>(this BytesClient client, TRequest request)
        //    where TRequest : IRequest
        //{
        //    client.Send(request.GetBytes());
        //}

        ///// <summary>
        ///// 异步发送请求实体
        ///// </summary>
        ///// <typeparam name="TRequest"></typeparam>
        ///// <param name="client"></param>
        ///// <param name="request"></param>
        ///// <returns></returns>
        //public static Task SendAsync<TRequest>(this BytesClient client, TRequest request)
        //    where TRequest : IRequest
        //{
        //    return Task.Run(() => client.Send(request));
        //}

        /// <summary>
        /// 异步数据
        /// 异步线程等待单次发送的响应结果
        /// </summary>
        /// <param name="data">数据</param>
        /// <param name="timeout">超时时间(ms)</param>
        /// <param name="matchHandler">收发数据匹配回调。
        /// 每次接收到的数据帧不一定就是对上一条发送数据的响应（如心跳包等），
        /// 所以需要根据协议编写对收发数据帧匹配的回调用以确定正确的响应数据。
        /// 包括不限于以下方式：
        /// 1.对发送数据的命令位于接受数据的命令位进行对比；
        /// 2.对发送数据的任务号及通信计数与接收数据的对应位进行对比；
        /// 3.过滤高频的主动推送数据（如：心跳包、状态更新、异常报告等）,取其后第一帧；
        /// </param>
        /// <returns>单次发送数据并等待远端响应的任务</returns>
        //public static Task<Reply<TRecv>> SendAsync<TSend, TRecv>(IDataSender<TSend, TRecv> sender, TSend data, int timeout, ReplyMatchHandler<TSend, TRecv> matchHandler = null)
        //{
        //    return Task.Run(() => sender.Send(data, timeout, matchHandler));
        //}

    }
}
