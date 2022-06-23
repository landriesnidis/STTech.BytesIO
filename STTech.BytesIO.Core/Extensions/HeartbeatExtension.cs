using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace STTech.BytesIO.Core
{

    /// <summary>
    /// 心跳超时扩展
    /// </summary>
    public static class HeartbeatExtension
    {
        private static Lazy<Dictionary<BytesClient, ClientReceiveTimeoutTimer>> lazyDictHeartbeatTimeoutTimers = new Lazy<Dictionary<BytesClient, ClientReceiveTimeoutTimer>>();
        private static Lazy<Dictionary<BytesClient, Timer>> lazyDictHeartbeatTimers = new Lazy<Dictionary<BytesClient, Timer>>();

        /// <summary>
        /// 启用心跳功能
        /// 定时执行发送心跳包的功能
        /// </summary>
        /// <param name="client"></param>
        /// <param name="sendHeartbeatHandler"></param>
        /// <param name="interval">如果设置的心跳时间间隔小于等于0，则说明关闭心跳功能</param>
        public static void UseHeartbeat<T>(this T client, Action<T> sendHeartbeatHandler, int interval = 5000) where T: BytesClient
        {
            // 使用时检查集合空值
            var dict = lazyDictHeartbeatTimers.Value;

            // 如果客户端已启用心跳功能则移除之前的定时器
            if (dict.ContainsKey(client))
            {
                var t = dict[client];
                t.Stop();
                t.Dispose();
                dict.Remove(client);
            }

            // 如果设置的心跳时间间隔小于等于0，则说明关闭心跳功能
            if (interval <= 0)
            {
                return;
            }

            // 加入新的心跳发送定时器
            Timer timerSendHeartbeat = new Timer(interval);
            timerSendHeartbeat.Elapsed += (s, e) =>
            {
                if (client.IsConnected)
                {
                    sendHeartbeatHandler.Invoke(client);
                }
            };
            dict.Add(client, timerSendHeartbeat);
            timerSendHeartbeat.Enabled = true;
        }

        /// <summary>
        /// 启用心跳超时检查
        /// 根据预设的心跳超时时间，当规定时间内未收到任何数据则判断为超时
        /// 备注：仅适用于任何接收数据皆可认定为心跳保持的情况使用，
        /// 若需特定心跳包则不可使用此方法。
        /// </summary>
        /// <param name="client"></param>
        /// <param name="timeout">如果设置的心跳超时时间小于等于0，则说明关闭心跳超时检测</param>
        public static void UseHeartbeatTimeout(this BytesClient client, int timeout = 5000)
        {
            // 使用时检查集合空值
            var dict = lazyDictHeartbeatTimeoutTimers.Value;

            // 如果客户端已启用心跳超时功能则移除之前的设置
            if (dict.ContainsKey(client))
            {
                var t = dict[client];
                t.Stop();
                t.Dispose();
                dict.Remove(client);
            }

            // 如果设置的心跳超时时间小于等于0，则说明关闭心跳超时检测
            if (timeout <= 0)
            {
                return;
            }

            // 加入新的心跳超时
            dict.Add(client, new ClientReceiveTimeoutTimer(client, timeout, () => client.Disconnect(Entity.DisconnectionReasonCode.Timeout)) { Enabled = true });
        }
    }

    /// <summary>
    /// 客户端接收超时计时器
    /// </summary>
    internal class ClientReceiveTimeoutTimer : TimeoutTimer
    {
        public BytesClient Client { get; }

        public ClientReceiveTimeoutTimer(BytesClient client, int timeout, Action timeoutCallback) : base(timeout, timeoutCallback)
        {
            Client = client;
        }

        public override void OnElapsed(ElapsedEventArgs e)
        {
            var nowTimestamp = DateTime.Now;
            var intervalTime = (nowTimestamp - Client.LastMessageReceivedTime).TotalMilliseconds;
            if (intervalTime > timeout)
            {
                base.OnElapsed(e);
            }
            else
            {
                Reset(intervalTime);
            }
        }
    }

    /// <summary>
    /// 超时计时器
    /// </summary>
    internal class TimeoutTimer : Timer
    {
        protected readonly int timeout;
        protected readonly Action timeoutCallback;

        public TimeoutTimer(int timeout, Action timeoutCallback) : base(timeout)
        {
            this.timeout = timeout;
            this.timeoutCallback = timeoutCallback;
            Elapsed += (s, e) => OnElapsed(e);
        }

        public virtual void OnElapsed(ElapsedEventArgs e)
        {
            timeoutCallback.Invoke();
            Enabled = false;
        }

        public void Reset(double interval)
        {
            Stop();
            Interval = interval > 1000 ? interval : 1000;
            Start();
        }
    }
}
