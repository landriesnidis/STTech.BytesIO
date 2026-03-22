using System;
using System.Threading.Tasks;
using System.Timers;

namespace STTech.BytesIO.Core
{
    /// <summary>
    /// 尝试连接插件
    /// 如果连接失败，则隔一段时间后再次尝试。
    /// </summary>
    public class TryConnectPlugin : BytesClientPlugin
    {
        private Timer _timer;
        private int _delay;
        private int _maxTimes;
        private int _remainingTimes;
        private Action<BytesClient> _connectFailedHandler;

        /// <summary>
        /// 尝试连接延时（毫秒）
        /// </summary>
        public int Delay => _delay;

        /// <summary>
        /// 最大尝试次数（0 表示无限）
        /// </summary>
        public int MaxTimes => _maxTimes;

        /// <summary>
        /// 配置尝试连接参数并立即开始尝试
        /// </summary>
        /// <param name="delay">重连延时（毫秒），0 或负数表示取消</param>
        /// <param name="times">尝试次数，0 表示无限</param>
        /// <param name="connectFailedHandler">尝试最终失败回调</param>
        public void Configure(int delay, int times = 0, Action<BytesClient> connectFailedHandler = null)
        {
            _delay = delay;
            _maxTimes = times;
            _connectFailedHandler = connectFailedHandler;

            StopTimer();

            if (delay <= 0)
            {
                IsEnabled = false;
                return;
            }

            IsEnabled = true;
            _remainingTimes = times;

            _timer = new Timer(delay);
            _timer.Elapsed += OnTimerElapsed;
            _timer.Enabled = true;

            // 立即尝试第一次连接
            if (Client != null && !Client.IsConnected)
            {
                Client.ConnectAsync();
            }
        }

        private void OnTimerElapsed(object sender, ElapsedEventArgs e)
        {
            if (Client == null) return;

            if (Client.IsConnected)
            {
                Cancel();
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
                    var handler = _connectFailedHandler;
                    Cancel();
                    handler?.Invoke(Client);
                }
            }
            else
            {
                Client.ConnectAsync();
            }
        }

        /// <inheritdoc/>
        protected internal override void OnConnectedSuccessfully(ConnectedSuccessfullyEventArgs e)
        {
            Cancel();
        }

        /// <inheritdoc/>
        protected internal override void OnConnectionFailed(ConnectionFailedEventArgs e)
        {
            // 连接参数错误时停止尝试
            if (e.ErrorCode == ConnectErrorCode.ConnectionParameterError)
            {
                var handler = _connectFailedHandler;
                Cancel();
                handler?.Invoke(Client);
            }
        }

        /// <summary>
        /// 取消尝试连接
        /// </summary>
        public void Cancel()
        {
            StopTimer();
            IsEnabled = false;

            // 从客户端移除自身
            Client?.RemovePlugin(this);
        }

        private void StopTimer()
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
            StopTimer();
            _connectFailedHandler = null;
        }
    }

    /// <summary>
    /// 尝试连接扩展方法（保持 API 兼容）
    /// </summary>
    public static class TryConnectPluginExtensions
    {
        /// <summary>
        /// 取消持续尝试重连的任务
        /// </summary>
        /// <param name="client"></param>
        public static void TryConnectCancel(this BytesClient client)
        {
            client.TryConnect(0, 0);
        }

        /// <summary>
        /// 尝试连接，如果连接失败则隔一段时间后再次尝试
        /// </summary>
        /// <param name="client"></param>
        /// <param name="delay">重连延时</param>
        /// <param name="times">尝试连接次数</param>
        /// <param name="connectFailedHandler">连接失败处理回调</param>
        public static void TryConnect(this BytesClient client, int delay, int times = 0, Action<BytesClient> connectFailedHandler = null)
        {
            var plugin = client.GetPlugin<TryConnectPlugin>();

            if (delay <= 0)
            {
                if (plugin != null)
                {
                    plugin.Cancel();
                    plugin.Dispose();
                }
                return;
            }

            if (client.IsConnected)
            {
                return;
            }

            if (plugin == null)
            {
                plugin = new TryConnectPlugin();
                client.AddPlugin(plugin);
            }

            plugin.Configure(delay, times, connectFailedHandler);
        }
    }
}
