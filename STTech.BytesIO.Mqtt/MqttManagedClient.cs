using STTech.BytesIO.Core;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace STTech.BytesIO.Mqtt
{
    /// <summary>
    /// 支持指定内部通信客户端类型的托管 MQTT 客户端。
    /// 继承自 <see cref="MqttClient{TClient}"/>，内置自动重连、离线发布队列与断线自动重订阅机制。
    /// </summary>
    /// <typeparam name="TClient">内部通信客户端类型</typeparam>
    public class MqttManagedClient<TClient> : MqttClient<TClient> where TClient : BytesClient
    {
        /// <summary>
        /// 消息发送队列与离线缓冲插件。
        /// </summary>
        public MqttPublishQueuePlugin PublishQueue { get; }

        /// <summary>
        /// 断线自动恢复订阅插件。
        /// </summary>
        public MqttAutoResubscribePlugin AutoResubscribe { get; }

        /// <summary>
        /// 自动重连插件。
        /// </summary>
        public ReconnectPlugin Reconnect { get; }

        /// <summary>
        /// 队列最大消息容量（小于等于 0 表示无限制）。默认 10,000。
        /// </summary>
        public int MaxQueueSize
        {
            get => PublishQueue.MaxQueueSize;
            set => PublishQueue.MaxQueueSize = value;
        }

        /// <summary>
        /// 队列溢出策略，默认为 <see cref="QueueOverflowStrategy.DropOldest"/>。
        /// </summary>
        public QueueOverflowStrategy OverflowStrategy
        {
            get => PublishQueue.OverflowStrategy;
            set => PublishQueue.OverflowStrategy = value;
        }

        /// <summary>
        /// 阻塞入队超时时长。默认 5000 毫秒。
        /// </summary>
        public TimeSpan BlockTimeout
        {
            get => PublishQueue.BlockTimeout;
            set => PublishQueue.BlockTimeout = value;
        }

        /// <summary>
        /// 当前队列中积压的消息数量。
        /// </summary>
        public int QueuedCount => PublishQueue.QueuedCount;

        /// <summary>
        /// 构造托管 MQTT 客户端实例。
        /// </summary>
        /// <param name="client">底层的通信客户端</param>
        /// <param name="protocolVersion">协议版本</param>
        public MqttManagedClient(TClient client, MqttProtocolVersion protocolVersion = MqttProtocolVersion.V5)
            : base(client, protocolVersion)
        {
            // 托管客户端由 AutoResubscribe 插件全权管理断线重订阅，禁用底层的 AutoResubscribe 选项以避免双重补发风暴
            Options.AutoResubscribe = false;

            PublishQueue = new MqttPublishQueuePlugin();
            AutoResubscribe = new MqttAutoResubscribePlugin();
            Reconnect = new ReconnectPlugin();

            AddPlugin(PublishQueue);
            AddPlugin(AutoResubscribe);
            AddPlugin(Reconnect);

            // 默认配置重连延时 5000 毫秒，无限次重试
            Reconnect.Configure(5000, 0);
        }

        /// <summary>
        /// 配置自动重连参数。
        /// </summary>
        /// <param name="delay">重连延时（毫秒），0 或负数表示禁用</param>
        /// <param name="times">最大重试次数，0 表示无限</param>
        /// <param name="reconnectFailedHandler">重连失败回调</param>
        public void ConfigureReconnect(int delay, int times = 0, Action<BytesClient> reconnectFailedHandler = null)
        {
            Reconnect.Configure(delay, times, reconnectFailedHandler);
        }

        /// <summary>
        /// 异步将消息加入发送队列排队发布。若离线则自动暂存，待连接恢复后重发。
        /// </summary>
        /// <param name="message">应用业务消息</param>
        /// <param name="cancellationToken">取消令牌</param>
        public Task<MqttPublishResult> EnqueuePublishAsync(MqttApplicationMessage message, CancellationToken cancellationToken = default)
        {
            return PublishQueue.EnqueuePublishAsync(message, cancellationToken);
        }

        /// <summary>
        /// 异步订阅指定主题并将其自动纳管到自动重订阅插件中。
        /// </summary>
        public new async Task<MqttSubscribeResult> SubscribeAsync(MqttTopicSubscription subscription, Func<MqttApplicationMessage, Task> handler = null, CancellationToken cancellationToken = default)
        {
            if (subscription == null) throw new ArgumentNullException(nameof(subscription));
            AutoResubscribe.TrackSubscription(subscription.TopicFilter, subscription.QualityOfServiceLevel);
            return await base.SubscribeAsync(subscription, handler, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// 异步订阅指定主题并将其自动纳管到自动重订阅插件中。
        /// </summary>
        public new async Task<MqttSubscribeResult> SubscribeAsync(string topicFilter, MqttQualityOfServiceLevel qos = MqttQualityOfServiceLevel.AtMostOnce, Func<MqttApplicationMessage, Task> handler = null, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(topicFilter)) throw new ArgumentNullException(nameof(topicFilter));
            AutoResubscribe.TrackSubscription(topicFilter, qos);
            return await base.SubscribeAsync(topicFilter, qos, handler, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// 批量异步订阅主题并将其自动纳管到自动重订阅插件中。
        /// </summary>
        public new async Task<MqttSubscribeResult> SubscribeAsync(IEnumerable<MqttTopicSubscription> subscriptions, Func<string, Func<MqttApplicationMessage, Task>, Func<MqttApplicationMessage, Task>> handlerSelector = null, CancellationToken cancellationToken = default)
        {
            if (subscriptions != null)
            {
                foreach (var sub in subscriptions)
                {
                    AutoResubscribe.TrackSubscription(sub.TopicFilter, sub.QualityOfServiceLevel);
                }
            }
            return await base.SubscribeAsync(subscriptions, handlerSelector, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// 异步取消订阅指定主题，并从自动重订阅列表中移除。
        /// </summary>
        public new async Task<MqttUnsubscribeResult> UnsubscribeAsync(string topicFilter, CancellationToken cancellationToken = default)
        {
            AutoResubscribe.UntrackSubscription(topicFilter);
            return await base.UnsubscribeAsync(topicFilter, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// 批量异步取消订阅主题，并从自动重订阅列表中移除。
        /// </summary>
        public new async Task<MqttUnsubscribeResult> UnsubscribeAsync(IEnumerable<string> topicFilters, CancellationToken cancellationToken = default)
        {
            if (topicFilters != null)
            {
                foreach (var filter in topicFilters)
                {
                    AutoResubscribe.UntrackSubscription(filter);
                }
            }
            return await base.UnsubscribeAsync(topicFilters, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// 托管 MQTT 客户端基类。
    /// </summary>
    public class MqttManagedClient : MqttManagedClient<BytesClient>
    {
        /// <summary>
        /// 构造托管 MQTT 客户端。
        /// </summary>
        /// <param name="client">底层的通信客户端</param>
        /// <param name="protocolVersion">协议版本</param>
        public MqttManagedClient(BytesClient client, MqttProtocolVersion protocolVersion = MqttProtocolVersion.V5) : base(client, protocolVersion)
        {
        }
    }
}
