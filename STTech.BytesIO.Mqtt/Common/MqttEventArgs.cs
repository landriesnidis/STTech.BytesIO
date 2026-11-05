using System;

namespace STTech.BytesIO.Mqtt
{
    /// <summary>
    /// MQTT 应用消息到达事件参数。
    /// </summary>
    public class MqttApplicationMessageReceivedEventArgs : EventArgs
    {
        public MqttApplicationMessage ApplicationMessage { get; }

        public MqttApplicationMessageReceivedEventArgs(MqttApplicationMessage message)
        {
            ApplicationMessage = message ?? throw new ArgumentNullException(nameof(message));
        }
    }

    /// <summary>
    /// MQTT 会话连接成功事件参数。
    /// </summary>
    public class MqttConnectedEventArgs : EventArgs
    {
        public MqttConnectResult ConnectResult { get; }

        public MqttConnectedEventArgs(MqttConnectResult connectResult)
        {
            ConnectResult = connectResult;
        }
    }

    /// <summary>
    /// MQTT 会话断开事件参数。
    /// </summary>
    public class MqttDisconnectedEventArgs : EventArgs
    {
        public MqttDisconnectReasonCode? ReasonCode { get; }
        public string ReasonString { get; }
        public Exception Exception { get; }

        public MqttDisconnectedEventArgs(MqttDisconnectReasonCode? reasonCode = null, string reasonString = null, Exception exception = null)
        {
            ReasonCode = reasonCode;
            ReasonString = reasonString;
            Exception = exception;
        }
    }

    /// <summary>
    /// MQTT 报文接收事件参数。
    /// </summary>
    public class MqttPacketReceivedEventArgs<T> : EventArgs where T : MqttPacket
    {
        public T Packet { get; }

        public MqttPacketReceivedEventArgs(T packet)
        {
            Packet = packet;
        }
    }
}
