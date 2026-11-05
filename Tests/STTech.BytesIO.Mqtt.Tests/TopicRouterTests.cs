using STTech.BytesIO.Mqtt;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace STTech.BytesIO.Mqtt.Tests
{
    public class TopicRouterTests
    {
        [Theory]
        [InlineData("sport/tennis/player1", "sport/tennis/player1", true)]
        [InlineData("sport/tennis/player1", "sport/tennis/player2", false)]
        [InlineData("sport/tennis/player1", "sport/tennis/+", true)]
        [InlineData("sport/tennis/player1", "sport/+/player1", true)]
        [InlineData("sport/tennis/player1", "+/tennis/player1", true)]
        [InlineData("sport/tennis/player1", "+/+/+", true)]
        [InlineData("sport/tennis/player1", "+/+", false)]
        [InlineData("sport/tennis/player1", "sport/#", true)]
        [InlineData("sport/tennis/player1", "sport/tennis/#", true)]
        [InlineData("sport/tennis/player1", "sport/tennis/player1/#", true)]
        [InlineData("sport/tennis/player1", "#", true)]
        [InlineData("sport/tennis/player1", "+/#", true)]
        [InlineData("/sport/tennis", "/sport/+", true)]
        [InlineData("/sport/tennis", "/#", true)]
        [InlineData("$SYS/broker/version", "#", false)]
        [InlineData("$SYS/broker/version", "$SYS/#", true)]
        [InlineData("$SYS/broker/version", "$SYS/broker/+", true)]
        public void Test_Topic_Matching_Rules(string topic, string topicFilter, bool expectedMatch)
        {
            bool isMatch = TopicRouter.IsTopicMatch(topic, topicFilter);
            Assert.Equal(expectedMatch, isMatch);
        }

        [Fact]
        public async Task Test_TopicRouter_Add_Match_Remove()
        {
            var router = new TopicRouter();
            var received1 = new List<string>();
            var received2 = new List<string>();

            router.AddRoute("device/+/status", msg =>
            {
                received1.Add(msg.Topic);
                return Task.CompletedTask;
            });

            router.AddRoute("device/#", msg =>
            {
                received2.Add(msg.Topic);
                return Task.CompletedTask;
            });

            var handlers = router.MatchHandlers("device/sensor1/status");
            Assert.Equal(2, handlers.Count);

            var testMsg = new MqttApplicationMessage("device/sensor1/status");
            foreach (var h in handlers)
            {
                await h(testMsg);
            }

            Assert.Single(received1);
            Assert.Single(received2);

            // Match only the '#' wildcard
            var handlersWildcard = router.MatchHandlers("device/sensor1/temperature/current");
            Assert.Single(handlersWildcard);

            // Remove route
            router.RemoveRoute("device/+/status");
            var handlersAfterRemove = router.MatchHandlers("device/sensor1/status");
            Assert.Single(handlersAfterRemove);
        }
    }
}
