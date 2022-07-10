using STTech.BytesIO.Core.Entity;
using STTech.BytesIO.Core.Component;
using System;
using System.Collections.Generic;
using System.Timers;

namespace STTech.BytesIO.Core
{
    /// <summary>
    /// 自动重连扩展
    /// </summary>
    public static class ReconnectExtension
    {
        private readonly static Lazy<Dictionary<BytesClient, ReconnectTimer>> dictReconnectTimers = new Lazy<Dictionary<BytesClient, ReconnectTimer>>();

        /// <summary>
        /// 启动自动重连
        /// </summary>
        /// <param name="client"></param>
        /// <param name="delay">重连延时</param>
        /// <param name="times">重连次数</param>
        /// <param name="reconnectFailedHandler">重连失败处理回调</param>
        public static void UseAutoReconnect(this BytesClient client, int delay, int times = 0, Action<BytesClient> reconnectFailedHandler = null)
        {
            var dict = dictReconnectTimers.Value;
            ReconnectTimer timer;

            // 当delay为0时，关闭现有的定时重连
            if (delay <= 0)
            {
                if (dict.TryGetValue(client, out timer))
                {
                    timer.Enabled = false;
                    client.OnConnectedSuccessfully -= Client_OnConnectedSuccessfully;
                    client.OnDisconnected -= Client_OnDisconnected;
                    timer.Dispose();
                }
                return;
            }

            // 如果客户端已经设置过定时自动重连
            if (dict.TryGetValue(client, out timer))
            {
                timer.Enabled = false;
                timer.Interval = delay;
                timer.ResetReconnectTimes();
                timer.Enabled = true;
            }
            else
            {
                timer = new ReconnectTimer(client);
                timer.ElapsedHnadler = t => client.ConnectAsync();
                timer.ReconnectFailedHandler = reconnectFailedHandler;
                timer.ReconnectTimes = times;
                timer.ResetReconnectTimes();
                timer.Interval = delay;
                timer.Enabled = true;
                client.OnConnectedSuccessfully += Client_OnConnectedSuccessfully;
                client.OnDisconnected += Client_OnDisconnected;
                dict[client] = timer;
            }
        }

        private static void Client_OnDisconnected(object sender, DisconnectedEventArgs e)
        {
            var dict = dictReconnectTimers.Value;
            ReconnectTimer timer;
            if (dict.TryGetValue((BytesClient)sender, out timer))
            {
                timer.Enabled = !e.IsActively;
                timer.ResetReconnectTimes();
            }
        }

        private static void Client_OnConnectedSuccessfully(object sender, ConnectedSuccessfullyEventArgs e)
        {
            var dict = dictReconnectTimers.Value;
            ReconnectTimer timer;
            if (dict.TryGetValue((BytesClient)sender, out timer))
            {
                timer.Enabled = false;
                timer.ResetReconnectTimes();
            }
        }

        private class ReconnectTimer : Timer
        {
            public Action<ReconnectTimer> ElapsedHnadler { get; set; }
            public Action<BytesClient> ReconnectFailedHandler { get; set; }
            public int ReconnectTimes { get; set; }
            private int currentTimes;
            private BytesClient client;

            public ReconnectTimer(BytesClient client)
            {
                this.client = client;

                Elapsed += (s, e) =>
                {
                    // 判断重连次数是否有限制
                    if (ReconnectTimes > 0)
                    {
                        if (currentTimes > 0)
                        {
                            currentTimes--;
                            ElapsedHnadler?.Invoke(this);
                        }
                        else
                        {
                            Stop();
                            ReconnectFailedHandler?.Invoke(this.client);
                        }
                    }
                    else
                    {
                        ElapsedHnadler?.Invoke(this);
                    }
                };
            }


            /// <summary>
            /// 重置重连次数
            /// </summary>
            public void ResetReconnectTimes()
            {
                currentTimes = ReconnectTimes;
            }

            protected override void Dispose(bool disposing)
            {
                ElapsedHnadler = null;
                ReconnectFailedHandler = null;
                client = null;

                base.Dispose(disposing);
            }
        }
    }
}
