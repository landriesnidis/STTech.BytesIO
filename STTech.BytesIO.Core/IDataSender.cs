using STTech.BytesIO.Core.Component;
using STTech.BytesIO.Core.Entity;
using System.Threading.Tasks;

namespace STTech.BytesIO.Core
{
    /// <summary>
    /// 回复(数据收发)匹配委托
    /// </summary>
    /// <typeparam name="TSend">发送数据类型</typeparam>
    /// <typeparam name="TRecv">接收数据类型</typeparam>
    /// <param name="send">发送数据</param>
    /// <param name="recv">接收数据</param>
    /// <returns></returns>
    public delegate bool ReplyMatchHandler<TSend, TRecv>(TSend send, TRecv recv);

    /// <summary>
    /// 数据发送器接口
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IDataSender<T>
    {
        /// <summary>
        /// 发送数据
        /// </summary>
        /// <param name="data"></param>
        // public void Send(T data);
    }

    /// <summary>
    /// 支持等待响应结果的数据发送器接口
    /// </summary>
    /// <typeparam name="TSend">发送数据类型</typeparam>
    /// <typeparam name="TRecv">接收数据类型</typeparam>
    public interface IDataSender<TSend, TRecv>
    {
        /// <summary>
        /// 发送数据
        /// 阻塞等待单次发送的响应结果
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
        /// <returns>单次发送数据的远端响应</returns>
        // Reply<TRecv> Send(TSend data, int timeout, ReplyMatchHandler<TSend, TRecv> matchHandler = null);
    }
}
