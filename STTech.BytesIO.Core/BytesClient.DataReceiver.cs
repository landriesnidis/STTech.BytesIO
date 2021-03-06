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
    //                                  接收数据
    // 
    // ===============================================================================

    public abstract partial class BytesClient
    {
        /// <summary>
        /// 异步接收数据任务取消令牌
        /// </summary>
        private CancellationToken _asyncReceiveDataCancellationToken;
        private CancellationTokenSource _cancellationTokenSource;

        /// <summary>
        /// 启动异步数据接收任务
        /// </summary>
        protected virtual void StartReceiveDataTask()
        {
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = new CancellationTokenSource();

            CancelReceiveDataTask();
            _asyncReceiveDataCancellationToken = _cancellationTokenSource.Token;

            Task task = new Task(() => SafelyInvokeCallback(() => { ReceiveDataHandle(); }), _asyncReceiveDataCancellationToken);
            var awaiter = task.GetAwaiter();
            awaiter.OnCompleted(() =>
            {
                if (awaiter.IsCompleted)
                {
                    SafelyInvokeCallback(() => { ReceiveDataCompletedHandle(); });
                }
            });
            task.Start();
        }

        /// <summary>
        /// 取消异步数据接收任务
        /// </summary>
        protected virtual void CancelReceiveDataTask()
        {
            // 关闭异步接收任务
            if (_asyncReceiveDataCancellationToken != null && _asyncReceiveDataCancellationToken.IsCancellationRequested)
            {
                _asyncReceiveDataCancellationToken.ThrowIfCancellationRequested();
            }
        }

        /// <summary>
        /// 异步接收数据的处理过程
        /// </summary>
        protected abstract void ReceiveDataHandle();

        /// <summary>
        /// 数据接收完成的处理过程
        /// </summary>
        protected abstract void ReceiveDataCompletedHandle();
    }
}
