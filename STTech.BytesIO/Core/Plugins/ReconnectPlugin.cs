using System;
using System.Threading.Tasks;
using System.Timers;

namespace STTech.BytesIO.Core
{
    /// <summary>
    /// 自动重连插件
    /// </summary>
    public class ReconnectPlugin : BytesClientPlugin
    {
        private readonly object _timerLock = new object();
        private Timer _timer;
        private int _delay;
        private int _maxTimes;
        private int _remainingTimes;
        private Action<BytesClient> _reconnectFailedHandler;

        /// <summary>
        /// 重连延时（毫秒）
        /// </summary>
        public int Delay => _delay;

        /// <summary>
        /// 最大重连次数（0 表示无限）
        /// </summary>
        public int MaxTimes => _maxTimes;

        /// <summary>
        /// 配置自动重连参数
        /// </summary>
        /// <param name="delay">重连延时（毫秒），0 或负数表示禁用</param>
        /// <param name="times">重连次数，0 表示无限</param>
        /// <param name="reconnectFailedHandler">重连失败回调</param>
        public void Configure(int delay, int times = 0, Action<BytesClient> reconnectFailedHandler = null)
        {
            lock (_timerLock)
            {
                _delay = delay;
                _maxTimes = times;
                _reconnectFailedHandler = reconnectFailedHandler;

                PauseTimer();

                if (delay <= 0)
                {
                    IsEnabled = false;
                    DestroyTimer();
                    return;
                }

                IsEnabled = true;
                _remainingTimes = times;

                if (_timer == null)
                {
                    _timer = new Timer(delay) { AutoReset = false };
                    _timer.Elapsed += OnTimerElapsed;
                }
                else
                {
                    _timer.Interval = delay;
                    _timer.AutoReset = false;
                }
            }
        }

        private async void OnTimerElapsed(object sender, ElapsedEventArgs e)
        {
            try
            {
                lock (_timerLock)
                {
                    if (Client == null || Client.IsConnected)
                    {
                        PauseTimer();
                        return;
                    }

                    if (_maxTimes > 0 && _remainingTimes <= 0)
                    {
                        PauseTimer();
                        SafelyInvokeReconnectFailedHandler();
                        return;
                    }

                    if (_maxTimes > 0)
                    {
                        _remainingTimes--;
                    }
                }

                try
                {
                    // 等待单次异步连接完成，杜绝未决连接被定时器周期性重入轰炸
                    var result = await Client.ConnectAsync().ConfigureAwait(false);
                    if (result.IsSuccess || (Client != null && Client.IsConnected))
                    {
                        lock (_timerLock)
                        {
                            // 仅当客户端依然处于连接状态时方可暂停定时器；
                            // 若在此期间客户端已被断开（例如被对端或测试快速断开，已触发 OnDisconnected 重新启动了定时器），
                            // 则绝不能再次调用 PauseTimer()，否则会误把新一轮重连定时器掐死。
                            if (Client != null && Client.IsConnected)
                            {
                                PauseTimer();
                            }
                        }
                        return;
                    }
                }
                catch
                {
                    // 忽略瞬态连接异常，由后续逻辑决定是否继续定时
                }

                // 若依然未连接且插件未被停用，启动下一轮单次重试
                lock (_timerLock)
                {
                    if (IsEnabled && Client != null && !Client.IsConnected)
                    {
                        if (_maxTimes > 0 && _remainingTimes <= 0)
                        {
                            PauseTimer();
                            SafelyInvokeReconnectFailedHandler();
                        }
                        else
                        {
                            _timer?.Start();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // 确保 async void 内部任何未知异常绝不逃逸致进程崩溃
                Client?.RaiseExceptionOccurs(ex);
            }
        }

        private void SafelyInvokeReconnectFailedHandler()
        {
            try
            {
                _reconnectFailedHandler?.Invoke(Client);
            }
            catch (Exception ex)
            {
                Client?.RaiseExceptionOccurs(ex);
            }
        }

        /// <inheritdoc/>
        protected internal override void OnDisconnected(DisconnectedEventArgs e)
        {
            // 非主动断开时启动重连
            if (!e.IsActively && IsEnabled)
            {
                lock (_timerLock)
                {
                    if (_timer == null && _delay > 0)
                    {
                        _timer = new Timer(_delay) { AutoReset = false };
                        _timer.Elapsed += OnTimerElapsed;
                    }

                    if (_timer != null)
                    {
                        _remainingTimes = _maxTimes;
                        _timer.Enabled = true;
                    }
                }
            }
        }

        /// <inheritdoc/>
        protected internal override void OnConnectedSuccessfully(ConnectedSuccessfullyEventArgs e)
        {
            lock (_timerLock)
            {
                PauseTimer();
                _remainingTimes = _maxTimes;
            }
        }

        private void PauseTimer()
        {
            if (_timer != null)
            {
                _timer.Enabled = false;
            }
        }

        private void DestroyTimer()
        {
            lock (_timerLock)
            {
                if (_timer != null)
                {
                    _timer.Enabled = false;
                    _timer.Elapsed -= OnTimerElapsed;
                    _timer.Dispose();
                    _timer = null;
                }
            }
        }

        /// <inheritdoc/>
        public override void Dispose()
        {
            DestroyTimer();
            _reconnectFailedHandler = null;
        }
    }

    /// <summary>
    /// 自动重连扩展方法（保持 API 兼容）
    /// </summary>
    public static class ReconnectPluginExtensions
    {
        /// <summary>
        /// 禁用自动重连功能
        /// </summary>
        /// <param name="client"></param>
        public static void DisableAutoReconnect(this BytesClient client)
        {
            client.UseAutoReconnect(0, 0);
        }

        /// <summary>
        /// 启动自动重连功能
        /// </summary>
        /// <param name="client"></param>
        /// <param name="delay">重连延时</param>
        /// <param name="times">重连次数</param>
        /// <param name="reconnectFailedHandler">重连失败处理回调</param>
        public static void UseAutoReconnect(this BytesClient client, int delay, int times = 0, Action<BytesClient> reconnectFailedHandler = null)
        {
            var plugin = client.GetPlugin<ReconnectPlugin>();

            if (delay <= 0)
            {
                // 禁用
                if (plugin != null)
                {
                    client.RemovePlugin(plugin);
                    plugin.Dispose();
                }
                return;
            }

            if (plugin == null)
            {
                plugin = new ReconnectPlugin();
                client.AddPlugin(plugin);
            }

            plugin.Configure(delay, times, reconnectFailedHandler);
        }
    }
}
