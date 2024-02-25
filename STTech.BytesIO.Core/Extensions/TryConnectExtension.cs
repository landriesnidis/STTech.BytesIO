using System;
using System.Collections.Generic;
using System.Timers;

namespace STTech.BytesIO.Core
{
    public static class TryConnectExtension
    {
        private readonly static Lazy<Dictionary<BytesClient, TryConnectTimer>> dictTryConnectTimers = new Lazy<Dictionary<BytesClient, TryConnectTimer>>();

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
            var dict = dictTryConnectTimers.Value;
            TryConnectTimer timer;

            // 当delay为0时，关闭现有的定时重连
            if (delay <= 0)
            {
                if (dict.TryGetValue(client, out timer))
                {
                    timer.Enabled = false;
                    client.OnConnectedSuccessfully -= Client_OnConnectedSuccessfully;
                    client.OnConnectionFailed -= Client_OnConnectionFailed;
                    timer.Dispose();
                    dict.Remove(client);
                }
                return;
            }

            if (client.IsConnected)
            {
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
                timer = new TryConnectTimer(client);
                timer.ElapsedHnadler = Client_ElapsedHnadler;
                timer.ElapsedHnadler = t => client.ConnectAsync();
                timer.ReconnectFailedHandler = connectFailedHandler;
                timer.ReconnectTimes = times;
                timer.ResetReconnectTimes();
                timer.Interval = delay;
                client.OnConnectedSuccessfully += Client_OnConnectedSuccessfully;
                client.OnConnectionFailed += Client_OnConnectionFailed;
                dict[client] = timer;

                timer.Enabled = true;
                Client_ElapsedHnadler(timer);
            }
        }


        private static void Client_ElapsedHnadler(TryConnectTimer timer)
        {
            if (timer.Client.IsConnected)
            {
                timer.Client.TryConnect(0, 0);
            }
            else
            {
                timer.Client.Connect();
            }
        }

        private static void Client_OnConnectionFailed(object sender, ConnectionFailedEventArgs e)
        {
            var dict = dictTryConnectTimers.Value;
            TryConnectTimer timer;
            if (dict.TryGetValue((BytesClient)sender, out timer))
            {
                if (e.ErrorCode == ConnectErrorCode.ConnectionParameterError)
                {
                    timer.Enabled = false;
                    timer.Client.TryConnect(0, 0);
                    timer.ReconnectFailedHandler?.Invoke(timer.Client);
                }
            }
        }

        private static void Client_OnConnectedSuccessfully(object sender, ConnectedSuccessfullyEventArgs e)
        {
            var dict = dictTryConnectTimers.Value;
            TryConnectTimer timer;
            if (dict.TryGetValue((BytesClient)sender, out timer))
            {
                timer.Enabled = false;
                timer.Client.TryConnect(0, 0);
            }
        }

        private class TryConnectTimer : Timer
        {
            public Action<TryConnectTimer> ElapsedHnadler { get; set; }
            public Action<BytesClient> ReconnectFailedHandler { get; set; }
            public int ReconnectTimes { get; set; }
            private int currentTimes;
            public BytesClient Client { get; private set; }

            public TryConnectTimer(BytesClient client)
            {
                this.Client = client;

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
                            ReconnectFailedHandler?.Invoke(this.Client);
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
                Client = null;

                base.Dispose(disposing);
            }
        }
    }
}
