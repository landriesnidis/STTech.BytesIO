using STTech.BytesIO.Core;
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
        /// 为基于BytesClient的客户端绑定解包器
        /// </summary>
        /// <param name="client"></param>
        /// <param name="unpacker"></param>
        public static void BindUnpacker(this BytesClient client, Unpacker unpacker)
        {
            client.OnDataReceived += (s, e) =>
            {
                unpacker.Input(e.Data);
            };
        }

        /// <summary>
        /// 发送数据
        /// 阻塞等待单次发送的响应结果
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
        /// <param name="options"></param>
        /// <returns>单次发送数据的远端响应</returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static Reply<TRecv> Send<TSend, TRecv>(this IUnpackerSupport<TRecv> unpackerSupport, TSend request, int timeout, ReplyMatchHandler<TSend, TRecv> matchHandler = null, SendOptions options = null)
            where TSend : IRequest
            where TRecv : Response
        {
            return SendAsync(unpackerSupport, request, timeout, matchHandler, options).GetAwaiter().GetResult();
        }

        /// <summary>
        /// 发送数据
        /// 异步非阻塞等待单次发送的响应结果
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
        /// <param name="options">发送选项</param>
        /// <returns>单次发送数据并等待远端响应的任务</returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static async Task<Reply<TRecv>> SendAsync<TSend, TRecv>(this IUnpackerSupport<TRecv> unpackerSupport, TSend request, int timeout, ReplyMatchHandler<TSend, TRecv> matchHandler = null, SendOptions options = null) 
            where TSend : IRequest 
            where TRecv : Response
        {
            if (unpackerSupport is null) throw new ArgumentNullException(nameof(unpackerSupport));
            if (unpackerSupport.Unpacker is null) throw new ArgumentNullException(nameof(unpackerSupport.Unpacker));

            var client = unpackerSupport.Unpacker.Client;
            var tcs = new TaskCompletionSource<Reply<TRecv>>(TaskCreationOptions.RunContinuationsAsynchronously);

            EventHandler<DataParsedEventArgs<TRecv>> dataParsedHandle = null;
            EventHandler<DisconnectedEventArgs> disconnectedHandle = null;

            dataParsedHandle = (sender, e) =>
            {
                if (matchHandler != null && !matchHandler.Invoke(request, e.Data)) return;
                tcs.TrySetResult(new Reply<TRecv>(client, e.Data));
            };

            disconnectedHandle = (sender, e) =>
            {
                tcs.TrySetResult(new Reply<TRecv>(client, ReplyStatus.Interrupted, null));
            };

            unpackerSupport.Unpacker.OnDataParsed += dataParsedHandle;
            unpackerSupport.Unpacker.Client.OnDisconnected += disconnectedHandle;

            try
            {
                client.Send(request.GetBytes(), options);

                if (timeout > 0 && timeout != Timeout.Infinite)
                {
                    using (var cts = new CancellationTokenSource(timeout))
                    using (cts.Token.Register(() => tcs.TrySetResult(new Reply<TRecv>(client, ReplyStatus.Timeout, null)), useSynchronizationContext: false))
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
                unpackerSupport.Unpacker.OnDataParsed -= dataParsedHandle;
                unpackerSupport.Unpacker.Client.OnDisconnected -= disconnectedHandle;
            }
        }
    }
}
