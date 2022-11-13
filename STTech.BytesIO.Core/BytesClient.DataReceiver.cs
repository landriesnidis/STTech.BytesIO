using STTech.BytesIO.Core;
using STTech.BytesIO.Core.Exceptions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace STTech.BytesIO.Core
{
    // ===============================================================================
    // 
    //                                  接收数据
    // 
    // ===============================================================================

    public abstract partial class BytesClient
    {
        /// <summary>
        /// 异步接收数据任务取消令牌
        /// </summary>
        protected CancellationTokenSource ReceiveTaskCancellationTokenSource { get; private set; }

        /// <summary>
        /// 启动异步数据接收任务
        /// </summary>
        protected virtual void StartReceiveDataTask()
        {
            ReceiveTaskCancellationTokenSource?.Cancel();
            ReceiveTaskCancellationTokenSource?.Dispose();
            ReceiveTaskCancellationTokenSource = new CancellationTokenSource();

            Task task = new Task(ReceiveDataHandle, ReceiveTaskCancellationTokenSource.Token);
            task.ContinueWith(t =>
            {
                if (t.IsCompleted)
                {
                    SafelyInvokeCallback(ReceiveDataCompletedHandle);
                }
            });
            task.Start();
        }

        /// <summary>
        /// 待触发事件的接收数据队列
        /// </summary>
        private ConcurrentQueue<byte[]> receivedDataQueue = new ConcurrentQueue<byte[]>();

        /// <summary>
        /// 触发数据接收事件的锁
        /// </summary>
        private readonly object lockerRaiseDataReceived = new object();

        /// <summary>
        /// 接收数据帧的ID
        /// </summary>
        private uint receivedDataFrameId = 0;

        /// <summary>
        /// 取消异步数据接收任务
        /// </summary>
        protected virtual void CancelReceiveDataTask()
        {
            // 关闭异步接收任务
            //if (_asyncReceiveDataCancellationToken != null && _asyncReceiveDataCancellationToken.IsCancellationRequested)
            //{
            //    _asyncReceiveDataCancellationToken.ThrowIfCancellationRequested();
            //}

            ReceiveTaskCancellationTokenSource?.Cancel();
        }

        /// <summary>
        /// 异步接收数据的处理过程
        /// </summary>
        protected abstract void ReceiveDataHandle();

        /// <summary>
        /// 数据接收完成的处理过程
        /// </summary>
        protected abstract void ReceiveDataCompletedHandle();

        /// <summary>
        /// 调用数据接收事件的回调
        /// </summary>
        /// <param name="data"></param>
        protected void InvokeDataReceivedEventCallback(byte[] data)
        {
            // 更新时间戳
            UpdateLastMessageTimestamp();

            // 将新的数据帧加入队列
            receivedDataQueue.Enqueue(data);

            // 异步执行触发数据接收事件的回调
            Task.Factory.StartNew(() =>
            {
                lock (lockerRaiseDataReceived)
                {
                    byte[] data;
                    while (receivedDataQueue.TryDequeue(out data))
                    {
                        // 执行接收到数据的回调事件
                        RaiseDataReceived(this, new DataReceivedEventArgs(data, receivedDataFrameId++));
                    }
                }
            });
        }
    }
}
