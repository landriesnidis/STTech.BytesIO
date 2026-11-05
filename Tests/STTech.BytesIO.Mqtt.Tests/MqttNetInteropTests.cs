using MQTTnet;
using MQTTnet.Client;
using STTech.BytesIO.Core;
using STTech.BytesIO.Mqtt;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace STTech.BytesIO.Mqtt.Tests
{
    /// <summary>
    /// MQTTnet 与 STTech.BytesIO.Mqtt 跨客户端在公网 EMQX Broker 上的互通测试套件。
    /// 涵盖 QoS 0/1/2 双向互通、通配符路由、Retain 消息、MQTT v5 特性（UserProperties、ResponseTopic）、LWT 遗嘱及托管离线队列。
    /// </summary>
    [Trait("Category", "LiveBrokerInterop")]
    public class MqttNetInteropTests
    {
        private const string BrokerHost = "broker.emqx.io";
        private const int BrokerPort = 1883;

        private static string CreateUniqueTopic(string suffix) => $"sttech/interop/{Guid.NewGuid():N}/{suffix}";

        private static async Task<IMqttClient> CreateConnectedMqttNetClientAsync(string clientId = null)
        {
            var factory = new MqttFactory();
            var client = factory.CreateMqttClient();
            var options = new MqttClientOptionsBuilder()
                .WithTcpServer(BrokerHost, BrokerPort)
                .WithClientId(clientId ?? $"MqttNet_{Guid.NewGuid():N}")
                .WithCleanSession(true)
                .WithProtocolVersion(MQTTnet.Formatter.MqttProtocolVersion.V500)
                .WithTimeout(TimeSpan.FromSeconds(15))
                .Build();

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
            var connectResult = await client.ConnectAsync(options, cts.Token);
            Assert.Equal(MqttClientConnectResultCode.Success, connectResult.ResultCode);
            return client;
        }

        private static async Task<MqttTcpClient> CreateConnectedBytesIOClientAsync(string clientId = null)
        {
            var client = new MqttTcpClient(BrokerHost, BrokerPort);
            client.Options.ClientId = clientId ?? $"BytesIO_{Guid.NewGuid():N}";
            client.Options.CleanStart = true;
            client.Options.CommunicationTimeout = TimeSpan.FromSeconds(15);

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
            var connectResult = await client.ConnectMqttAsync(cancellationToken: cts.Token);
            Assert.True(connectResult.IsSuccess, $"BytesIO 连接 Broker 失败: {connectResult.Exception?.Message}");
            return client;
        }

        #region Test 1: 双向 QoS 0/1/2 消息互通

        [Theory]
        [InlineData(MqttQualityOfServiceLevel.AtMostOnce, MQTTnet.Protocol.MqttQualityOfServiceLevel.AtMostOnce)]
        [InlineData(MqttQualityOfServiceLevel.AtLeastOnce, MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)]
        [InlineData(MqttQualityOfServiceLevel.ExactlyOnce, MQTTnet.Protocol.MqttQualityOfServiceLevel.ExactlyOnce)]
        public async Task Interop_BiDirectional_PublishSubscribe_QoS(
            MqttQualityOfServiceLevel bytesIoQoS,
            MQTTnet.Protocol.MqttQualityOfServiceLevel mqttNetQoS)
        {
            using var bytesIoClient = await CreateConnectedBytesIOClientAsync();
            using var mqttNetClient = await CreateConnectedMqttNetClientAsync();

            var topicBytesIoToNet = CreateUniqueTopic("bio_to_net");
            var topicNetToBytesIo = CreateUniqueTopic("net_to_bio");

            // 1. MQTTnet 订阅 topicBytesIoToNet
            var mqttNetReceivedTcs = new TaskCompletionSource<MQTTnet.MqttApplicationMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
            mqttNetClient.ApplicationMessageReceivedAsync += args =>
            {
                if (args.ApplicationMessage.Topic == topicBytesIoToNet)
                {
                    mqttNetReceivedTcs.TrySetResult(args.ApplicationMessage);
                }
                return Task.CompletedTask;
            };
            await mqttNetClient.SubscribeAsync(new MqttTopicFilterBuilder()
                .WithTopic(topicBytesIoToNet)
                .WithQualityOfServiceLevel(mqttNetQoS)
                .Build());

            // 2. BytesIO 订阅 topicNetToBytesIo
            var bytesIoReceivedTcs = new TaskCompletionSource<STTech.BytesIO.Mqtt.MqttApplicationMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
            await bytesIoClient.SubscribeAsync(topicNetToBytesIo, bytesIoQoS, msg =>
            {
                bytesIoReceivedTcs.TrySetResult(msg);
                return Task.CompletedTask;
            });

            await Task.Delay(1000); // 确保订阅已在公网 Broker 上就绪

            // 3. BytesIO 发送 -> MQTTnet 接收
            var payloadFromBytesIO = $"Payload_From_BytesIO_{Guid.NewGuid():N}";
            var pubResult1 = await bytesIoClient.PublishAsync(new STTech.BytesIO.Mqtt.MqttApplicationMessage(topicBytesIoToNet, payloadFromBytesIO, qos: bytesIoQoS));
            Assert.True(pubResult1.IsSuccess);

            var netReceivedMsg = await Task.WhenAny(mqttNetReceivedTcs.Task, Task.Delay(10000));
            Assert.Same(mqttNetReceivedTcs.Task, netReceivedMsg);
            var actualMsg1 = await mqttNetReceivedTcs.Task;
            Assert.Equal(payloadFromBytesIO, Encoding.UTF8.GetString(actualMsg1.PayloadSegment.ToArray()));

            // 4. MQTTnet 发送 -> BytesIO 接收
            var payloadFromNet = $"Payload_From_MqttNet_{Guid.NewGuid():N}";
            var pubResult2 = await mqttNetClient.PublishAsync(new MQTTnet.MqttApplicationMessageBuilder()
                .WithTopic(topicNetToBytesIo)
                .WithPayload(Encoding.UTF8.GetBytes(payloadFromNet))
                .WithQualityOfServiceLevel(mqttNetQoS)
                .Build());
            Assert.Equal(MqttClientPublishReasonCode.Success, pubResult2.ReasonCode);

            var bytesIoReceivedMsg = await Task.WhenAny(bytesIoReceivedTcs.Task, Task.Delay(10000));
            Assert.Same(bytesIoReceivedTcs.Task, bytesIoReceivedMsg);
            var actualMsg2 = await bytesIoReceivedTcs.Task;
            Assert.Equal(payloadFromNet, actualMsg2.ConvertPayloadToString());
        }

        #endregion

        #region Test 2: 通配符主题路由 (+ 与 #)

        [Fact]
        public async Task Interop_WildcardTopicRouting_MatchesExpectedSubscribers()
        {
            using var bytesIoClient = await CreateConnectedBytesIOClientAsync();
            using var mqttNetClient = await CreateConnectedMqttNetClientAsync();

            var basePrefix = $"sttech/interop/{Guid.NewGuid():N}";
            var singleLevelFilter = $"{basePrefix}/+/temperature";
            var multiLevelFilter = $"{basePrefix}/sensors/#";

            var singleWildcardReceived = new List<string>();
            var multiWildcardReceived = new List<string>();

            // BytesIO 订阅单层通配符
            await bytesIoClient.SubscribeAsync(singleLevelFilter, MqttQualityOfServiceLevel.AtLeastOnce, msg =>
            {
                lock (singleWildcardReceived) singleWildcardReceived.Add(msg.Topic);
                return Task.CompletedTask;
            });

            // BytesIO 订阅多层通配符
            await bytesIoClient.SubscribeAsync(multiLevelFilter, MqttQualityOfServiceLevel.AtLeastOnce, msg =>
            {
                lock (multiWildcardReceived) multiWildcardReceived.Add(msg.Topic);
                return Task.CompletedTask;
            });

            await Task.Delay(1000);

            // 由 MQTTnet 发布不同层级的主题
            var t1 = $"{basePrefix}/room1/temperature";    // 命中 singleLevelFilter
            var t2 = $"{basePrefix}/sensors/device1/data"; // 命中 multiLevelFilter
            var t3 = $"{basePrefix}/room1/humidity";       // 都不命中

            await mqttNetClient.PublishStringAsync(t1, "24.5C");
            await mqttNetClient.PublishStringAsync(t2, "voltage:220V");
            await mqttNetClient.PublishStringAsync(t3, "60%");

            // 等待消息接收
            await Task.Delay(3000);

            lock (singleWildcardReceived)
            {
                Assert.Contains(t1, singleWildcardReceived);
                Assert.DoesNotContain(t3, singleWildcardReceived);
            }

            lock (multiWildcardReceived)
            {
                Assert.Contains(t2, multiWildcardReceived);
                Assert.DoesNotContain(t3, multiWildcardReceived);
            }
        }

        #endregion

        #region Test 3: Retain 消息与清理机制

        [Fact]
        public async Task Interop_RetainMessage_And_Clear()
        {
            var topic = CreateUniqueTopic("retain_test");
            var retainedContent = $"Retained_Content_{Guid.NewGuid():N}";

            // 1. BytesIO 发布 Retain 消息
            using (var publisher = await CreateConnectedBytesIOClientAsync())
            {
                var pubResult = await publisher.PublishAsync(new STTech.BytesIO.Mqtt.MqttApplicationMessage(
                    topic,
                    retainedContent,
                    qos: MqttQualityOfServiceLevel.AtLeastOnce,
                    retain: true
                ));
                Assert.True(pubResult.IsSuccess);
            }

            await Task.Delay(1000);

            // 2. MQTTnet 随后上线并订阅，应当立刻收到 Broker 缓存的 Retain 消息
            var mqttNetReceivedTcs = new TaskCompletionSource<MQTTnet.MqttApplicationMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
            using (var subscriber = await CreateConnectedMqttNetClientAsync())
            {
                subscriber.ApplicationMessageReceivedAsync += args =>
                {
                    if (args.ApplicationMessage.Topic == topic)
                    {
                        mqttNetReceivedTcs.TrySetResult(args.ApplicationMessage);
                    }
                    return Task.CompletedTask;
                };

                await subscriber.SubscribeAsync(topic);

                var waitMsg = await Task.WhenAny(mqttNetReceivedTcs.Task, Task.Delay(10000));
                Assert.Same(mqttNetReceivedTcs.Task, waitMsg);

                var receivedMsg = await mqttNetReceivedTcs.Task;
                Assert.True(receivedMsg.Retain);
                Assert.Equal(retainedContent, Encoding.UTF8.GetString(receivedMsg.PayloadSegment.ToArray()));
            }

            // 3. 清理 Broker 上的 Retain 消息（发送空负载且 Retain=true）
            using (var cleaner = await CreateConnectedBytesIOClientAsync())
            {
                await cleaner.PublishAsync(new STTech.BytesIO.Mqtt.MqttApplicationMessage(topic, Array.Empty<byte>(), retain: true));
            }
        }

        #endregion

        #region Test 4: MQTT v5 扩展属性（UserProperties 与 ResponseTopic）

        [Fact]
        public async Task Interop_MqttV5_UserProperties_And_ResponseTopic()
        {
            using var bytesIoClient = await CreateConnectedBytesIOClientAsync();
            using var mqttNetClient = await CreateConnectedMqttNetClientAsync();

            var reqTopic = CreateUniqueTopic("v5/request");
            var expectedResponseTopic = CreateUniqueTopic("v5/response");

            var netReceivedTcs = new TaskCompletionSource<MQTTnet.MqttApplicationMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
            mqttNetClient.ApplicationMessageReceivedAsync += args =>
            {
                if (args.ApplicationMessage.Topic == reqTopic)
                {
                    netReceivedTcs.TrySetResult(args.ApplicationMessage);
                }
                return Task.CompletedTask;
            };
            await mqttNetClient.SubscribeAsync(reqTopic);
            await Task.Delay(1000);

            // BytesIO 发送包含 MQTT v5 属性的消息
            var v5Msg = new STTech.BytesIO.Mqtt.MqttApplicationMessage(reqTopic, "Call Method", qos: MqttQualityOfServiceLevel.AtLeastOnce)
            {
                ResponseTopic = expectedResponseTopic
            };
            v5Msg.Properties.UserProperties.Add(new MqttUserProperty("traceId", "TR-888999"));
            v5Msg.Properties.UserProperties.Add(new MqttUserProperty("author", "landriesnidis"));

            var pubResult = await bytesIoClient.PublishAsync(v5Msg);
            Assert.True(pubResult.IsSuccess);

            var completedTask = await Task.WhenAny(netReceivedTcs.Task, Task.Delay(10000));
            Assert.Same(netReceivedTcs.Task, completedTask);

            var netMsg = await netReceivedTcs.Task;
            // 验证 MQTTnet 成功收到 BytesIO 编码的 v5 属性
            Assert.Equal(expectedResponseTopic, netMsg.ResponseTopic);
            Assert.NotNull(netMsg.UserProperties);
            Assert.Contains(netMsg.UserProperties, up => up.Name == "traceId" && up.Value == "TR-888999");
            Assert.Contains(netMsg.UserProperties, up => up.Name == "author" && up.Value == "landriesnidis");
        }

        #endregion

        #region Test 5: 遗嘱消息（Last Will and Testament, LWT）

        [Fact]
        public async Task Interop_LastWillAndTestament_TriggeredOnAbruptDisconnect()
        {
            var willTopic = CreateUniqueTopic("will_topic");
            var willPayload = "BytesIO_Client_Dead_Abruptly";

            using var observerNetClient = await CreateConnectedMqttNetClientAsync();
            var willReceivedTcs = new TaskCompletionSource<MQTTnet.MqttApplicationMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
            observerNetClient.ApplicationMessageReceivedAsync += args =>
            {
                if (args.ApplicationMessage.Topic == willTopic)
                {
                    willReceivedTcs.TrySetResult(args.ApplicationMessage);
                }
                return Task.CompletedTask;
            };
            await observerNetClient.SubscribeAsync(willTopic, MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce);
            await Task.Delay(1000);

            // 创建注册了遗嘱的 BytesIO 客户端
            var bytesIoClient = new MqttTcpClient(BrokerHost, BrokerPort);
            bytesIoClient.Options.ClientId = $"BytesIO_Will_{Guid.NewGuid():N}";
            bytesIoClient.Options.CleanStart = true;
            bytesIoClient.Options.HasWill = true;
            bytesIoClient.Options.WillTopic = willTopic;
            bytesIoClient.Options.WillPayload = Encoding.UTF8.GetBytes(willPayload);
            bytesIoClient.Options.WillQoS = MqttQualityOfServiceLevel.AtLeastOnce;

            var connResult = await bytesIoClient.ConnectMqttAsync();
            Assert.True(connResult.IsSuccess);

            // 模拟异常掉线（不断开 MQTT DISCONNECT，直接暴力关闭底层 TCP 传输）
            bytesIoClient.InnerClient.Disconnect(new DisconnectArgument(DisconnectionReasonCode.Active));

            // 等待公网 Broker 感知并把遗嘱派发给 observerNetClient
            var waitWill = await Task.WhenAny(willReceivedTcs.Task, Task.Delay(15000));
            Assert.Same(willReceivedTcs.Task, waitWill);

            var actualWill = await willReceivedTcs.Task;
            Assert.Equal(willPayload, Encoding.UTF8.GetString(actualWill.PayloadSegment.ToArray()));

            bytesIoClient.Dispose();
        }

        #endregion

        #region Test 6: MqttManagedTcpClient 离线积压与重连后投递至 MQTTnet

        [Fact]
        public async Task Interop_ManagedClient_OfflineEnqueue_DeliversToMqttNetUponConnect()
        {
            var topic = CreateUniqueTopic("managed_offline");
            using var mqttNetClient = await CreateConnectedMqttNetClientAsync();

            var receivedPackets = new List<string>();
            var allReceivedSignal = new SemaphoreSlim(0, 3);

            mqttNetClient.ApplicationMessageReceivedAsync += args =>
            {
                if (args.ApplicationMessage.Topic == topic)
                {
                    lock (receivedPackets)
                    {
                        receivedPackets.Add(Encoding.UTF8.GetString(args.ApplicationMessage.PayloadSegment.ToArray()));
                    }
                    allReceivedSignal.Release();
                }
                return Task.CompletedTask;
            };
            await mqttNetClient.SubscribeAsync(topic, MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce);
            await Task.Delay(1000);

            // 创建开箱即用的 MqttManagedTcpClient（先不连公网）
            using var managedClient = new MqttManagedTcpClient(BrokerHost, BrokerPort);

            // 在未连接状态下入队 3 条消息
            var msg1 = new STTech.BytesIO.Mqtt.MqttApplicationMessage(topic, "Packet1", qos: MqttQualityOfServiceLevel.AtLeastOnce);
            var msg2 = new STTech.BytesIO.Mqtt.MqttApplicationMessage(topic, "Packet2", qos: MqttQualityOfServiceLevel.AtLeastOnce);
            var msg3 = new STTech.BytesIO.Mqtt.MqttApplicationMessage(topic, "Packet3", qos: MqttQualityOfServiceLevel.AtLeastOnce);

            var t1 = managedClient.EnqueuePublishAsync(msg1);
            var t2 = managedClient.EnqueuePublishAsync(msg2);
            var t3 = managedClient.EnqueuePublishAsync(msg3);

            Assert.Equal(3, managedClient.QueuedCount);

            // 连接到公网 Broker
            var conn = await managedClient.ConnectMqttAsync();
            Assert.True(conn.IsSuccess);

            // 验证 3 个任务在客户端恢复后成功发送
            await Task.WhenAll(t1, t2, t3);
            Assert.Equal(0, managedClient.QueuedCount);

            // 等待 MQTTnet 全部接收到
            for (int i = 0; i < 3; i++)
            {
                bool got = await allReceivedSignal.WaitAsync(TimeSpan.FromSeconds(10));
                Assert.True(got, $"未在时限内收到全部 3 条离线消息，当前收到数: {receivedPackets.Count}");
            }

            lock (receivedPackets)
            {
                Assert.Contains("Packet1", receivedPackets);
                Assert.Contains("Packet2", receivedPackets);
                Assert.Contains("Packet3", receivedPackets);
            }
        }

        #endregion
    }
}
