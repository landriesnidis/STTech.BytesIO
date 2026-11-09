using System;
using System.Collections.Concurrent;
using System.Threading;
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

        private System.Timers.Timer _heartbeatTimer;
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

            _heartbeatTimer = new System.Timers.Timer(interval) { AutoReset = true };
            _heartbeatTimer.Elapsed += (s, e) =>
            {
                if (Client != null && Client.IsConnected)
                {
                    try
                    {
                        _sendHeartbeatHandler?.Invoke(Client);
                    }
                    catch (Exception ex)
                    {
                        Client?.RaiseExceptionOccurs(ex);
                    }
                }
            };

            if (Client != null && Client.IsConnected)
            {
                _heartbeatTimer.Start();
            }
        }

        private void StopHeartbeatTimer()
        {
            if (_heartbeatTimer != null)
            {
                try { _heartbeatTimer.Stop(); } catch { }
                try { _heartbeatTimer.Dispose(); } catch { }
                _heartbeatTimer = null;
            }
        }

        // ==================== 心跳超时检测 ====================

        private System.Timers.Timer _timeoutCheckTimer;
        private long _lastReceivedTimeMs = MonotonicClock.NowMs;

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

            int checkInterval = Math.Max(100, Math.Min(timeout / 2, 1000));
            if (_timeoutCheckTimer == null)
            {
                _timeoutCheckTimer = new System.Timers.Timer(checkInterval) { AutoReset = true };
                _timeoutCheckTimer.Elapsed += OnTimeoutCheckTimerElapsed;
            }
            else
            {
                _timeoutCheckTimer.Interval = checkInterval;
            }

            if (Client != null && Client.IsConnected)
            {
                StartTimeoutTimer();
            }
        }

        private void OnTimeoutCheckTimerElapsed(object sender, ElapsedEventArgs e)
        {
            if (Client == null || !Client.IsConnected || HeartbeatTimeout <= 0)
            {
                return;
            }

            long elapsed = MonotonicClock.NowMs - Interlocked.Read(ref _lastReceivedTimeMs);
            if (elapsed >= HeartbeatTimeout)
            {
                OnHeartbeatTimeout();
            }
        }

        private void OnHeartbeatTimeout()
        {
            PauseTimeoutTimer();
            Client?.Disconnect(new DisconnectArgument(DisconnectionReasonCode.Timeout));
        }

        /// <summary>
        /// 启动超时检测（连接成功后调用）
        /// </summary>
        private void StartTimeoutTimer()
        {
            if (HeartbeatTimeout > 0 && _timeoutCheckTimer != null)
            {
                Interlocked.Exchange(ref _lastReceivedTimeMs, MonotonicClock.NowMs);
                _timeoutCheckTimer.Start();
            }
        }

        /// <summary>
        /// 暂停超时检测
        /// </summary>
        private void PauseTimeoutTimer()
        {
            _timeoutCheckTimer?.Stop();
        }

        /// <summary>
        /// 销毁超时检测定时器
        /// </summary>
        private void DestroyTimeoutTimer()
        {
            if (_timeoutCheckTimer != null)
            {
                try { _timeoutCheckTimer.Stop(); } catch { }
                try { _timeoutCheckTimer.Dispose(); } catch { }
                _timeoutCheckTimer = null;
            }
        }

        // ==================== 生命周期 ====================

        /// <inheritdoc/>
        protected internal override void OnConnectedSuccessfully(ConnectedSuccessfullyEventArgs e)
        {
            // 连接成功后启动超时计时器与心跳发送
            Interlocked.Exchange(ref _lastReceivedTimeMs, MonotonicClock.NowMs);
            StartTimeoutTimer();

            if (HeartbeatInterval > 0 && _heartbeatTimer != null)
            {
                _heartbeatTimer.Start();
            }
        }

        /// <inheritdoc/>
        protected internal override void OnDisconnected(DisconnectedEventArgs e)
        {
            // 断开连接后暂停超时检测与心跳发送，彻底消除后台空转
            PauseTimeoutTimer();
            _heartbeatTimer?.Stop();
        }

        /// <inheritdoc/>
        protected internal override void OnDataReceived(DataReceivedEventArgs e)
        {
            // 收到数据时轻量原子更新单调时间戳，杜绝每包高频重启系统定时器振荡
            if (HeartbeatTimeout > 0)
            {
                Interlocked.Exchange(ref _lastReceivedTimeMs, MonotonicClock.NowMs);
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
    /// 超时计时器（向后兼容保留）
    /// </summary>
    internal class TimeoutTimer : System.Timers.Timer
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
