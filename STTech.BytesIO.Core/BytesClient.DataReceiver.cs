using STTech.BytesIO.Core;
using STTech.BytesIO.Core.Exceptions;
using STTech.CodePlus.Components;
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
        /// 数据接收任务队列
        /// </summary>
        private TaskQueue<DataReceivedEventArgs> dataReceiveTaskQueue;
        private TaskQueue<SendArgs> dataSendTaskQueue;

        /// <summary>
        /// 接收数据帧的ID
        /// </summary>
        private uint receivedDataFrameId = 0;

        public BytesClient()
        {
            dataReceiveTaskQueue = new FloaterTaskQueue<DataReceivedEventArgs>(DataReceiveTaskQueueHandler);
            dataSendTaskQueue = new FloaterTaskQueue<SendArgs>(SendHandler);
        }

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
                ReceiveTaskCancellationTokenSource = null;

                // 不同状态分别处理
                if (t.IsCompleted)
                {
                    ReceiveDataCompletedHandle();
                }
                else if (t.IsCanceled)
                {
                    Disconnect(new DisconnectArgument(DisconnectionReasonCode.Active));
                }
                else if (t.IsFaulted)
                {
                    Disconnect(new DisconnectArgument(DisconnectionReasonCode.Error, t.Exception));
                }
            });
            task.Start();
        }

        /// <summary>
        /// 取消异步数据接收任务
        /// </summary>
        protected virtual void CancelReceiveDataTask()
        {
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
        /// <param name="block"></param>
        protected void InvokeDataReceivedEventCallback(MemoryBlock block)
        {
            // 更新时间戳
            UpdateLastMessageTimestamp();

            var args = new DataReceivedEventArgs(block, receivedDataFrameId++);
            dataReceiveTaskQueue.Join(args);
        }

        private void DataReceiveTaskQueueHandler(DataReceivedEventArgs e)
        {
            // 执行接收到数据的回调事件
            RaiseDataReceived(this, e);

            if (e.Data.AutoDispose)
            {
                e.Data.Dispose();
            }
        }
    }
}
