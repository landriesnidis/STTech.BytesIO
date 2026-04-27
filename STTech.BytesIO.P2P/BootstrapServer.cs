using Microsoft.Extensions.DependencyInjection;
using Multiformats.Address;
using Nethermind.Libp2p;
using Nethermind.Libp2p.Core;
using Nethermind.Libp2p.Core.Discovery;
using STTech.BytesIO.Core;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace STTech.BytesIO.P2P
{
    /// <summary>
    /// P2P 引导节点（种子节点）
    /// <para>
    /// 提供基于 Libp2p 的网络引导服务，作为 P2P 网络的入口节点。
    /// 其他 <see cref="PeerClient"/> 可以通过此节点加入网络并发现其他对等节点。
    /// </para>
    /// <para>
    /// 通过 <see cref="Options"/> 中的布尔开关灵活控制是否启用 TCP/QUIC 传输、
    /// DHT 节点发现、Relay 中继穿透、PubSub 发布订阅等功能。
    /// </para>
    /// </summary>
    public class BootstrapServer : IBytesServer
    {
        private ServiceProvider? _serviceProvider;
        private ILocalPeer? _localPeer;
        private CancellationTokenSource? _cts;
        private BytesIOProtocol? _protocol;

        /// <summary>
        /// 客户端列表
        /// </summary>
        private readonly ConcurrentDictionary<PeerClient, byte> _clients = new();

        /// <summary>
        /// 服务器状态锁
        /// </summary>
        private readonly object _stateLocker = new();

        // ===============================================================================
        // 
        //                                  属性
        // 
        // ===============================================================================

        /// <summary>
        /// P2P 节点配置选项
        /// </summary>
        [Category("P2P 配置")]
        [Description("引导节点的详细配置选项。")]
        public P2PNodeOptions Options { get; set; } = new();

        /// <summary>
        /// 服务器运行状态
        /// </summary>
        public ServerState State { get; private set; } = ServerState.Closed;

        /// <summary>
        /// 是否正在运行
        /// </summary>
        public bool IsRunning => State != ServerState.Closed;

        /// <summary>
        /// 是否正在监听
        /// </summary>
        public bool IsListening => State == ServerState.Listening;

        /// <summary>
        /// 本地节点的 PeerId
        /// </summary>
        [Category("P2P 属性")]
        [Description("本地节点的唯一标识符 (PeerId)。")]
        public string? LocalPeerId { get; private set; }

        /// <summary>
        /// 本地节点的监听地址列表
        /// </summary>
        [Category("P2P 属性")]
        [Description("本地节点当前正在监听的 Multiaddr 地址列表。")]
        public IReadOnlyList<string> ListenAddresses => _localPeer?.ListenAddresses
            .Select(a => a.ToString())
            .ToList()
            .AsReadOnly() ?? new List<string>().AsReadOnly();

        /// <summary>
        /// 当前在线的客户端列表
        /// </summary>
        public PeerClient[] Clients => _clients.Keys.ToArray();

        /// <summary>
        /// 当前在线的客户端数量
        /// </summary>
        public int ClientCount => _clients.Count;

        // ===============================================================================
        // 
        //                                  事件
        // 
        // ===============================================================================

        /// <summary>
        /// 有新的 Peer 连入时发生
        /// </summary>
        public event EventHandler<PeerConnectedEventArgs>? PeerConnected;

        /// <summary>
        /// Peer 断开连接时发生
        /// </summary>
        public event EventHandler<PeerDisconnectedEventArgs>? PeerDisconnected;

        /// <summary>
        /// 服务器已启动时发生
        /// </summary>
        public event EventHandler? Started;

        /// <summary>
        /// 服务器已关闭时发生
        /// </summary>
        public event EventHandler? Closed;

        /// <summary>
        /// 服务器已暂停监听事件（未实现）
        /// </summary>
        public event EventHandler? Paused;

        /// <summary>
        /// 发生异常时发生
        /// </summary>
        public event EventHandler<ExceptionOccursEventArgs>? OnExceptionOccurs;

        // ===============================================================================
        // 
        //                                  方法
        // 
        // ===============================================================================

        /// <summary>
        /// 异步启动引导节点
        /// </summary>
        public async Task StartAsync()
        {
            if (IsListening) return;

            lock (_stateLocker)
            {
                if (IsListening) return;

                _cts = new CancellationTokenSource();

                // 创建内部协议实例
                _protocol = new BytesIOProtocol();
                _protocol.OnChannelEstablished += OnChannelEstablished;

                // 构建服务容器
                _serviceProvider = PeerClient.BuildServiceProvider(Options, _protocol);

                var peerFactory = _serviceProvider.GetRequiredService<IPeerFactory>();

                // 创建本地节点
                Identity? identity = Options.IdentitySeed != null ? new Identity(Options.IdentitySeed) : null;
                _localPeer = peerFactory.Create(identity);
                LocalPeerId = _localPeer.Identity.PeerId.ToString();

                // 注册连接事件
                _localPeer.OnConnected += OnPeerConnectedAsync;

                State = ServerState.Listening;
            }

            // 构建监听地址
            var listenAddrs = new List<Multiaddress>();

            if (Options.EnableTcp)
            {
                listenAddrs.Add($"/ip4/{Options.ListenAddress}/tcp/{Options.TcpPort}");
            }

            if (Options.EnableQuic)
            {
                listenAddrs.Add($"/ip4/{Options.ListenAddress}/udp/{Options.QuicPort}/quic-v1");
            }

            // 如果没有任何传输被启用，默认启用 TCP
            if (listenAddrs.Count == 0)
            {
                listenAddrs.Add($"/ip4/{Options.ListenAddress}/tcp/{Options.TcpPort}");
            }

            // 开始监听
            await _localPeer.StartListenAsync(listenAddrs.ToArray(), _cts.Token).ConfigureAwait(false);

            // 连接到引导节点
            foreach (var bootstrapAddr in Options.BootstrapPeers)
            {
                try
                {
                    Multiaddress addr = bootstrapAddr;
                    await _localPeer.DialAsync(addr, _cts.Token).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    OnExceptionOccurs?.Invoke(this, new ExceptionOccursEventArgs(ex));
                }
            }

            Task.Run(() => Started?.Invoke(this, EventArgs.Empty));
        }

        /// <summary>
        /// 异步关闭引导节点
        /// </summary>
        public async Task CloseAsync()
        {
            if (!IsRunning) return;

            lock (_stateLocker)
            {
                if (!IsRunning) return;

                _cts?.Cancel();

                // 断开所有客户端
                var activeClients = _clients.Keys.ToList();
                _clients.Clear();
                foreach (var client in activeClients)
                {
                    try
                    {
                        client.Disconnect();
                    }
                    catch { }
                }

                State = ServerState.Closed;
            }

            // 清理 Libp2p 资源
            if (_localPeer != null)
            {
                try
                {
                    await _localPeer.DisposeAsync().ConfigureAwait(false);
                }
                catch { }
                _localPeer = null;
            }

            _serviceProvider?.Dispose();
            _serviceProvider = null;

            LocalPeerId = null;

            Task.Run(() => Closed?.Invoke(this, EventArgs.Empty));
        }

        /// <summary>
        /// 异步停止监听（Libp2p 中 Stop 和 Close 逻辑相近，此处直接调用 Close）
        /// </summary>
        public Task StopAsync() => CloseAsync();

        /// <summary>
        /// 获取所有客户端
        /// </summary>
        public IEnumerable<BytesClient> GetClients() => _clients.Keys;

        /// <summary>
        /// 当远端节点连入时
        /// </summary>
        private Task OnPeerConnectedAsync(ISession newSession)
        {
            // Libp2p 层面的连接回调
            // 实际的字节流通道在 BytesIOProtocol.ListenAsync 中建立
            return Task.CompletedTask;
        }

        /// <summary>
        /// 当字节流通道建立时
        /// </summary>
        private void OnChannelEstablished(IChannel channel, ISessionContext context, bool isDialer)
        {
            if (isDialer) return; // 引导节点只处理被动接入

            try
            {
                // 包装为 PeerClient
                var client = new PeerClient(channel, context);

                // 注册断连事件
                client.OnDisconnected += (sender, e) =>
                {
                    if (sender is PeerClient pc && _clients.TryRemove(pc, out _))
                    {
                        Task.Run(() => PeerDisconnected?.Invoke(this, new PeerDisconnectedEventArgs(pc, e)));
                    }
                };

                _clients.TryAdd(client, 0);
                Task.Run(() => PeerConnected?.Invoke(this, new PeerConnectedEventArgs(client)));
            }
            catch (Exception ex)
            {
                OnExceptionOccurs?.Invoke(this, new ExceptionOccursEventArgs(ex));
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            CloseAsync().GetAwaiter().GetResult();
        }
    }

    // ===============================================================================
    // 
    //                              事件参数类
    // 
    // ===============================================================================

    /// <summary>
    /// Peer 已连接的事件参数
    /// </summary>
    public class PeerConnectedEventArgs : EventArgs
    {
        /// <summary>
        /// 已连接的客户端实例
        /// </summary>
        public PeerClient Client { get; }

        /// <summary>
        /// 构造函数
        /// </summary>
        public PeerConnectedEventArgs(PeerClient client)
        {
            Client = client;
        }
    }

    /// <summary>
    /// Peer 已断开连接的事件参数
    /// </summary>
    public class PeerDisconnectedEventArgs : EventArgs
    {
        /// <summary>
        /// 已断开的客户端实例
        /// </summary>
        public PeerClient Client { get; }

        /// <summary>
        /// 断开连接的详细信息
        /// </summary>
        public DisconnectedEventArgs DisconnectedInfo { get; }

        /// <summary>
        /// 构造函数
        /// </summary>
        public PeerDisconnectedEventArgs(PeerClient client, DisconnectedEventArgs info)
        {
            Client = client;
            DisconnectedInfo = info;
        }
    }
}
