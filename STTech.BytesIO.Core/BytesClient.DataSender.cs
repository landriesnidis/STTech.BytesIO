using STTech.BytesIO.Core;
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
        private readonly object sendingLocker = new object();

        /// <summary>
        /// 默认的发送选项
        /// </summary>
        public SendOptions DefaultSendOptions { get; } = new SendOptions();

        /// <summary>
        /// 发送数据
        /// </summary>
        /// <param name="data"></param>
        /// <param name="options">发送选项</param>
        public void Send(byte[] data, SendOptions options = null)
        {
            lock (sendingLocker)
            {
                options ??= DefaultSendOptions;

                // 执行发送操作
                SendHandler(data);

                // 延时
                Task.Delay(options.PauseTime).Wait();
            }
        }

        /// <summary>
        /// 发送数据的实现过程
        /// </summary>
        protected abstract void SendHandler(byte[] data);

        /// <summary>
        /// 异步发送数据
        /// </summary>
        /// <param name="data"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public Task SendAsync(byte[] data, SendOptions options = null) => Task.Run(() => Send(data, options));

        /// <summary>
        /// 发送请求实体
        /// </summary>
        /// <typeparam name="TRequest"></typeparam>
        /// <param name="request"></param>
        /// <param name="options"></param>
        public void Send<TRequest>(TRequest request, SendOptions options = null)
            where TRequest : IRequest
        {
            this.Send(request.GetBytes(), options);
        }

        /// <summary>
        /// 异步发送请求实体
        /// </summary>
        /// <typeparam name="TRequest"></typeparam>
        /// <param name="request"></param>
        /// <returns></returns>
        public Task SendAsync<TRequest>(TRequest request, SendOptions options = null)
            where TRequest : IRequest
        {
            return Task.Run(() => this.Send(request, options));
        }


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
        /// <param name="options"></param>
        /// <returns>单次发送数据的远端响应</returns>
        public ReplyBytes Send(byte[] data, int timeout, ReplyMatchHandler<byte[], byte[]> matchHandler = null, SendOptions options = null)
        {
            EventHandler<DataReceivedEventArgs> dataReceivedHandle = null;
            byte[] buffer = null;

            // 是否接收到有效数据
            bool isCompleted = false;

            // 信号事件
            AutoResetEvent evt = new(false);

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
            Send(data, options);

            // 创建Task等待被阻塞的信号事件
            // 通过条件一：信号事件的阻塞解除（接收到有效数据）
            // 通过条件二：超时
            evt.WaitOne(timeout);

            // 再次主动移除监听（避免因超时结束但监听未注销）
            OnDataReceived -= dataReceivedHandle;

            if (isCompleted)
            {
                return new ReplyBytes(this, buffer);
            }
            else
            {
                return new ReplyBytes(this, ReplyStatus.Timeout, null);
            }
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
        public Task<ReplyBytes> SendAsync(byte[] data, int timeout, ReplyMatchHandler<byte[], byte[]> matchHandler = null, SendOptions options = null)
        {
            return Task.Run(() => Send(data, timeout, matchHandler, options));
        }
    }
}
