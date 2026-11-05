using System;

namespace STTech.BytesIO.Mqtt
{
    /// <summary>
    /// MQTT 基础异常类。
    /// </summary>
    public class MqttException : Exception
    {
        public MqttException() { }
        public MqttException(string message) : base(message) { }
        public MqttException(string message, Exception innerException) : base(message, innerException) { }
    }

    /// <summary>
    /// MQTT 协议违例异常（如数据格式错误、非法长度、超出规范限制等）。
    /// </summary>
    public class MqttProtocolViolationException : MqttException
    {
        public MqttProtocolViolationException() { }
        public MqttProtocolViolationException(string message) : base(message) { }
        public MqttProtocolViolationException(string message, Exception innerException) : base(message, innerException) { }
    }

    /// <summary>
    /// MQTT 通信交互异常（如超时、Broker 拒绝等）。
    /// </summary>
    public class MqttCommunicationException : MqttException
    {
        public MqttCommunicationException() { }
        public MqttCommunicationException(string message) : base(message) { }
        public MqttCommunicationException(string message, Exception innerException) : base(message, innerException) { }
    }
}
