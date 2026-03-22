using STTech.CodePlus.Components;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;

namespace STTech.BytesIO.Core
{
    // ===============================================================================
    // 
    //                                  发送数据
    // 
    // ===============================================================================

    public abstract partial class BytesClient
    {
        // 异步发送专用并发队列
        private readonly ConcurrentQueue<SendArgs> asyncSendQueue = new ConcurrentQueue<SendArgs>();
        
        // 发送泵执行状态锁(无锁设计): 0=空闲, 1=发送中
        private int asyncSendPumpStatus = 0;

        /// <summary>
        /// 默认的发送选项
        /// </summary>
        public SendOptions DefaultSendOptions { get; } = new SendOptions();

        /// <summary>
        /// 异步发送数据的实现过程
        /// </summary>
        protected abstract Task SendHandlerAsync(SendArgs data);

        /// <summary>
        /// 触发发送泵，处理队列积压信号
        /// </summary>
        private void TriggerSendPump()
        {
            if (Interlocked.CompareExchange(ref asyncSendPumpStatus, 1, 0) == 0)
            {
                _ = ProcessSendQueueAsync();
            }
        }

        private async Task ProcessSendQueueAsync()
        {
            while (asyncSendQueue.TryDequeue(out var args))
            {
                try
                {
                    // 检测：是否已经被提供者取消？
                    if (args.Options.CancellationToken.IsCancellationRequested)
                    {
                        args.Tcs?.TrySetCanceled();
                        continue;
                    }

                    // 检测：是否因为在发送队列堆积过久导致 TTL 超时？
                    if (args.Options.ExpireTimeout > 0 && (DateTime.Now - args.EnqueueTime).TotalMilliseconds > args.Options.ExpireTimeout)
                    {
                        args.Tcs?.TrySetException(new TimeoutException($"数据包在队列中等待发送的时间超过了允许的 {args.Options.ExpireTimeout} 毫秒。"));
                        continue;
                    }

                    await SendHandlerAsync(args).ConfigureAwait(false);
                    args.Tcs?.TrySetResult(true);
                }
                catch (Exception ex)
                {
                    // 若底层协议处理抛异常，通知到用户的 await
                    args.Tcs?.TrySetException(ex);
                }
            }
            
            Interlocked.Exchange(ref asyncSendPumpStatus, 0);

            // 二次确认，防止出队检查时恰好有新请求插入
            if (!asyncSendQueue.IsEmpty && Interlocked.CompareExchange(ref asyncSendPumpStatus, 1, 0) == 0)
            {
                _ = ProcessSendQueueAsync();
            }
        }

        /// <summary>
        /// 安全排空当前的发送队列（阻塞等待直到队列为空或超时）。
        /// 配合 GracefulShutdown 优雅关闭通信链路前使用。
        /// </summary>
        /// <param name="timeoutMs">最大容忍等待时长 (毫秒)</param>
        protected void WaitAndDrainSendQueue(int timeoutMs = 3000)
        {
            if (asyncSendQueue.IsEmpty) return;

            var sw = System.Diagnostics.Stopwatch.StartNew();
            while (!asyncSendQueue.IsEmpty && sw.ElapsedMilliseconds < timeoutMs)
            {
                // 如果在等排空的时候底层已经意外断开了，直接放弃排队。
                if (!IsConnected) break; 
                Thread.Sleep(10);
            }
        }

        /// <summary>
        /// 发送数据 (同步代理)
        /// </summary>
        /// <param name="data"></param>
        /// <param name="options">发送选项</param>
        public void Send(byte[] data, SendOptions options = null)
        {
            SendAsync(data, options).GetAwaiter().GetResult();
        }

        /// <summary>
        /// 异步发送数据
        /// </summary>
        /// <param name="data"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public Task SendAsync(byte[] data, SendOptions options = null)
        {
            options ??= DefaultSendOptions;
            var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            asyncSendQueue.Enqueue(new SendArgs(data, options, tcs));
            TriggerSendPump();
            return tcs.Task;
        }

        /// <summary>
        /// 发送请求实体
        /// </summary>
        /// <typeparam name="TRequest"></typeparam>
        /// <param name="request"></param>
        /// <param name="options"></param>
        public void Send<TRequest>(TRequest request, SendOptions options = null)
            where TRequest : IRequest
        {
            SendAsync(request, options).GetAwaiter().GetResult();
        }

        /// <summary>
        /// 异步发送请求实体
        /// </summary>
        /// <typeparam name="TRequest"></typeparam>
        /// <param name="request"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public Task SendAsync<TRequest>(TRequest request, SendOptions options = null)
            where TRequest : IRequest
        {
            return SendAsync(request.GetBytes(), options);
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
        public ReplyBytes Send(byte[] data, int timeout, ReplyMatchHandler<byte[], ReceiveContext> matchHandler = null, SendOptions options = null)
        {
            return SendAsync(data, timeout, matchHandler, options).GetAwaiter().GetResult();
        }

        /// <summary>
        /// 异步数据
        /// 异步非阻塞等待单次发送的响应结果
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
        /// <param name="options">附加发送选项</param>
        /// <returns>单次发送数据并等待远端响应的任务</returns>
        public async Task<ReplyBytes> SendAsync(byte[] data, int timeout, ReplyMatchHandler<byte[], ReceiveContext> matchHandler = null, SendOptions options = null)
        {
            var tcs = new TaskCompletionSource<ReplyBytes>(TaskCreationOptions.RunContinuationsAsynchronously);

            EventHandler<DataReceivedEventArgs> dataReceivedHandle = null;
            EventHandler<DisconnectedEventArgs> disconnectedHandle = null;

            dataReceivedHandle = (sender, e) =>
            {
                if (matchHandler != null && !matchHandler.Invoke(data, e.Data)) return;
                tcs.TrySetResult(new ReplyBytes(this, e.Data));
            };

            disconnectedHandle = (sender, e) =>
            {
                tcs.TrySetResult(new ReplyBytes(this, ReplyStatus.Interrupted, null));
            };

            OnDataReceived += dataReceivedHandle;
            OnDisconnected += disconnectedHandle;

            try
            {
                Send(data, options);

                if (timeout > 0 && timeout != Timeout.Infinite)
                {
                    using (var cts = new CancellationTokenSource(timeout))
                    using (cts.Token.Register(() => tcs.TrySetResult(new ReplyBytes(this, ReplyStatus.Timeout, null)), useSynchronizationContext: false))
                    {
                        return await tcs.Task.ConfigureAwait(false);
                    }
                }
                else
                {
                    return await tcs.Task.ConfigureAwait(false);
                }
            }
            finally
            {
                OnDataReceived -= dataReceivedHandle;
                OnDisconnected -= disconnectedHandle;
            }
        }
    }
}
