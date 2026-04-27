using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace STTech.BytesIO.P2P
{
    /// <summary>
    /// P2P 节点配置选项
    /// </summary>
    [TypeConverter(typeof(ExpandableObjectConverter))]
    public class P2PNodeOptions
    {
        /// <summary>
        /// 是否启用 TCP 传输协议
        /// </summary>
        [Category("传输层")]
        [Description("是否启用 TCP 传输协议。")]
        public bool EnableTcp { get; set; } = true;

        /// <summary>
        /// 是否启用 QUIC 传输协议
        /// </summary>
        [Category("传输层")]
        [Description("是否启用 QUIC 传输协议。建议在网络环境较差或需要低延迟时开启。")]
        public bool EnableQuic { get; set; } = false;

        /// <summary>
        /// 是否启用 Kademlia DHT (分布式哈希表) 用于去中心化节点发现
        /// </summary>
        [Category("节点发现")]
        [Description("是否启用 Kademlia DHT 用于去中心化节点发现。")]
        public bool EnableDht { get; set; } = false;

        /// <summary>
        /// 是否启用 Relay 中继功能，允许 NAT 穿透
        /// </summary>
        [Category("网络穿透")]
        [Description("是否启用 Relay 中继功能，允许在 NAT 后的节点相互通信。")]
        public bool EnableRelay { get; set; } = false;

        /// <summary>
        /// 是否启用 PubSub (发布/订阅) 功能
        /// </summary>
        [Category("功能扩展")]
        [Description("是否启用 PubSub (发布/订阅) 功能。")]
        public bool EnablePubSub { get; set; } = false;

        /// <summary>
        /// 是否启用 mDNS 局域网节点发现
        /// </summary>
        [Category("节点发现")]
        [Description("是否启用 mDNS 局域网节点发现。适用于局域网内的自动对等发现。")]
        public bool EnableMdns { get; set; } = false;

        /// <summary>
        /// 是否强制使用明文传输（调试用途，生产环境请勿开启）
        /// </summary>
        [Category("安全性")]
        [Description("是否强制使用明文传输。注意：仅供调试使用，生产环境请务必保持关闭以保证通信安全。")]
        public bool EnforcePlaintext { get; set; } = false;

        /// <summary>
        /// TCP 监听端口
        /// </summary>
        [Category("网络配置")]
        [Description("TCP 监听端口。默认值为 4001。")]
        public int TcpPort { get; set; } = 4001;

        /// <summary>
        /// QUIC 监听端口
        /// </summary>
        [Category("网络配置")]
        [Description("QUIC 监听端口。默认值为 4002。")]
        public int QuicPort { get; set; } = 4002;

        /// <summary>
        /// 监听地址（默认监听所有 IPv4 地址）
        /// </summary>
        [Category("网络配置")]
        [Description("节点监听的本地 IP 地址。")]
        public string ListenAddress { get; set; } = "0.0.0.0";

        /// <summary>
        /// 已知的引导节点地址列表（Multiaddr 格式）
        /// <para>示例: /ip4/192.168.1.100/tcp/4001/p2p/QmXxx...</para>
        /// </summary>
        [Category("节点发现")]
        [Description("已知的引导节点 Multiaddr 地址列表。")]
        public List<string> BootstrapPeers { get; set; } = new();

        /// <summary>
        /// 固定身份的私钥种子（32字节）。
        /// 为 null 时将自动生成随机身份。
        /// </summary>
        [Category("安全性")]
        [Description("用于生成固定身份的私钥种子。若为 null，则每次启动生成随机身份。")]
        public byte[]? IdentitySeed { get; set; }
    }
}
