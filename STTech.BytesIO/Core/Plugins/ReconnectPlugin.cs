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
                _timer = new Timer(delay);
                _timer.Elapsed += OnTimerElapsed;
            }
            else
            {
                _timer.Interval = delay;
            }
        }

        private void OnTimerElapsed(object sender, ElapsedEventArgs e)
        {
            if (Client == null || Client.IsConnected)
            {
                PauseTimer();
                return;
            }

            if (_maxTimes > 0)
            {
                if (_remainingTimes > 0)
                {
                    _remainingTimes--;
                    Client.ConnectAsync();
                }
                else
                {
                    PauseTimer();
                    _reconnectFailedHandler?.Invoke(Client);
                }
            }
            else
            {
                Client.ConnectAsync();
            }
        }

        /// <inheritdoc/>
        protected internal override void OnDisconnected(DisconnectedEventArgs e)
        {
            // 非主动断开时启动重连
            if (!e.IsActively && IsEnabled)
            {
                if (_timer == null && _delay > 0)
                {
                    _timer = new Timer(_delay);
                    _timer.Elapsed += OnTimerElapsed;
                }

                if (_timer != null)
                {
                    _remainingTimes = _maxTimes;
                    _timer.Enabled = true;
                }
            }
        }

        /// <inheritdoc/>
        protected internal override void OnConnectedSuccessfully(ConnectedSuccessfullyEventArgs e)
        {
            PauseTimer();
            _remainingTimes = _maxTimes;
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
            if (_timer != null)
            {
                _timer.Enabled = false;
                _timer.Elapsed -= OnTimerElapsed;
                _timer.Dispose();
                _timer = null;
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
