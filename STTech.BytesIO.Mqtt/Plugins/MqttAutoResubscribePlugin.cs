using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace STTech.BytesIO.Mqtt
{
    /// <summary>
    /// MQTT 客户端自动重新订阅插件。
    /// 自动跟踪所有已成功发起订阅的主题；当网络重连握手成功（且服务端会话未保持）时，自动补发所有订阅，实现业务无感知恢复。
    /// </summary>
    public class MqttAutoResubscribePlugin : MqttClientPlugin
    {
        private readonly ConcurrentDictionary<string, MqttQualityOfServiceLevel> _subscribedTopics =
            new ConcurrentDictionary<string, MqttQualityOfServiceLevel>(StringComparer.Ordinal);

        /// <summary>
        /// 当重订阅开始时发生。
        /// </summary>
        public event EventHandler OnResubscribing;

        /// <summary>
        /// 当重订阅完成时发生。
        /// </summary>
        public event EventHandler<MqttSubscribeResult> OnResubscribeCompleted;

        /// <summary>
        /// 当前受纳管的主题过滤器数量。
        /// </summary>
        public int ManagedSubscriptionCount => _subscribedTopics.Count;

        /// <summary>
        /// 注册要受保护自动重订阅的主题。
        /// </summary>
        /// <param name="topicFilter">主题过滤器</param>
        /// <param name="qos">QoS 服务质量等级</param>
        public void TrackSubscription(string topicFilter, MqttQualityOfServiceLevel qos)
        {
            if (string.IsNullOrEmpty(topicFilter)) return;
            _subscribedTopics[topicFilter] = qos;
        }

        /// <summary>
        /// 移除不再需要重订阅的主题。
        /// </summary>
        /// <param name="topicFilter">主题过滤器</param>
        public void UntrackSubscription(string topicFilter)
        {
            if (string.IsNullOrEmpty(topicFilter)) return;
            _subscribedTopics.TryRemove(topicFilter, out _);
        }

        /// <summary>
        /// 清空所有纳管的重订阅主题。
        /// </summary>
        public void Clear()
        {
            _subscribedTopics.Clear();
        }

        /// <inheritdoc/>
        protected internal override void OnMqttConnectedSuccessfully(MqttConnectedEventArgs e)
        {
            base.OnMqttConnectedSuccessfully(e);

            // 若服务端已保持会话 (SessionPresent == true)，MQTT 协议规范规定无需重新订阅
            if (e.ConnectResult != null && e.ConnectResult.SessionPresent)
            {
                return;
            }

            // 若无任何纳管主题，直接返回
            if (_subscribedTopics.IsEmpty)
            {
                return;
            }

            _ = Task.Run(async () =>
            {
                try
                {
                    OnResubscribing?.Invoke(this, EventArgs.Empty);

                    var subscriptions = _subscribedTopics.Select(kv => new MqttTopicSubscription(kv.Key, kv.Value)).ToList();
                    if (subscriptions.Count > 0 && Client != null && Client.IsConnected)
                    {
                        var result = await Client.SubscribeAsync(subscriptions).ConfigureAwait(false);
                        OnResubscribeCompleted?.Invoke(this, result);
                    }
                }
                catch { }
            });
        }
    }
}
