using STTech.BytesIO.Mqtt;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace STTech.BytesIO.Mqtt.Tests
{
    [Trait("Category", "LiveBroker")]
    public class MqttLiveBrokerTests
    {
        private const string BrokerHost = "broker.emqx.io";
        private const int BrokerPort = 1883;

        [Fact]
        public async Task LiveBroker_EMQX_Connect_Publish_Subscribe_QoS1_QoS2()
        {
            string sessionGuid = Guid.NewGuid().ToString("N");
            string testTopic = $"sttech/bytesio/test/{sessionGuid}/sensor";
            string testPayloadQoS1 = "{\"temperature\": 26.5, \"humidity\": 60.2}";
            string testPayloadQoS2 = "{\"command\": \"calibrate\", \"target\": \"sensor1\"}";

            using var client = new MqttTcpClient(BrokerHost, BrokerPort);
            client.Options.ClientId = $"STTech_Client_{sessionGuid}";
            client.Options.CleanStart = true;
            client.Options.KeepAlive = 30;
            client.Options.CommunicationTimeout = TimeSpan.FromSeconds(15);

            // 1. 连接公网 EMQX Broker
            var connectResult = await client.ConnectMqttAsync();
            Assert.True(connectResult.IsSuccess, $"连接公网 Broker 失败: {connectResult.ReasonCode}, {connectResult.ReasonString}, {connectResult.Exception?.Message}");
            Assert.True(client.IsConnected);

            var messageReceivedTcs = new TaskCompletionSource<MqttApplicationMessage>(TaskCreationOptions.RunContinuationsAsynchronously);

            // 2. 订阅专属隔离主题
            var subResult = await client.SubscribeAsync(testTopic, MqttQualityOfServiceLevel.ExactlyOnce, msg =>
            {
                messageReceivedTcs.TrySetResult(msg);
                return Task.CompletedTask;
            });
            Assert.True(subResult.IsSuccess, "订阅公网主题失败。");

            // 3. 发布 QoS 1 消息并附加用户自定义属性
            var qos1Msg = new MqttApplicationMessage(testTopic, testPayloadQoS1)
            {
                QualityOfServiceLevel = MqttQualityOfServiceLevel.AtLeastOnce
            };
            qos1Msg.Properties.UserProperties.Add(new MqttUserProperty("traceId", sessionGuid));
            qos1Msg.Properties.ContentType = "application/json";

            var pub1Result = await client.PublishAsync(qos1Msg);
            Assert.True(pub1Result.IsSuccess, $"QoS 1 发布失败: {pub1Result.Exception?.Message}");

            // 4. 等待 Broker 推送订阅的消息
            var completedTask = await Task.WhenAny(messageReceivedTcs.Task, Task.Delay(10000));
            Assert.Equal(messageReceivedTcs.Task, completedTask);

            var receivedMsg = await messageReceivedTcs.Task;
            Assert.Equal(testTopic, receivedMsg.Topic);
            Assert.Equal(testPayloadQoS1, receivedMsg.ConvertPayloadToString());

            // 5. 发布 QoS 2 消息
            var messageQoS2Tcs = new TaskCompletionSource<MqttApplicationMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
            client.Router.AddRoute(testTopic, msg =>
            {
                if (msg.ConvertPayloadToString() == testPayloadQoS2)
                {
                    messageQoS2Tcs.TrySetResult(msg);
                }
                return Task.CompletedTask;
            });

            var qos2Msg = new MqttApplicationMessage(testTopic, testPayloadQoS2)
            {
                QualityOfServiceLevel = MqttQualityOfServiceLevel.ExactlyOnce
            };
            var pub2Result = await client.PublishAsync(qos2Msg);
            Assert.True(pub2Result.IsSuccess, $"QoS 2 发布失败: {pub2Result.Exception?.Message}");

            var completedQoS2Task = await Task.WhenAny(messageQoS2Tcs.Task, Task.Delay(10000));
            Assert.Equal(messageQoS2Tcs.Task, completedQoS2Task);
            var receivedQoS2Msg = await messageQoS2Tcs.Task;
            Assert.Equal(testPayloadQoS2, receivedQoS2Msg.ConvertPayloadToString());

            // 6. 心跳保活测试
            await client.PingAsync();

            // 7. 取消订阅与断开连接
            var unsubResult = await client.UnsubscribeAsync(testTopic);
            Assert.True(unsubResult.IsSuccess);

            await client.DisconnectMqttAsync();
            Assert.False(client.IsConnected);
        }
    }
}
