using System;
using System.Collections.Concurrent;
using System.Timers;

namespace STTech.BytesIO.Core
{
    /// <summary>
    /// 心跳插件
    /// 提供心跳发送和心跳超时检测功能。
    /// </summary>
    public class HeartbeatPlugin : BytesClientPlugin
    {
        // ==================== 心跳发送 ====================

        private Timer _heartbeatTimer;
        private Action<BytesClient> _sendHeartbeatHandler;

        /// <summary>
        /// 心跳发送间隔（毫秒）
        /// </summary>
        public int HeartbeatInterval { get; private set; }

        /// <summary>
        /// 配置心跳发送
        /// </summary>
        /// <param name="sendHandler">发送心跳包的回调</param>
        /// <param name="interval">心跳间隔（毫秒），0 或负数表示禁用</param>
        public void ConfigureHeartbeat(Action<BytesClient> sendHandler, int interval = 5000)
        {
            StopHeartbeatTimer();
            _sendHeartbeatHandler = sendHandler;
            HeartbeatInterval = interval;

            if (interval <= 0 || sendHandler == null)
            {
                return;
            }

            _heartbeatTimer = new Timer(interval);
            _heartbeatTimer.Elapsed += (s, e) =>
            {
                if (Client != null && Client.IsConnected)
                {
                    _sendHeartbeatHandler?.Invoke(Client);
                }
            };
            _heartbeatTimer.Enabled = true;
        }

        private void StopHeartbeatTimer()
        {
            if (_heartbeatTimer != null)
            {
                _heartbeatTimer.Stop();
                _heartbeatTimer.Dispose();
                _heartbeatTimer = null;
            }
        }

        // ==================== 心跳超时检测 ====================

        private TimeoutTimer _timeoutTimer;

        /// <summary>
        /// 心跳超时时间（毫秒）
        /// </summary>
        public int HeartbeatTimeout { get; private set; }

        /// <summary>
        /// 配置心跳超时检测
        /// </summary>
        /// <param name="timeout">超时时间（毫秒），0 或负数表示禁用</param>
        public void ConfigureTimeout(int timeout = 5000)
        {
            HeartbeatTimeout = timeout;

            if (timeout <= 0)
            {
                DestroyTimeoutTimer();
                return;
            }

            if (_timeoutTimer == null)
            {
                _timeoutTimer = new TimeoutTimer(timeout, OnHeartbeatTimeout);
            }
            else
            {
                _timeoutTimer.Interval = timeout;
            }

            if (Client != null && Client.IsConnected)
            {
                StartTimeoutTimer();
            }
        }

        private void OnHeartbeatTimeout()
        {
            Client?.Disconnect(new DisconnectArgument(DisconnectionReasonCode.Timeout));
            PauseTimeoutTimer();
        }

        /// <summary>
        /// 启动超时检测（连接成功后调用）
        /// </summary>
        private void StartTimeoutTimer()
        {
            if (HeartbeatTimeout > 0)
            {
                if (_timeoutTimer == null)
                {
                    _timeoutTimer = new TimeoutTimer(HeartbeatTimeout, OnHeartbeatTimeout);
                }
                _timeoutTimer.Interval = HeartbeatTimeout;
                _timeoutTimer.Start();
            }
        }

        /// <summary>
        /// 暂停超时检测
        /// </summary>
        private void PauseTimeoutTimer()
        {
            _timeoutTimer?.Stop();
        }

        /// <summary>
        /// 销毁超时检测定时器
        /// </summary>
        private void DestroyTimeoutTimer()
        {
            if (_timeoutTimer != null)
            {
                _timeoutTimer.Stop();
                _timeoutTimer.Dispose();
                _timeoutTimer = null;
            }
        }

        // ==================== 生命周期 ====================

        /// <inheritdoc/>
        protected internal override void OnConnectedSuccessfully(ConnectedSuccessfullyEventArgs e)
        {
            // 连接成功后启动超时计时器
            StartTimeoutTimer();
        }

        /// <inheritdoc/>
        protected internal override void OnDisconnected(DisconnectedEventArgs e)
        {
            // 断开连接后暂停超时检测（保留计时器实例，重连后可再次复用）
            PauseTimeoutTimer();
        }

        /// <inheritdoc/>
        protected internal override void OnDataReceived(DataReceivedEventArgs e)
        {
            // 收到数据时重置超时计时器
            if (HeartbeatTimeout > 0 && Client != null)
            {
                if (_timeoutTimer == null)
                {
                    StartTimeoutTimer();
                }
                _timeoutTimer?.Reset(HeartbeatTimeout);
            }
        }

        /// <inheritdoc/>
        public override void Dispose()
        {
            StopHeartbeatTimer();
            DestroyTimeoutTimer();
            _sendHeartbeatHandler = null;
        }
    }

    /// <summary>
    /// 心跳扩展方法（保持 API 兼容）
    /// </summary>
    public static class HeartbeatPluginExtensions
    {
        /// <summary>
        /// 启用心跳功能
        /// 定时执行发送心跳包的功能。
        /// </summary>
        /// <param name="client"></param>
        /// <param name="sendHeartbeatHandler">心跳发送回调</param>
        /// <param name="interval">心跳间隔（毫秒），小于等于0则关闭</param>
        public static void UseHeartbeat<T>(this T client, Action<T> sendHeartbeatHandler, int interval = 5000) where T : BytesClient
        {
            var plugin = client.GetPlugin<HeartbeatPlugin>();

            if (interval <= 0)
            {
                if (plugin != null)
                {
                    plugin.ConfigureHeartbeat(null, 0);
                    // 如果超时也没启用，则移除插件
                    if (plugin.HeartbeatTimeout <= 0)
                    {
                        client.RemovePlugin(plugin);
                        plugin.Dispose();
                    }
                }
                return;
            }

            if (plugin == null)
            {
                plugin = new HeartbeatPlugin();
                client.AddPlugin(plugin);
            }

            plugin.ConfigureHeartbeat(c => sendHeartbeatHandler((T)c), interval);
        }

        /// <summary>
        /// 启用心跳超时检查
        /// </summary>
        /// <param name="client"></param>
        /// <param name="timeout">超时时间（毫秒），小于等于0则关闭</param>
        public static void UseHeartbeatTimeout(this BytesClient client, int timeout = 5000)
        {
            var plugin = client.GetPlugin<HeartbeatPlugin>();

            if (timeout <= 0)
            {
                if (plugin != null)
                {
                    plugin.ConfigureTimeout(0);
                    // 如果心跳发送也没启用，则移除插件
                    if (plugin.HeartbeatInterval <= 0)
                    {
                        client.RemovePlugin(plugin);
                        plugin.Dispose();
                    }
                }
                return;
            }

            if (plugin == null)
            {
                plugin = new HeartbeatPlugin();
                client.AddPlugin(plugin);
            }

            plugin.ConfigureTimeout(timeout);
        }
    }

    /// <summary>
    /// 超时计时器
    /// </summary>
    internal class TimeoutTimer : Timer
    {
        private readonly int _timeout;
        private readonly Action _timeoutCallback;

        public TimeoutTimer(int timeout, Action timeoutCallback) : base(timeout)
        {
            _timeout = timeout;
            _timeoutCallback = timeoutCallback;
            Elapsed += (s, e) => OnElapsed(e);
        }

        public virtual void OnElapsed(ElapsedEventArgs e)
        {
            _timeoutCallback?.Invoke();
            Enabled = false;
        }

        public void Reset(double interval)
        {
            Stop();
            Interval = interval > 0 ? interval : (_timeout > 0 ? _timeout : 1);
            Start();
        }
    }
}
