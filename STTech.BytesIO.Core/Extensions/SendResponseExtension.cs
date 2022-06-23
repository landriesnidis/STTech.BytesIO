using STTech.BytesIO.Core.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace STTech.BytesIO.Core.Extensions
{


    /// <summary>
    /// 数据发送扩展
    /// </summary>
    public static class SendResponseExtension
    {
        /// <summary>
        /// 发送数据
        /// 阻塞等待单次发送的响应结果
        /// 2022年4月8日13:28:22 废弃该方法，原因：相比新的Send方法多消耗了一个Task
        /// </summary>
        /// <param name="client">通信客户端</param>
        /// <param name="bytes">数据</param>
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
        //public static SendResponse<byte[]> Send(this IBytesClient client, byte[] bytes, int timeout = 100, SendReceiveMatchHandler<byte[], byte[]> matchHandler = null)
        //{
        //    DataReceivedHandler dataReceivedHandle = null;
        //    byte[] buffer = null;

        //    // 创建两个支持取消的任务：超时计时任务、被当做唤醒触发的空任务
        //    CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        //    Task taskTimeout = Task.Delay(timeout, cancellationTokenSource.Token);
        //    Task taskTrigger = new Task(() => { }, cancellationTokenSource.Token);

        //    // 接收到数据的回调
        //    dataReceivedHandle = (sender, e) =>
        //    {
        //        // 判断当前帧是否符合，不符合则跳过
        //        if (matchHandler != null && !matchHandler.Invoke(bytes, e.Data))
        //        {
        //            return;
        //        }

        //        // 匹配到对应帧后移除当前监听,保存数据，标记已完成
        //        client.OnDataReceived -= dataReceivedHandle;
        //        buffer = e.Data;
        //        taskTrigger.Start();
        //    };

        //    // 监听数据接受事件 & 同步发送数据
        //    client.OnDataReceived += dataReceivedHandle;
        //    client.Send(bytes);

        //    // 等待超时与数据接收任一执行结束
        //    Task.WaitAny(taskTimeout, taskTrigger);

        //    // 是否接收到有效数据
        //    bool isCompleted = !taskTrigger.IsCanceled;

        //    // 再次主动移除监听（避免因超时结束但监听未注销）
        //    client.OnDataReceived -= dataReceivedHandle;

        //    // 取消所有任务并释放
        //    cancellationTokenSource.Cancel();
        //    taskTimeout.Dispose();
        //    taskTrigger.Dispose();

        //    return new SendResponse<byte[]>(client, isCompleted ? ResponseStatus.Completed : ResponseStatus.Timeout, buffer);
        //}

        /// <summary>
        /// 异步数据
        /// 异步线程等待单次发送的响应结果
        /// </summary>
        /// <param name="client">通信客户端</param>
        /// <param name="bytes">数据</param>
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
        public static Task<Reply<byte[]>> SendAsync(this IBytesClient client, byte[] bytes, int timeout = 100, ReplyMatchHandler<byte[], byte[]> matchHandler = null)
        {
            return Task.Run(() => client.Send(bytes, timeout, matchHandler));
        }

        /// <summary>
        /// 发送数据
        /// 阻塞等待单次发送的响应结果
        /// </summary>
        /// <param name="client">通信客户端</param>
        /// <param name="bytes">数据</param>
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
        public static Reply<byte[]> Send(this BytesClient client, byte[] bytes, int timeout = 100, ReplyMatchHandler<byte[], byte[]> matchHandler = null)
        {
            DataReceivedHandler dataReceivedHandle = null;
            byte[] buffer = null;

            // 是否接收到有效数据
            bool isCompleted = false;

            // 信号事件
            AutoResetEvent evt = new AutoResetEvent(false);

            // 创建两个支持取消的任务：超时计时任务、被当做唤醒触发的空任务
            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

            // 接收到数据的回调
            dataReceivedHandle = (sender, e) =>
            {
                // 判断当前帧是否符合，不符合则跳过
                if (matchHandler != null && !matchHandler.Invoke(bytes, e.Data))
                {
                    return;
                }

                // 匹配到对应帧后移除当前监听,保存数据，标记已完成
                client.OnDataReceived -= dataReceivedHandle;
                buffer = e.Data;
                isCompleted = true;
                evt.Set();
            };

            // 监听数据接受事件 & 同步发送数据
            client.OnDataReceived += dataReceivedHandle;
            client.Send(bytes);

            // 创建Task等待被阻塞的信号事件
            // 通过条件一：信号事件的阻塞解除（接收到有效数据）
            // 通过条件二：超时
            Task.Run(() =>
            {
                // 阻塞等待信号
                evt.WaitOne();
            }).Wait(timeout, cancellationTokenSource.Token);

            // 再次主动移除监听（避免因超时结束但监听未注销）
            client.OnDataReceived -= dataReceivedHandle;

            // 取消所有任务并释放
            cancellationTokenSource.Cancel();

            return new Reply<byte[]>(client, isCompleted ? ReplyStatus.Completed : ReplyStatus.Timeout, buffer);
        }
    }
}
