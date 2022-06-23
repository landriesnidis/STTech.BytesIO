using STTech.BytesIO.Core.Entity;
using STTech.BytesIO.Core.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace STTech.BytesIO.Core
{
    // ===============================================================================
    // 
    //                                  发送数据
    // 
    // ===============================================================================

    public abstract partial class BytesClient
    {
        /// <summary>
        /// 发送数据
        /// </summary>
        /// <param name="data"></param>
        public abstract void Send(byte[] data);

        /// <summary>
        /// 异步发送数据
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public virtual Task SendAsync(byte[] data) => Task.Run(() => Send(data));

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
        public Reply<byte[]> Send(byte[] data, int timeout, ReplyMatchHandler<byte[], byte[]> matchHandler = null)
        {
            EventHandler<DataReceivedEventArgs> dataReceivedHandle = null;
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
                if (matchHandler != null && !matchHandler.Invoke(data, e.Data))
                {
                    return;
                }

                // 匹配到对应帧后移除当前监听,保存数据，标记已完成
                OnDataReceived -= dataReceivedHandle;
                buffer = e.Data;
                isCompleted = true;
                evt.Set();
            };

            // 监听数据接受事件 & 同步发送数据
            OnDataReceived += dataReceivedHandle;
            Send(data);

            // 创建Task等待被阻塞的信号事件
            // 通过条件一：信号事件的阻塞解除（接收到有效数据）
            // 通过条件二：超时
            Task.Run(() =>
            {
                // 阻塞等待信号
                evt.WaitOne();
            }).Wait(timeout, cancellationTokenSource.Token);

            // 再次主动移除监听（避免因超时结束但监听未注销）
            OnDataReceived -= dataReceivedHandle;

            // 取消所有任务并释放
            cancellationTokenSource.Cancel();

            return new Reply<byte[]>(this, isCompleted ? ReplyStatus.Completed : ReplyStatus.Timeout, buffer);
        }

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
        public Task<Reply<byte[]>> SendAsync(byte[] data, int timeout, ReplyMatchHandler<byte[], byte[]> matchHandler = null)
        {
            return Task.Run(() => Send(data, timeout, matchHandler));
        }
    }
}
