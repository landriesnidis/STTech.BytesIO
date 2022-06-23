using STTech.BytesIO.Core.Entity;
using STTech.BytesIO.Core.Component;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace STTech.BytesIO.Core
{
    /// <summary>
    /// 解包器扩展
    /// </summary>
    public static class UnpackerExtension
    {
        /// <summary>
        /// 创建解包器
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="client"></param>
        /// <param name="calculatePacketLengthHandler"></param>
        /// <returns></returns>
        public static Unpacker<T> CreateUnpacker<T>(this BytesClient client, Func<IEnumerable<byte>, int> calculatePacketLengthHandler) where T : Response
        {
            var unpacker = new Unpacker<T>(client, calculatePacketLengthHandler);

            client.OnDataReceived += (s, e) =>
            {
                unpacker.Input(e.Data);
            };

            return unpacker;
        }

        /// <summary>
        /// 发送数据
        /// 阻塞等待单次发送的响应结果
        /// <code>
        /// ctor:
        /// this.Unpacker = this.CreateUnpacker&lt;T&gt;(()=>{});
        /// </code>
        /// </summary>
        /// <param name="unpackerSupport"></param>
        /// <param name="request">数据</param>
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
        /// <exception cref="ArgumentNullException"></exception>
        public static Reply<TRecv> Send<TSend, TRecv>(this IUnpackerSupport<TRecv> unpackerSupport, TSend request, int timeout, ReplyMatchHandler<TSend, TRecv> matchHandler = null) where TSend : IRequest where TRecv : Response
        {
            if (unpackerSupport is null)
            {
                throw new ArgumentNullException(nameof(unpackerSupport));
            }

            //if (!(unpackerSupport is BytesClient))
            //{
            //    throw new Exception("实现IUnpackerSupport<TRecv>的对象必须是基于BytesClient的子类型");
            //}

            if (unpackerSupport.Unpacker is null)
            {
                throw new ArgumentNullException(nameof(unpackerSupport.Unpacker));
            }

            EventHandler<DataParsedEventArgs<TRecv>> dataParsedHandle = null;
            TRecv buffer = null;

            // 客户端
            var client = unpackerSupport.Unpacker.Client;

            // 待发送数据
            var data = request.GetBytes();

            // 是否接收到有效数据
            bool isCompleted = false;

            // 信号事件
            AutoResetEvent evt = new AutoResetEvent(false);

            // 创建两个支持取消的任务：超时计时任务、被当做唤醒触发的空任务
            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

            // 接收到数据的回调
            dataParsedHandle = (sender, e) =>
            {
                // 判断当前帧是否符合，不符合则跳过
                if (matchHandler != null && !matchHandler.Invoke(request, e.Data))
                {
                    return;
                }

                // 匹配到对应帧后移除当前监听,保存数据，标记已完成
                unpackerSupport.Unpacker.OnDataParsed += dataParsedHandle;
                buffer = e.Data;
                isCompleted = true;
                evt.Set();
            };

            // 监听数据接受事件 & 同步发送数据
            unpackerSupport.Unpacker.OnDataParsed += dataParsedHandle;
            client.Send(data);

            // 创建Task等待被阻塞的信号事件
            // 通过条件一：信号事件的阻塞解除（接收到有效数据）
            // 通过条件二：超时
            Task.Run(() =>
            {
                // 阻塞等待信号
                evt.WaitOne();
            }).Wait(timeout, cancellationTokenSource.Token);

            // 再次主动移除监听（避免因超时结束但监听未注销）
            unpackerSupport.Unpacker.OnDataParsed -= dataParsedHandle;

            // 取消所有任务并释放
            cancellationTokenSource.Cancel();

            return new Reply<TRecv>(client, isCompleted ? ReplyStatus.Completed : ReplyStatus.Timeout, buffer);
        }
    }
}
