using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace STTech.BytesIO.Mqtt
{
    public abstract partial class MqttClient
    {
        #region Subscribe Async

        /// <summary>
        /// 异步订阅指定主题并绑定处理委托。
        /// </summary>
        public async Task<MqttSubscribeResult> SubscribeAsync(MqttTopicSubscription subscription, Func<MqttApplicationMessage, Task> handler = null, CancellationToken cancellationToken = default)
        {
            if (subscription == null) throw new ArgumentNullException(nameof(subscription));
            return await SubscribeAsync(new[] { subscription }, handler != null ? (topic, h) => { if (topic == subscription.TopicFilter) return handler; return null; } : null, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// 异步订阅指定主题。
        /// </summary>
        public Task<MqttSubscribeResult> SubscribeAsync(string topicFilter, MqttQualityOfServiceLevel qos = MqttQualityOfServiceLevel.AtMostOnce, Func<MqttApplicationMessage, Task> handler = null, CancellationToken cancellationToken = default)
        {
            return SubscribeAsync(new MqttTopicSubscription(topicFilter, qos), handler, cancellationToken);
        }

        /// <summary>
        /// 批量异步订阅主题。
        /// </summary>
        public async Task<MqttSubscribeResult> SubscribeAsync(IEnumerable<MqttTopicSubscription> subscriptions, Func<string, Func<MqttApplicationMessage, Task>, Func<MqttApplicationMessage, Task>> handlerSelector = null, CancellationToken cancellationToken = default)
        {
            if (subscriptions == null) throw new ArgumentNullException(nameof(subscriptions));
            var subList = subscriptions.ToList();
            if (subList.Count == 0) throw new ArgumentException("订阅列表不能为空。", nameof(subscriptions));

            if (!IsConnected)
            {
                return new MqttSubscribeResult
                {
                    IsSuccess = false,
                    Exception = new MqttCommunicationException("客户端尚未连接，无法发起订阅。")
                };
            }

            ushort packetId = PacketIdManager.GetNextPacketId();
            var subPacket = new MqttSubscribePacket
            {
                PacketId = packetId,
                Subscriptions = subList
            };

            var tcs = new TaskCompletionSource<MqttSubAckPacket>(TaskCreationOptions.RunContinuationsAsynchronously);
            _pendingSubAcks[packetId] = tcs;

            // 预先注册路由与 QoS，防止 Broker 在回复 SUBACK 时并行推送数据导致匹配不到路由
            foreach (var sub in subList)
            {
                Func<MqttApplicationMessage, Task> handler = handlerSelector?.Invoke(sub.TopicFilter, null);
                Router.AddRoute(sub.TopicFilter, handler, sub.QualityOfServiceLevel);
            }

            var result = new MqttSubscribeResult { PacketId = packetId };

            try
            {
                await SendPacketAsync(subPacket, cancellationToken).ConfigureAwait(false);

                using (var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
                {
                    linkedCts.CancelAfter(Options.CommunicationTimeout);
                    using (linkedCts.Token.Register(() => tcs.TrySetCanceled()))
                    {
                        var subAck = await tcs.Task.ConfigureAwait(false);
                        result.ReasonCodes = subAck.ReasonCodes;
                        result.Properties = subAck.Properties;
                        result.IsSuccess = subAck.ReasonCodes.All(code => (byte)code < 0x80);

                        if (!result.IsSuccess)
                        {
                            foreach (var sub in subList)
                            {
                                Router.RemoveRoute(sub.TopicFilter);
                            }
                        }

                        return result;
                    }
                }
            }
            catch (Exception ex)
            {
                foreach (var sub in subList)
                {
                    Router.RemoveRoute(sub.TopicFilter);
                }

                result.IsSuccess = false;
                result.Exception = ex;
                return result;
            }
            finally
            {
                _pendingSubAcks.TryRemove(packetId, out _);
                PacketIdManager.ReleasePacketId(packetId);
            }
        }

        #endregion

        #region Unsubscribe Async

        /// <summary>
        /// 异步取消订阅指定主题。
        /// </summary>
        public Task<MqttUnsubscribeResult> UnsubscribeAsync(string topicFilter, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(topicFilter)) throw new ArgumentNullException(nameof(topicFilter));
            return UnsubscribeAsync(new[] { topicFilter }, cancellationToken);
        }

        /// <summary>
        /// 批量异步取消订阅指定主题。
        /// </summary>
        public async Task<MqttUnsubscribeResult> UnsubscribeAsync(IEnumerable<string> topicFilters, CancellationToken cancellationToken = default)
        {
            if (topicFilters == null) throw new ArgumentNullException(nameof(topicFilters));
            var filterList = topicFilters.ToList();
            if (filterList.Count == 0) throw new ArgumentException("取消订阅列表不能为空。", nameof(topicFilters));

            if (!IsConnected)
            {
                return new MqttUnsubscribeResult
                {
                    IsSuccess = false,
                    Exception = new MqttCommunicationException("客户端尚未连接，无法取消订阅。")
                };
            }

            ushort packetId = PacketIdManager.GetNextPacketId();
            var unsubPacket = new MqttUnsubscribePacket
            {
                PacketId = packetId,
                TopicFilters = filterList
            };

            var tcs = new TaskCompletionSource<MqttUnsubAckPacket>(TaskCreationOptions.RunContinuationsAsynchronously);
            _pendingUnsubAcks[packetId] = tcs;

            var result = new MqttUnsubscribeResult { PacketId = packetId };

            try
            {
                await SendPacketAsync(unsubPacket, cancellationToken).ConfigureAwait(false);

                using (var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
                {
                    linkedCts.CancelAfter(Options.CommunicationTimeout);
                    using (linkedCts.Token.Register(() => tcs.TrySetCanceled()))
                    {
                        var unsubAck = await tcs.Task.ConfigureAwait(false);
                        result.ReasonCodes = unsubAck.ReasonCodes;
                        result.Properties = unsubAck.Properties;
                        result.IsSuccess = unsubAck.ReasonCodes.All(code => (byte)code < 0x80);

                        if (result.IsSuccess)
                        {
                            foreach (var filter in filterList)
                            {
                                Router.RemoveRoute(filter);
                            }
                        }

                        return result;
                    }
                }
            }
            catch (Exception ex)
            {
                result.IsSuccess = false;
                result.Exception = ex;
                return result;
            }
            finally
            {
                _pendingUnsubAcks.TryRemove(packetId, out _);
                PacketIdManager.ReleasePacketId(packetId);
            }
        }

        #endregion

        #region Subscribe & Unsubscribe Sync Wrappers

        /// <summary>
        /// 同步订阅指定主题。
        /// </summary>
        public MqttSubscribeResult Subscribe(string topicFilter, MqttQualityOfServiceLevel qos = MqttQualityOfServiceLevel.AtMostOnce, Action<MqttApplicationMessage> handler = null, int timeout = 10000)
        {
            using (var cts = new CancellationTokenSource(timeout))
            {
                Func<MqttApplicationMessage, Task> asyncHandler = handler != null ? (msg) => { handler(msg); return Task.CompletedTask; } : (Func<MqttApplicationMessage, Task>)null;
                return SubscribeAsync(topicFilter, qos, asyncHandler, cts.Token).GetAwaiter().GetResult();
            }
        }

        /// <summary>
        /// 同步取消订阅指定主题。
        /// </summary>
        public MqttUnsubscribeResult Unsubscribe(string topicFilter, int timeout = 10000)
        {
            using (var cts = new CancellationTokenSource(timeout))
            {
                return UnsubscribeAsync(topicFilter, cts.Token).GetAwaiter().GetResult();
            }
        }

        #endregion

        #region Auto Resubscribe

        /// <summary>
        /// 重新补订所有已知的主题（保留原有的 QoS 服务质量等级）。
        /// </summary>
        private async Task ResubscribeAllAsync()
        {
            var registeredTopics = Router.GetSubscribedTopicsWithQoS();
            if (registeredTopics == null || registeredTopics.Count == 0)
            {
                return;
            }

            var subscriptions = registeredTopics.Select(kv => new MqttTopicSubscription(kv.Key, kv.Value)).ToList();
            try
            {
                await SubscribeAsync(subscriptions).ConfigureAwait(false);
            }
            catch { }
        }

        #endregion
    }
}
