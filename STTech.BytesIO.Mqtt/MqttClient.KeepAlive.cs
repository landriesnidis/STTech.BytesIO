using STTech.BytesIO.Core;
using STTech.BytesIO.Core.Component;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace STTech.BytesIO.Mqtt
{
    public abstract partial class MqttClient
    {
        private Timer _keepAliveTimer;
        private DateTime _lastSentTime = DateTime.UtcNow;
        private DateTime _lastReceivedTime = DateTime.UtcNow;
        private TimeSpan _keepAliveInterval = TimeSpan.Zero;
        private readonly object _pingLock = new object();

        private void UpdateLastSentTimestamp()
        {
            _lastSentTime = DateTime.UtcNow;
        }

        private void UpdateLastReceivedTimestamp()
        {
            _lastReceivedTime = DateTime.UtcNow;
        }

        private void StartKeepAlive(ushort keepAliveSeconds)
        {
            StopKeepAlive();

            if (keepAliveSeconds <= 0) return;

            _keepAliveInterval = TimeSpan.FromSeconds(keepAliveSeconds);
            _lastSentTime = DateTime.UtcNow;
            _lastReceivedTime = DateTime.UtcNow;

            _keepAliveTimer = new Timer(KeepAliveTimerCallback, null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
        }

        private void StopKeepAlive()
        {
            if (_keepAliveTimer != null)
            {
                _keepAliveTimer.Dispose();
                _keepAliveTimer = null;
            }
        }

        private void KeepAliveTimerCallback(object state)
        {
            if (!IsConnected) return;

            var now = DateTime.UtcNow;

            // 检查接收超时：若已超出 1.5 倍心跳间隔仍未收到任何报文，视作心跳超时
            if (_keepAliveInterval > TimeSpan.Zero && now - _lastReceivedTime > _keepAliveInterval + TimeSpan.FromSeconds(5))
            {
                _ = Task.Run(async () =>
                {
                    await DisconnectMqttAsync(MqttDisconnectReasonCode.KeepAliveTimeout, "心跳响应超时").ConfigureAwait(false);
                });
                return;
            }

            // 检查发送间隔：若空闲时间超过心跳间隔的 75%，主动发送 PINGREQ 保活
            if (now - _lastSentTime >= TimeSpan.FromSeconds(_keepAliveInterval.TotalSeconds * 0.75))
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await PingAsync().ConfigureAwait(false);
                    }
                    catch { }
                });
            }
        }

        /// <summary>
        /// 异步发送 PINGREQ 并等待 PINGRESP 心跳响应。
        /// </summary>
        public async Task PingAsync(CancellationToken cancellationToken = default)
        {
            if (!IsConnected) return;

            var tcs = new TaskCompletionSource<MqttPingRespPacket>(TaskCreationOptions.RunContinuationsAsynchronously);
            lock (_pingLock)
            {
                _pendingPingTcs = tcs;
            }

            try
            {
                await SendPacketAsync(new MqttPingReqPacket(), cancellationToken).ConfigureAwait(false);

                using (var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
                {
                    linkedCts.CancelAfter(Options.CommunicationTimeout);
                    using (linkedCts.Token.Register(() => tcs.TrySetCanceled()))
                    {
                        await tcs.Task.ConfigureAwait(false);
                    }
                }
            }
            finally
            {
                lock (_pingLock)
                {
                    _pendingPingTcs = null;
                }
            }
        }

        /// <summary>
        /// 当解包器解析出 MQTT 报文时的统一分发处理。
        /// </summary>
        protected virtual void Unpacker_OnDataParsed(object sender, DataParsedEventArgs<MqttPacket> e)
        {
            UpdateLastReceivedTimestamp();
            var packet = e.Data;

            SafelyInvokeCallback(() =>
            {
                OnPacketReceived?.Invoke(this, new MqttPacketReceivedEventArgs<MqttPacket>(packet));
            });

            switch (packet)
            {
                case MqttConnAckPacket connAck:
                    lock (_connectLock)
                    {
                        _connectTcs?.TrySetResult(connAck);
                    }
                    break;

                case MqttPubAckPacket pubAck:
                    if (_pendingPubAcks.TryGetValue(pubAck.PacketId, out var pubAckTcs))
                    {
                        pubAckTcs.TrySetResult(pubAck);
                    }
                    break;

                case MqttPubRecPacket pubRec:
                    // QoS 2 收到 PUBREC，客户端应答 PUBREL
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            var pubRel = new MqttPubRelPacket
                            {
                                PacketId = pubRec.PacketId,
                                ReasonCode = MqttPubRelReasonCode.Success
                            };
                            await SendPacketAsync(pubRel).ConfigureAwait(false);
                        }
                        catch { }
                    });
                    break;

                case MqttPubRelPacket pubRel:
                    // QoS 2 阶段：收到 Broker 的 PUBREL，清理去重缓存并应答 PUBCOMP
                    _incomingQos2PendingPackets.TryRemove(pubRel.PacketId, out _);

                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            var pubComp = new MqttPubCompPacket
                            {
                                PacketId = pubRel.PacketId,
                                ReasonCode = MqttPubCompReasonCode.Success
                            };
                            await SendPacketAsync(pubComp).ConfigureAwait(false);
                        }
                        catch { }
                    });
                    break;

                case MqttPubCompPacket pubComp:
                    if (_pendingPubComps.TryGetValue(pubComp.PacketId, out var pubCompTcs))
                    {
                        pubCompTcs.TrySetResult(pubComp);
                    }
                    break;

                case MqttSubAckPacket subAck:
                    if (_pendingSubAcks.TryGetValue(subAck.PacketId, out var subAckTcs))
                    {
                        subAckTcs.TrySetResult(subAck);
                    }
                    break;

                case MqttUnsubAckPacket unsubAck:
                    if (_pendingUnsubAcks.TryGetValue(unsubAck.PacketId, out var unsubAckTcs))
                    {
                        unsubAckTcs.TrySetResult(unsubAck);
                    }
                    break;

                case MqttPingRespPacket pingResp:
                    lock (_pingLock)
                    {
                        _pendingPingTcs?.TrySetResult(pingResp);
                    }
                    break;

                case MqttDisconnectPacket disconnect:
                    lock (_connectLock)
                    {
                        _explicitDisconnectReason = disconnect.ReasonCode;
                        _explicitDisconnectReasonString = disconnect.Properties?.ReasonString;
                    }

                    _ = Task.Run(async () =>
                    {
                        await DisconnectMqttAsync(disconnect.ReasonCode, disconnect.Properties?.ReasonString).ConfigureAwait(false);
                    });
                    break;

                case MqttPublishPacket publish:
                    HandleInboundPublishPacket(publish);
                    break;
            }
        }

        private void HandleInboundPublishPacket(MqttPublishPacket publish)
        {
            // QoS 确认响应
            if (publish.QoS == MqttQualityOfServiceLevel.AtLeastOnce)
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        var pubAck = new MqttPubAckPacket
                        {
                            PacketId = publish.PacketId,
                            ReasonCode = MqttPubAckReasonCode.Success
                        };
                        await SendPacketAsync(pubAck).ConfigureAwait(false);
                    }
                    catch { }
                });
            }
            else if (publish.QoS == MqttQualityOfServiceLevel.ExactlyOnce)
            {
                bool isDuplicate = !_incomingQos2PendingPackets.TryAdd(publish.PacketId, 0);

                _ = Task.Run(async () =>
                {
                    try
                    {
                        var pubRec = new MqttPubRecPacket
                        {
                            PacketId = publish.PacketId,
                            ReasonCode = MqttPubRecReasonCode.Success
                        };
                        await SendPacketAsync(pubRec).ConfigureAwait(false);
                    }
                    catch { }
                });

                if (isDuplicate)
                {
                    // 重复接收的 QoS 2 报文：MQTT v5 规范要求回复 PUBREC，但严禁向业务层再次分发消息
                    return;
                }
            }

            // 派发给应用消息事件与主题路由
            var appMessage = new MqttApplicationMessage(publish.Topic, publish.Payload, publish.QoS, publish.Retain)
            {
                Dup = publish.Dup,
                Properties = publish.Properties
            };

            SafelyInvokeCallback(() =>
            {
                OnApplicationMessageReceived?.Invoke(this, new MqttApplicationMessageReceivedEventArgs(appMessage));
            });

            var handlers = Router.MatchHandlers(publish.Topic);
            if (handlers != null && handlers.Count > 0)
            {
                foreach (var handler in handlers)
                {
                    if (handler != null)
                    {
                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                await handler(appMessage).ConfigureAwait(false);
                            }
                            catch (Exception ex)
                            {
                                RaiseExceptionOccurs(this, new ExceptionOccursEventArgs(ex));
                            }
                        });
                    }
                }
            }
        }
    }
}
