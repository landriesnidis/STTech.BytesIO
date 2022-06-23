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
        private static Dictionary<BytesClient, Timer> dictReconnectTimers;

        /// <summary>
        /// 启动自动重连
        /// </summary>
        /// <param name="client"></param>
        /// <param name="delay"></param>
        public static void UseAutoReconnect(this BytesClient client, int delay)
        {
            dictReconnectTimers ??= new Dictionary<BytesClient, Timer>();

            // 如果客户端已经设置过定时自动重连
            if (dictReconnectTimers.ContainsKey(client))
            {
                Timer timer = dictReconnectTimers[client];
                timer.Enabled = false;
                timer.Interval = delay;
                timer.Enabled = true;
            }
            else
            {
                Timer timer = new Timer();
                timer.Interval = delay;
                timer.Elapsed += (s, e) =>
                    client.ConnectAsync();
                timer.Enabled = true;
                client.OnConnectedSuccessfully += (s, e) => timer.Enabled = false;
                client.OnDisconnected += (s, e) => timer.Enabled = !e.IsActively;
                dictReconnectTimers[client] = timer;
            }
        }
    }
}
