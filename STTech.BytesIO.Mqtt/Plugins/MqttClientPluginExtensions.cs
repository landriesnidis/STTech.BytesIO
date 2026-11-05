using System;
using System.Threading;
using System.Threading.Tasks;

namespace STTech.BytesIO.Mqtt
{
    /// <summary>
    /// MQTT 客户端插件操作扩展方法。
    /// </summary>
    public static class MqttClientPluginExtensions
    {
        /// <summary>
        /// 使用插件发送队列发布 MQTT 消息。
        /// 若客户端未挂载 <see cref="MqttPublishQueuePlugin"/>，将自动添加。
        /// </summary>
        /// <param name="client">MQTT 客户端</param>
        /// <param name="message">应用消息</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>发布操作结果任务</returns>
        public static Task<MqttPublishResult> EnqueuePublishAsync(this MqttClient client, MqttApplicationMessage message, CancellationToken cancellationToken = default)
        {
            if (client == null) throw new ArgumentNullException(nameof(client));
            if (message == null) throw new ArgumentNullException(nameof(message));

            var plugin = client.GetPlugin<MqttPublishQueuePlugin>();
            if (plugin == null)
            {
                plugin = new MqttPublishQueuePlugin();
                client.AddPlugin(plugin);
            }

            return plugin.EnqueuePublishAsync(message, cancellationToken);
        }

        /// <summary>
        /// 启用自动重订阅保护。
        /// 自动挂载 <see cref="MqttAutoResubscribePlugin"/> 并在该主题订阅成功后纳管。
        /// </summary>
        public static MqttAutoResubscribePlugin EnableAutoResubscribe(this MqttClient client)
        {
            if (client == null) throw new ArgumentNullException(nameof(client));

            var plugin = client.GetPlugin<MqttAutoResubscribePlugin>();
            if (plugin == null)
            {
                plugin = new MqttAutoResubscribePlugin();
                client.AddPlugin(plugin);
            }

            return plugin;
        }
    }
}
