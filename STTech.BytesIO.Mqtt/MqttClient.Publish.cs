using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace STTech.BytesIO.Mqtt
{
    public abstract partial class MqttClient
    {
        #region Publish Async

        /// <summary>
        /// 异步发布 MQTT 应用消息。
        /// </summary>
        public async Task<MqttPublishResult> PublishAsync(MqttApplicationMessage message, CancellationToken cancellationToken = default)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));
            if (!IsConnected)
            {
                return new MqttPublishResult
                {
                    IsSuccess = false,
                    Exception = new MqttCommunicationException("客户端尚未连接，无法发布消息。")
                };
            }

            var publishPacket = new MqttPublishPacket
            {
                Topic = message.Topic,
                Payload = message.Payload,
                QoS = message.QualityOfServiceLevel,
                Retain = message.Retain,
                Dup = message.Dup,
                Properties = message.Properties
            };

            var result = new MqttPublishResult();

            if (publishPacket.QoS == MqttQualityOfServiceLevel.AtMostOnce)
            {
                // QoS 0: 尽力交付，无需确认
                try
                {
                    await SendPacketAsync(publishPacket, cancellationToken).ConfigureAwait(false);
                    result.IsSuccess = true;
                    return result;
                }
                catch (Exception ex)
                {
                    result.IsSuccess = false;
                    result.Exception = ex;
                    return result;
                }
            }

            ushort packetId = PacketIdManager.GetNextPacketId();
            publishPacket.PacketId = packetId;
            result.PacketId = packetId;

            if (publishPacket.QoS == MqttQualityOfServiceLevel.AtLeastOnce)
            {
                // QoS 1: 等待 PUBACK
                var tcs = new TaskCompletionSource<MqttPubAckPacket>(TaskCreationOptions.RunContinuationsAsynchronously);
                _pendingPubAcks[packetId] = tcs;

                try
                {
                    await SendPacketAsync(publishPacket, cancellationToken).ConfigureAwait(false);

                    using (var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
                    {
                        linkedCts.CancelAfter(Options.CommunicationTimeout);
                        using (linkedCts.Token.Register(() => tcs.TrySetCanceled()))
                        {
                            var pubAck = await tcs.Task.ConfigureAwait(false);
                            result.IsSuccess = pubAck.ReasonCode == MqttPubAckReasonCode.Success || (byte)pubAck.ReasonCode < 0x80;
                            result.ReasonCode = pubAck.ReasonCode;
                            result.Properties = pubAck.Properties;
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
                    _pendingPubAcks.TryRemove(packetId, out _);
                    PacketIdManager.ReleasePacketId(packetId);
                }
            }
            else
            {
                // QoS 2: 等待 PUBREC -> 发送 PUBREL -> 等待 PUBCOMP
                var tcs = new TaskCompletionSource<MqttPubCompPacket>(TaskCreationOptions.RunContinuationsAsynchronously);
                _pendingPubComps[packetId] = tcs;

                try
                {
                    await SendPacketAsync(publishPacket, cancellationToken).ConfigureAwait(false);

                    using (var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
                    {
                        linkedCts.CancelAfter(Options.CommunicationTimeout);
                        using (linkedCts.Token.Register(() => tcs.TrySetCanceled()))
                        {
                            var pubComp = await tcs.Task.ConfigureAwait(false);
                            result.IsSuccess = pubComp.ReasonCode == MqttPubCompReasonCode.Success;
                            result.Properties = pubComp.Properties;
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
                    _pendingPubComps.TryRemove(packetId, out _);
                    PacketIdManager.ReleasePacketId(packetId);
                }
            }
        }

        /// <summary>
        /// 异步发布 MQTT 字节消息。
        /// </summary>
        public Task<MqttPublishResult> PublishAsync(string topic, byte[] payload, MqttQualityOfServiceLevel qos = MqttQualityOfServiceLevel.AtMostOnce, bool retain = false, CancellationToken cancellationToken = default)
        {
            return PublishAsync(new MqttApplicationMessage(topic, payload, qos, retain), cancellationToken);
        }

        /// <summary>
        /// 异步发布 MQTT 字符串消息。
        /// </summary>
        public Task<MqttPublishResult> PublishAsync(string topic, string payloadString, Encoding encoding = null, MqttQualityOfServiceLevel qos = MqttQualityOfServiceLevel.AtMostOnce, bool retain = false, CancellationToken cancellationToken = default)
        {
            return PublishAsync(new MqttApplicationMessage(topic, payloadString, encoding, qos, retain), cancellationToken);
        }

        #endregion

        #region Publish Sync Wrappers

        /// <summary>
        /// 同步发布 MQTT 应用消息。
        /// </summary>
        public MqttPublishResult Publish(MqttApplicationMessage message, int timeout = 10000)
        {
            using (var cts = new CancellationTokenSource(timeout))
            {
                return PublishAsync(message, cts.Token).GetAwaiter().GetResult();
            }
        }

        /// <summary>
        /// 同步发布 MQTT 字节消息。
        /// </summary>
        public MqttPublishResult Publish(string topic, byte[] payload, MqttQualityOfServiceLevel qos = MqttQualityOfServiceLevel.AtMostOnce, bool retain = false, int timeout = 10000)
        {
            return Publish(new MqttApplicationMessage(topic, payload, qos, retain), timeout);
        }

        /// <summary>
        /// 同步发布 MQTT 字符串消息。
        /// </summary>
        public MqttPublishResult Publish(string topic, string payloadString, Encoding encoding = null, MqttQualityOfServiceLevel qos = MqttQualityOfServiceLevel.AtMostOnce, bool retain = false, int timeout = 10000)
        {
            return Publish(new MqttApplicationMessage(topic, payloadString, encoding, qos, retain), timeout);
        }

        #endregion
    }
}
