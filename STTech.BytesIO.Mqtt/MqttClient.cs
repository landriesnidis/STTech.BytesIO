using STTech.BytesIO.Core;
using STTech.BytesIO.Core.Component;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace STTech.BytesIO.Mqtt
{
    /// <summary>
    /// MQTT 客户端基类。
    /// 基于 VirtualClient 实现通信解耦，支持 MQTT v5.0 协议标准。
    /// </summary>
    public abstract partial class MqttClient : VirtualClient, IUnpackerSupport<MqttPacket>
    {
        private volatile bool _isMqttConnected;
        private readonly object _connectLock = new object();
        private TaskCompletionSource<MqttConnAckPacket> _connectTcs;
        private MqttDisconnectReasonCode? _explicitDisconnectReason;
        private string _explicitDisconnectReasonString;

        // 待确认响应字典 (PacketId -> TaskCompletionSource)
        private readonly ConcurrentDictionary<ushort, TaskCompletionSource<MqttPubAckPacket>> _pendingPubAcks =
            new ConcurrentDictionary<ushort, TaskCompletionSource<MqttPubAckPacket>>();
        private readonly ConcurrentDictionary<ushort, TaskCompletionSource<MqttPubCompPacket>> _pendingPubComps =
            new ConcurrentDictionary<ushort, TaskCompletionSource<MqttPubCompPacket>>();
        private readonly ConcurrentDictionary<ushort, TaskCompletionSource<MqttSubAckPacket>> _pendingSubAcks =
            new ConcurrentDictionary<ushort, TaskCompletionSource<MqttSubAckPacket>>();
        private readonly ConcurrentDictionary<ushort, TaskCompletionSource<MqttUnsubAckPacket>> _pendingUnsubAcks =
            new ConcurrentDictionary<ushort, TaskCompletionSource<MqttUnsubAckPacket>>();
        private TaskCompletionSource<MqttPingRespPacket> _pendingPingTcs;

        // QoS 2 入站未决报文跟踪集合（用于接收端报文去重）
        private readonly ConcurrentDictionary<ushort, byte> _incomingQos2PendingPackets =
            new ConcurrentDictionary<ushort, byte>();

        internal PacketIdManager PacketIdManager { get; } = new PacketIdManager();

        /// <summary>
        /// 主题路由分发引擎。
        /// </summary>
        public TopicRouter Router { get; } = new TopicRouter();

        /// <summary>
        /// MQTT 协议版本。
        /// </summary>
        public MqttProtocolVersion ProtocolVersion { get; set; } = MqttProtocolVersion.V5;

        /// <summary>
        /// 客户端配置选项。
        /// </summary>
        public MqttClientOptions Options { get; set; } = new MqttClientOptions();

        /// <summary>
        /// 服务端当前是否存在既有会话。
        /// </summary>
        public bool IsSessionPresent { get; private set; }

        /// <summary>
        /// MQTT 协议解包器。
        /// </summary>
        public Unpacker<MqttPacket> Unpacker { get; }

        /// <summary>
        /// 当前是否已处于 MQTT 协议连接就绪状态。
        /// </summary>
        public override bool IsConnected => (InnerClient?.IsConnected ?? false) && _isMqttConnected;

        #region Events

        /// <summary>
        /// 当接收到 MQTT 应用消息时发生。
        /// </summary>
        public event EventHandler<MqttApplicationMessageReceivedEventArgs> OnApplicationMessageReceived;

        /// <summary>
        /// 当 MQTT 协议握手建立成功时发生。
        /// </summary>
        public event EventHandler<MqttConnectedEventArgs> OnMqttConnectedSuccessfully;

        /// <summary>
        /// 当 MQTT 会话断开时发生。
        /// </summary>
        public event EventHandler<MqttDisconnectedEventArgs> OnMqttDisconnected;

        /// <summary>
        /// 当接收到任何 MQTT 控制报文时发生。
        /// </summary>
        public event EventHandler<MqttPacketReceivedEventArgs<MqttPacket>> OnPacketReceived;

        #endregion

        /// <summary>
        /// 构造 MQTT 客户端。
        /// </summary>
        /// <param name="client">底层的通信客户端</param>
        /// <param name="protocolVersion">协议版本</param>
        protected MqttClient(BytesClient client, MqttProtocolVersion protocolVersion = MqttProtocolVersion.V5) : base(client)
        {
            ProtocolVersion = protocolVersion;
            Unpacker = new MqttUnpacker(this, protocolVersion);
            this.BindUnpacker(Unpacker);
            Unpacker.OnDataParsed += Unpacker_OnDataParsed;
        }

        #region Lifecycle Event Overrides

        /// <inheritdoc/>
        protected override void RaiseConnectedSuccessfully(object sender, ConnectedSuccessfullyEventArgs e)
        {
            // 只有当 MQTT 握手成功（_isMqttConnected == true）时，才向外冒泡 BytesIO 的 ConnectedSuccessfully 事件
            if (_isMqttConnected)
            {
                base.RaiseConnectedSuccessfully(sender, e);
            }
        }

        /// <inheritdoc/>
        protected override void RaiseDisconnected(object sender, DisconnectedEventArgs e)
        {
            // 若服务端在发送 DISCONNECT 报文后立即物理断开 TCP，接收队列中的 DISCONNECT 报文可能正在排队解包。
            // 轮询等待极短时间以优先提取 DISCONNECT 报文中的准确 ReasonCode
            if (_explicitDisconnectReason == null)
            {
                for (int i = 0; i < 5 && _explicitDisconnectReason == null; i++)
                {
                    Thread.Sleep(10);
                }
            }

            bool wasConnected;
            MqttDisconnectReasonCode reasonCode;
            string reasonString;

            lock (_connectLock)
            {
                wasConnected = _isMqttConnected;
                _isMqttConnected = false;
                reasonCode = _explicitDisconnectReason ?? MqttDisconnectReasonCode.UnspecifiedError;
                reasonString = _explicitDisconnectReasonString ?? e?.Exception?.Message;
                _explicitDisconnectReason = null;
                _explicitDisconnectReasonString = null;
            }

            StopKeepAlive();
            PacketIdManager.Reset();
            FailAllPendingRequests(new MqttCommunicationException("网络连接已断开。", e?.Exception));

            if (wasConnected)
            {
                SafelyInvokeCallback(() =>
                {
                    OnMqttDisconnected?.Invoke(this, new MqttDisconnectedEventArgs(reasonCode, reasonString));
                });
            }

            base.RaiseDisconnected(sender, e);
        }

        private void FailAllPendingRequests(Exception ex)
        {
            lock (_connectLock)
            {
                _connectTcs?.TrySetException(ex);
                _connectTcs = null;
            }

            lock (_pingLock)
            {
                _pendingPingTcs?.TrySetException(ex);
                _pendingPingTcs = null;
            }

            foreach (var kvp in _pendingPubAcks)
            {
                kvp.Value.TrySetException(ex);
            }
            _pendingPubAcks.Clear();

            foreach (var kvp in _pendingPubComps)
            {
                kvp.Value.TrySetException(ex);
            }
            _pendingPubComps.Clear();

            foreach (var kvp in _pendingSubAcks)
            {
                kvp.Value.TrySetException(ex);
            }
            _pendingSubAcks.Clear();

            foreach (var kvp in _pendingUnsubAcks)
            {
                kvp.Value.TrySetException(ex);
            }
            _pendingUnsubAcks.Clear();

            _incomingQos2PendingPackets.Clear();
        }

        #endregion

        #region Connect & Disconnect

        /// <inheritdoc/>
        public override ConnectResult Connect(ConnectArgument argument = null)
        {
            return ConnectAsync(argument).GetAwaiter().GetResult();
        }

        /// <inheritdoc/>
        public override async Task<ConnectResult> ConnectAsync(ConnectArgument argument = null)
        {
            var mqttResult = await ConnectMqttAsync(Options).ConfigureAwait(false);
            return new ConnectResult(mqttResult.IsSuccess ? ConnectErrorCode.None : ConnectErrorCode.Timeout, mqttResult.Exception);
        }

        /// <summary>
        /// 连接并执行 MQTT 协议握手 (CONNECT)。
        /// </summary>
        public MqttConnectResult ConnectMqtt(MqttClientOptions options = null, int timeout = 10000)
        {
            using (var cts = new CancellationTokenSource(timeout))
            {
                return ConnectMqttAsync(options, cts.Token).GetAwaiter().GetResult();
            }
        }

        /// <summary>
        /// 异步连接并执行 MQTT 协议握手 (CONNECT)。
        /// </summary>
        public async Task<MqttConnectResult> ConnectMqttAsync(MqttClientOptions options = null, CancellationToken cancellationToken = default)
        {
            if (options != null)
            {
                Options = options;
            }

            var result = new MqttConnectResult();

            try
            {
                // 1. 若底层的 TCP 尚未建立连接，先发起底层传输连接
                if (!InnerClient.IsConnected)
                {
                    var tcpResult = await InnerClient.ConnectAsync(new ConnectArgument
                    {
                        Timeout = (int)Options.CommunicationTimeout.TotalMilliseconds
                    }).ConfigureAwait(false);

                    if (!tcpResult.IsSuccess)
                    {
                        result.ReasonCode = MqttConnectReasonCode.ServerUnavailable;
                        result.Exception = tcpResult.Exception ?? new MqttCommunicationException("底层传输层连接失败。");
                        return result;
                    }
                }

                // 2. 发起 MQTT CONNECT 握手
                var connectPacket = new MqttConnectPacket
                {
                    ProtocolVersion = ProtocolVersion,
                    ClientId = string.IsNullOrEmpty(Options.ClientId) ? Guid.NewGuid().ToString("N") : Options.ClientId,
                    CleanStart = Options.CleanStart,
                    KeepAlive = Options.KeepAlive,
                    UserName = Options.UserName,
                    Password = Options.Password,
                    HasWill = Options.HasWill,
                    WillTopic = Options.WillTopic,
                    WillPayload = Options.WillPayload,
                    WillQoS = Options.WillQoS,
                    WillRetain = Options.WillRetain,
                    WillProperties = Options.WillProperties,
                    Properties = Options.Properties
                };

                if (Options.SessionExpiryInterval.HasValue)
                {
                    connectPacket.Properties.SessionExpiryInterval = Options.SessionExpiryInterval.Value;
                }

                var tcs = new TaskCompletionSource<MqttConnAckPacket>(TaskCreationOptions.RunContinuationsAsynchronously);
                lock (_connectLock)
                {
                    _connectTcs = tcs;
                }

                await SendPacketAsync(connectPacket, cancellationToken).ConfigureAwait(false);

                // 等待 CONNACK 响应
                using (var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
                {
                    linkedCts.CancelAfter(Options.CommunicationTimeout);
                    using (linkedCts.Token.Register(() => tcs.TrySetCanceled()))
                    {
                        var connAck = await tcs.Task.ConfigureAwait(false);
                        result.ReasonCode = connAck.ReasonCode;
                        result.SessionPresent = connAck.SessionPresent;
                        result.Properties = connAck.Properties;
                        IsSessionPresent = connAck.SessionPresent;

                        if (connAck.ReasonCode == MqttConnectReasonCode.Success)
                        {
                            _isMqttConnected = true;

                            // 协商心跳时长
                            ushort keepAliveSeconds = connAck.Properties.ServerKeepAlive ?? Options.KeepAlive;
                            StartKeepAlive(keepAliveSeconds);

                            // 自动补发已注册的主题订阅
                            if (Options.AutoResubscribe && (!IsSessionPresent || Options.CleanStart))
                            {
                                _ = Task.Run(async () => await ResubscribeAllAsync().ConfigureAwait(false));
                            }

                            SafelyInvokeCallback(() =>
                            {
                                OnMqttConnectedSuccessfully?.Invoke(this, new MqttConnectedEventArgs(result));
                                RaiseConnectedSuccessfully(this, new ConnectedSuccessfullyEventArgs());
                            });
                        }
                        else
                        {
                            _isMqttConnected = false;
                            result.Exception = new MqttCommunicationException($"MQTT 握手被拒绝，原因码: {connAck.ReasonCode}");
                            await InnerClient.DisconnectAsync().ConfigureAwait(false);
                            RaiseConnectionFailed(this, new ConnectionFailedEventArgs(result.Exception));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _isMqttConnected = false;
                result.ReasonCode = MqttConnectReasonCode.ServerUnavailable;
                result.Exception = ex;
                try { await InnerClient.DisconnectAsync().ConfigureAwait(false); } catch { }
                RaiseConnectionFailed(this, new ConnectionFailedEventArgs(ex));
            }
            finally
            {
                lock (_connectLock)
                {
                    _connectTcs = null;
                }
            }

            return result;
        }

        /// <inheritdoc/>
        public override DisconnectResult Disconnect(DisconnectArgument argument = null)
        {
            return DisconnectAsync(argument).GetAwaiter().GetResult();
        }

        /// <inheritdoc/>
        public override async Task<DisconnectResult> DisconnectAsync(DisconnectArgument argument = null)
        {
            await DisconnectMqttAsync(MqttDisconnectReasonCode.NormalDisconnection).ConfigureAwait(false);
            return new DisconnectResult(DisconnectErrorCode.None);
        }

        /// <summary>
        /// 发送 DISCONNECT 报文并关闭连接。
        /// </summary>
        public async Task DisconnectMqttAsync(MqttDisconnectReasonCode reasonCode = MqttDisconnectReasonCode.NormalDisconnection, string reasonString = null, CancellationToken cancellationToken = default)
        {
            StopKeepAlive();

            if (IsConnected)
            {
                try
                {
                    var disconnectPacket = new MqttDisconnectPacket { ReasonCode = reasonCode };
                    if (!string.IsNullOrEmpty(reasonString))
                    {
                        disconnectPacket.Properties.ReasonString = reasonString;
                    }
                    await SendPacketAsync(disconnectPacket, cancellationToken).ConfigureAwait(false);
                }
                catch { }
            }

            lock (_connectLock)
            {
                _explicitDisconnectReason = reasonCode;
                _explicitDisconnectReasonString = reasonString;
            }

            if (InnerClient != null && InnerClient.IsConnected)
            {
                await InnerClient.DisconnectAsync().ConfigureAwait(false);
            }
            else
            {
                RaiseDisconnected(this, new DisconnectedEventArgs());
            }
        }

        /// <inheritdoc/>
        public override void Dispose()
        {
            StopKeepAlive();

            if (Unpacker != null)
            {
                Unpacker.OnDataParsed -= Unpacker_OnDataParsed;
            }

            lock (_connectLock)
            {
                _isMqttConnected = false;
            }

            PacketIdManager.Reset();
            FailAllPendingRequests(new ObjectDisposedException(nameof(MqttClient)));
            Router.Clear();

            base.Dispose();
        }

        #endregion

        /// <summary>
        /// 发送指定 MQTT 报文。
        /// </summary>
        public async Task SendPacketAsync(MqttPacket packet, CancellationToken cancellationToken = default)
        {
            if (packet == null) throw new ArgumentNullException(nameof(packet));
            byte[] data = packet.Encode();
            await InnerClient.SendAsync(data).ConfigureAwait(false);
            UpdateLastSentTimestamp();
        }
    }

    /// <summary>
    /// 支持指定内部通信客户端类型的 MQTT 泛型客户端基类。
    /// </summary>
    /// <typeparam name="TClient">内部通信客户端类型</typeparam>
    public abstract partial class MqttClient<TClient> : MqttClient where TClient : BytesClient
    {
        /// <summary>
        /// 获取强类型的内部通信客户端。
        /// </summary>
        public new TClient InnerClient => (TClient)base.InnerClient;

        /// <summary>
        /// 构造 MQTT 泛型客户端。
        /// </summary>
        protected MqttClient(TClient client, MqttProtocolVersion version = MqttProtocolVersion.V5) : base(client, version)
        {
        }
    }
}
