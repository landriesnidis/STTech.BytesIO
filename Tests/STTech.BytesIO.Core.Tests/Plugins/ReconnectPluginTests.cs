using STTech.BytesIO.Core;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace STTech.BytesIO.Core.Tests.Plugins
{
    /// <summary>
    /// 测试客户端桩：用于测试插件在连接与断开生命周期中的行为
    /// </summary>
    internal class MockPluginLifecycleClient : BytesClient
    {
        private bool _isConnected;
        public int ConnectCallCount { get; set; }
        public int DisconnectCallCount { get; set; }
        public bool SimulateConnectFailure { get; set; }

        public override bool IsConnected => _isConnected;

        public void SimulateConnected()
        {
            _isConnected = true;
            GenerateNewConnectionId();
            RaiseConnectedSuccessfully(this, new ConnectedSuccessfullyEventArgs());
        }

        public override ConnectResult Connect(ConnectArgument? argument = null)
        {
            ConnectCallCount++;
            if (SimulateConnectFailure)
            {
                var failedArgs = new ConnectionFailedEventArgs(ConnectErrorCode.Error);
                RaiseConnectionFailed(this, failedArgs);
                return new ConnectResult(ConnectErrorCode.Error);
            }

            _isConnected = true;
            GenerateNewConnectionId();
            RaiseConnectedSuccessfully(this, new ConnectedSuccessfullyEventArgs());
            return new ConnectResult();
        }

        public override Task<ConnectResult> ConnectAsync(ConnectArgument? argument = null)
        {
            return Task.FromResult(Connect(argument));
        }

        public override DisconnectResult Disconnect(DisconnectArgument? argument = null)
        {
            DisconnectCallCount++;
            if (!_isConnected)
            {
                return new DisconnectResult(DisconnectErrorCode.NoConnection);
            }

            _isConnected = false;
            argument ??= new DisconnectArgument();
            var args = new DisconnectedEventArgs(argument.ReasonCode, argument.Exception);
            RaiseDisconnected(this, args);
            return new DisconnectResult();
        }

        protected override Task SendHandlerAsync(SendArgs args) => Task.CompletedTask;
        protected override void ReceiveDataCompletedHandle() { }
        protected override Task ReceiveDataHandleAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public override void Dispose()
        {
            Disconnect();
        }
    }

    /// <summary>
    /// 自动重连插件（ReconnectPlugin）生命周期与重连行为单元测试
    /// </summary>
    public class ReconnectPluginTests
    {
        [Fact]
        public async Task ReconnectPlugin_MultipleReconnectCycles_TimerRemainsActive_AcrossDisconnections()
        {
            var client = new MockPluginLifecycleClient();
            // 配置 50ms 快速重连，无限次
            client.UseAutoReconnect(delay: 50, times: 0);

            // 初始连接成功
            client.SimulateConnected();
            Assert.True(client.IsConnected);
            Assert.Equal(0, client.ConnectCallCount);

            // ====== 周期 1 ======
            // 非主动断网，触发自动重连
            client.Disconnect(new DisconnectArgument(DisconnectionReasonCode.Passive));
            Assert.False(client.IsConnected);

            // 等待重连触发 (最长等待 1000ms)
            for (int i = 0; i < 20 && client.ConnectCallCount < 1; i++)
            {
                await Task.Delay(50);
            }
            Assert.Equal(1, client.ConnectCallCount);
            Assert.True(client.IsConnected);

            // ====== 周期 2 ======
            // 再次发生断开，旧代码由于 StopTimer() 销毁了 timer，此后永远无法重连！
            // 修复后定时器仅暂停，再次断开时将继续恢复重连
            client.Disconnect(new DisconnectArgument(DisconnectionReasonCode.Passive));
            Assert.False(client.IsConnected);

            for (int i = 0; i < 20 && client.ConnectCallCount < 2; i++)
            {
                await Task.Delay(50);
            }
            Assert.Equal(2, client.ConnectCallCount);
            Assert.True(client.IsConnected);

            // ====== 周期 3 ======
            // 第三次断开验证
            client.Disconnect(new DisconnectArgument(DisconnectionReasonCode.Passive));
            Assert.False(client.IsConnected);

            for (int i = 0; i < 20 && client.ConnectCallCount < 3; i++)
            {
                await Task.Delay(50);
            }
            Assert.Equal(3, client.ConnectCallCount);
            Assert.True(client.IsConnected);

            client.Dispose();
        }

        [Fact]
        public async Task ReconnectPlugin_RespectsMaxTimes_AndInvokesFailedHandler()
        {
            var client = new MockPluginLifecycleClient();
            client.SimulateConnectFailure = true; // 模拟每次连接均失败

            bool failedHandlerCalled = false;
            // 最多重试 2 次
            client.UseAutoReconnect(delay: 50, times: 2, reconnectFailedHandler: c =>
            {
                failedHandlerCalled = true;
            });

            client.SimulateConnected();
            // 发生被动断开
            client.Disconnect(new DisconnectArgument(DisconnectionReasonCode.Passive));

            // 等待重试完成 (最长等待 1500ms)
            for (int i = 0; i < 30 && !failedHandlerCalled; i++)
            {
                await Task.Delay(50);
            }

            Assert.True(failedHandlerCalled, "超过最大重连次数后应触发重连失败回调");
            Assert.True(client.ConnectCallCount <= 3);

            client.Dispose();
        }

        [Fact]
        public async Task ReconnectPlugin_DisableAutoReconnect_StopsReconnection()
        {
            var client = new MockPluginLifecycleClient();
            client.UseAutoReconnect(delay: 50, times: 0);

            client.SimulateConnected();
            // 禁用重连
            client.DisableAutoReconnect();

            client.Disconnect(new DisconnectArgument(DisconnectionReasonCode.Passive));

            await Task.Delay(200);
            Assert.Equal(0, client.ConnectCallCount);

            client.Dispose();
        }
    }
}
