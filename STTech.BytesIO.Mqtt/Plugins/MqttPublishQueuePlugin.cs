using STTech.BytesIO.Mqtt;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace STTech.BytesIO.Mqtt
{
    /// <summary>
    /// 待发送的队列消息任务包。
    /// </summary>
    internal class QueuedPublishItem
    {
        public MqttApplicationMessage Message { get; }
        public TaskCompletionSource<MqttPublishResult> CompletionSource { get; }
        public CancellationToken CancellationToken { get; }

        public QueuedPublishItem(MqttApplicationMessage message, TaskCompletionSource<MqttPublishResult> tcs, CancellationToken cancellationToken)
        {
            Message = message;
            CompletionSource = tcs;
            CancellationToken = cancellationToken;
        }
    }

    /// <summary>
    /// MQTT 消息发送队列与离线缓冲插件。
    /// 实现异步排队执行，支持离线消息自动暂存与重连后自动发送，提供最大容量限制与可配置溢出策略。
    /// </summary>
    public class MqttPublishQueuePlugin : MqttClientPlugin
    {
        private readonly object _lock = new object();
        private readonly LinkedList<QueuedPublishItem> _pendingQueue = new LinkedList<QueuedPublishItem>();
        private readonly SemaphoreSlim _capacitySignal;
        private readonly SemaphoreSlim _drainLock = new SemaphoreSlim(1, 1);

        /// <summary>
        /// 队列最大消息容量（小于等于 0 表示无限制）。默认 10,000。
        /// </summary>
        public int MaxQueueSize { get; set; } = 10000;

        /// <summary>
        /// 队列溢出策略，默认为 <see cref="QueueOverflowStrategy.DropOldest"/>。
        /// </summary>
        public QueueOverflowStrategy OverflowStrategy { get; set; } = QueueOverflowStrategy.DropOldest;

        /// <summary>
        /// 阻塞入队超时时长（当 <see cref="OverflowStrategy"/> 为 Block 时生效）。默认 5000 毫秒。
        /// </summary>
        public TimeSpan BlockTimeout { get; set; } = TimeSpan.FromSeconds(5);

        /// <summary>
        /// 当前队列中积压的消息数量。
        /// </summary>
        public int QueuedCount
        {
            get
            {
                lock (_lock)
                {
                    return _pendingQueue.Count;
                }
            }
        }

        /// <summary>
        /// 当队列发生溢出时触发。
        /// </summary>
        public event EventHandler<MqttQueueOverflowEventArgs> OnQueueOverflow;

        /// <summary>
        /// 构造 MQTT 消息发送队列插件。
        /// </summary>
        public MqttPublishQueuePlugin()
        {
            _capacitySignal = new SemaphoreSlim(0, int.MaxValue);
        }

        /// <summary>
        /// 将消息推入发送队列异步发布。
        /// 若客户端当前未连接，消息将暂存在队列中，待连接建立后自动发送。
        /// </summary>
        /// <param name="message">要发布的 MQTT 消息</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>发布操作结果任务</returns>
        public Task<MqttPublishResult> EnqueuePublishAsync(MqttApplicationMessage message, CancellationToken cancellationToken = default)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));

            var tcs = new TaskCompletionSource<MqttPublishResult>(TaskCreationOptions.RunContinuationsAsynchronously);

            if (cancellationToken.IsCancellationRequested)
            {
                tcs.TrySetCanceled(cancellationToken);
                return tcs.Task;
            }

            var item = new QueuedPublishItem(message, tcs, cancellationToken);

            if (cancellationToken.CanBeCanceled)
            {
                cancellationToken.Register(() =>
                {
                    lock (_lock)
                    {
                        if (_pendingQueue.Remove(item))
                        {
                            item.CompletionSource.TrySetCanceled(cancellationToken);
                        }
                    }
                });
            }

            // 队列容量与溢出控制
            if (!HandleCapacityAndEnqueue(item))
            {
                return tcs.Task;
            }

            // 若当前客户端在线，调度队列消费
            if (Client != null && Client.IsConnected)
            {
                TriggerDrain();
            }

            return tcs.Task;
        }

        private bool HandleCapacityAndEnqueue(QueuedPublishItem item)
        {
            while (true)
            {
                lock (_lock)
                {
                    if (MaxQueueSize <= 0 || _pendingQueue.Count < MaxQueueSize)
                    {
                        _pendingQueue.AddLast(item);
                        return true;
                    }

                    // 队列已满，按策略处理
                    switch (OverflowStrategy)
                    {
                        case QueueOverflowStrategy.DropOldest:
                            var oldest = _pendingQueue.First.Value;
                            _pendingQueue.RemoveFirst();
                            oldest.CompletionSource.TrySetException(new MqttCommunicationException("由于队列已满且采用 DropOldest 策略，该消息已被丢弃。"));
                            OnQueueOverflow?.Invoke(this, new MqttQueueOverflowEventArgs(oldest.Message, OverflowStrategy));
                            _pendingQueue.AddLast(item);
                            return true;

                        case QueueOverflowStrategy.DropNewest:
                            item.CompletionSource.TrySetException(new MqttCommunicationException("由于队列已满且采用 DropNewest 策略，该消息已被拒绝。"));
                            OnQueueOverflow?.Invoke(this, new MqttQueueOverflowEventArgs(item.Message, OverflowStrategy));
                            return false;

                        case QueueOverflowStrategy.ThrowException:
                            throw new InvalidOperationException($"MQTT 发布队列已达上限 ({MaxQueueSize})，无法入队。");

                        case QueueOverflowStrategy.Block:
                            // 离开锁并在下面等待信号
                            break;
                    }
                }

                // 阻塞模式
                bool waitSuccess = _capacitySignal.Wait(BlockTimeout, item.CancellationToken);
                if (!waitSuccess)
                {
                    item.CompletionSource.TrySetException(new TimeoutException("入队等待超时。"));
                    return false;
                }
            }
        }

        /// <inheritdoc/>
        protected internal override void OnMqttConnectedSuccessfully(MqttConnectedEventArgs e)
        {
            base.OnMqttConnectedSuccessfully(e);
            // 连接恢复后触发队列发送
            TriggerDrain();
        }

        private void TriggerDrain()
        {
            _ = Task.Run(async () =>
            {
                if (!await _drainLock.WaitAsync(0).ConfigureAwait(false))
                {
                    return;
                }

                try
                {
                    await DrainQueueAsync().ConfigureAwait(false);
                }
                finally
                {
                    _drainLock.Release();
                }

                // 双重校验：防止在释放锁的微瞬间有新消息入队
                bool hasPending;
                lock (_lock)
                {
                    hasPending = _pendingQueue.Count > 0;
                }
                if (hasPending && Client != null && Client.IsConnected)
                {
                    TriggerDrain();
                }
            });
        }

        private async Task DrainQueueAsync()
        {
            while (Client != null && Client.IsConnected)
            {
                QueuedPublishItem currentItem = null;
                lock (_lock)
                {
                    if (_pendingQueue.Count > 0)
                    {
                        currentItem = _pendingQueue.First.Value;
                        _pendingQueue.RemoveFirst();
                        if (OverflowStrategy == QueueOverflowStrategy.Block)
                        {
                            try { _capacitySignal.Release(); } catch { }
                        }
                    }
                }

                if (currentItem == null)
                {
                    break;
                }

                if (currentItem.CancellationToken.IsCancellationRequested)
                {
                    currentItem.CompletionSource.TrySetCanceled(currentItem.CancellationToken);
                    continue;
                }

                try
                {
                    var result = await Client.PublishAsync(currentItem.Message, currentItem.CancellationToken).ConfigureAwait(false);
                    currentItem.CompletionSource.TrySetResult(result);
                }
                catch (Exception ex)
                {
                    currentItem.CompletionSource.TrySetException(ex);
                }
            }
        }

        /// <summary>
        /// 清空队列中所有尚未发送的消息。
        /// </summary>
        public void Clear()
        {
            lock (_lock)
            {
                foreach (var item in _pendingQueue)
                {
                    item.CompletionSource.TrySetCanceled();
                }
                _pendingQueue.Clear();
            }
        }

        /// <inheritdoc/>
        public override void Dispose()
        {
            Clear();
            _drainLock.Dispose();
            _capacitySignal.Dispose();
            base.Dispose();
        }
    }
}
