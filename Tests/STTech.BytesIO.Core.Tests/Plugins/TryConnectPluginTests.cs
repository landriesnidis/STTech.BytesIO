using STTech.BytesIO.Core;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace STTech.BytesIO.Core.Tests.Plugins
{
    public class TryConnectPluginTests
    {
        [Fact]
        public void Configure_WithValidArguments_InitializesProperties()
        {
            var plugin = new TryConnectPlugin();
            plugin.Configure(100, 3);

            Assert.True(plugin.IsEnabled);
            Assert.Equal(100, plugin.Delay);
            Assert.Equal(3, plugin.MaxTimes);

            plugin.Cancel();
            Assert.False(plugin.IsEnabled);
        }

        [Fact]
        public void Configure_WithZeroOrNegativeDelay_DisablesPlugin()
        {
            var plugin = new TryConnectPlugin();
            plugin.Configure(0, 3);
            Assert.False(plugin.IsEnabled);

            plugin.Configure(-50, 3);
            Assert.False(plugin.IsEnabled);
        }

        [Fact]
        public void TryConnect_WhenClientAlreadyConnected_DoesNotAddPlugin()
        {
            var client = new MockPluginLifecycleClient();
            client.SimulateConnected();

            client.TryConnect(100, 3);

            Assert.Null(client.GetPlugin<TryConnectPlugin>());
        }

        [Fact]
        public void OnConnectedSuccessfully_AutomaticallyCancelsAndRemovesPlugin()
        {
            var client = new MockPluginLifecycleClient();
            client.SimulateConnectFailure = true; // 初始保持失败状态

            client.TryConnect(50, 3);
            var plugin = client.GetPlugin<TryConnectPlugin>();
            Assert.NotNull(plugin);
            Assert.True(plugin.IsEnabled);

            // 触发连接成功
            client.SimulateConnected();

            Assert.False(plugin.IsEnabled);
            Assert.Null(client.GetPlugin<TryConnectPlugin>());
        }

        [Fact]
        public void ParameterError_ShouldTriggerConnectFailedHandlerAndCancel()
        {
            var client = new MockPluginLifecycleClient();
            client.SimulateConnectFailure = true;
            bool failedHandlerTriggered = false;

            client.TryConnect(50, 3, c => failedHandlerTriggered = true);
            var plugin = client.GetPlugin<TryConnectPlugin>();
            Assert.NotNull(plugin);

            // 模拟参数错误失败事件
            plugin.OnConnectionFailed(new ConnectionFailedEventArgs(ConnectErrorCode.ConnectionParameterError));

            Assert.True(failedHandlerTriggered);
            Assert.False(plugin.IsEnabled);
            Assert.Null(client.GetPlugin<TryConnectPlugin>());
        }

        [Fact]
        public void TryConnectCancel_RemovesActivePlugin()
        {
            var client = new MockPluginLifecycleClient();
            client.SimulateConnectFailure = true;
            client.TryConnect(200, 5);

            Assert.NotNull(client.GetPlugin<TryConnectPlugin>());

            client.TryConnectCancel();
            Assert.Null(client.GetPlugin<TryConnectPlugin>());
        }

        [Fact]
        public async Task MaxTimesExceeded_ShouldTriggerConnectFailedHandler()
        {
            var client = new MockPluginLifecycleClient();
            client.SimulateConnectFailure = true;
            var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            // 延时 20ms，最多重试 2 次
            client.TryConnect(20, 2, c => tcs.TrySetResult(true));

            // 最长等待 3 秒
            var completed = await Task.WhenAny(tcs.Task, Task.Delay(3000));
            Assert.Same(tcs.Task, completed);
            Assert.Null(client.GetPlugin<TryConnectPlugin>());
        }
    }
}
