using STTech.BytesIO.Core;
using System;

namespace STTech.BytesIO.Mqtt
{
    /// <summary>
    /// MQTT 客户端插件基类。
    /// 继承自 BytesClientPlugin，提供 MQTT 协议层专有的生命周期回调与强类型 Client 访问。
    /// </summary>
    public abstract class MqttClientPlugin : BytesClientPlugin
    {
        /// <summary>
        /// 所属的 MQTT 客户端实例。
        /// </summary>
        public new MqttClient Client => base.Client as MqttClient;

        /// <inheritdoc/>
        protected internal override void OnAttached()
        {
            base.OnAttached();

            if (Client != null)
            {
                Client.OnMqttConnectedSuccessfully += HandleMqttConnectedSuccessfully;
                Client.OnMqttDisconnected += HandleMqttDisconnected;
                Client.OnApplicationMessageReceived += HandleApplicationMessageReceived;
            }
        }

        /// <inheritdoc/>
        protected internal override void OnDetached()
        {
            if (Client != null)
            {
                Client.OnMqttConnectedSuccessfully -= HandleMqttConnectedSuccessfully;
                Client.OnMqttDisconnected -= HandleMqttDisconnected;
                Client.OnApplicationMessageReceived -= HandleApplicationMessageReceived;
            }

            base.OnDetached();
        }

        private void HandleMqttConnectedSuccessfully(object sender, MqttConnectedEventArgs e)
        {
            if (IsEnabled)
            {
                OnMqttConnectedSuccessfully(e);
            }
        }

        private void HandleMqttDisconnected(object sender, MqttDisconnectedEventArgs e)
        {
            if (IsEnabled)
            {
                OnMqttDisconnected(e);
            }
        }

        private void HandleApplicationMessageReceived(object sender, MqttApplicationMessageReceivedEventArgs e)
        {
            if (IsEnabled)
            {
                OnApplicationMessageReceived(e);
            }
        }

        /// <summary>
        /// 当 MQTT 协议握手建立成功时调用。
        /// </summary>
        /// <param name="e">连接事件参数</param>
        protected internal virtual void OnMqttConnectedSuccessfully(MqttConnectedEventArgs e) { }

        /// <summary>
        /// 当 MQTT 会话断开时调用。
        /// </summary>
        /// <param name="e">断开事件参数</param>
        protected internal virtual void OnMqttDisconnected(MqttDisconnectedEventArgs e) { }

        /// <summary>
        /// 当接收到 MQTT 应用消息时调用。
        /// </summary>
        /// <param name="e">消息接收事件参数</param>
        protected internal virtual void OnApplicationMessageReceived(MqttApplicationMessageReceivedEventArgs e) { }
    }
}
