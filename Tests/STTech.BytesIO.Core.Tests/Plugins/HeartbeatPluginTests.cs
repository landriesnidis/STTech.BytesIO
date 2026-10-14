using STTech.BytesIO.Core;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace STTech.BytesIO.Core.Tests.Plugins
{
    /// <summary>
    /// 测试客户端桩：用于测试心跳与超时看门狗行为
    /// </summary>
    internal class MockHeartbeatClient : BytesClient
    {
        private bool _isConnected;
        public DisconnectionReasonCode? LastDisconnectReason { get; set; }

        public override bool IsConnected => _isConnected;

        public void SimulateConnected()
        {
            _isConnected = true;
            GenerateNewConnectionId();
            RaiseConnectedSuccessfully(this, new ConnectedSuccessfullyEventArgs());
        }

        public override ConnectResult Connect(ConnectArgument? argument = null)
        {
            _isConnected = true;
            GenerateNewConnectionId();
            RaiseConnectedSuccessfully(this, new ConnectedSuccessfullyEventArgs());
            return new ConnectResult();
        }

        public override DisconnectResult Disconnect(DisconnectArgument? argument = null)
        {
            if (!_isConnected)
            {
                return new DisconnectResult(DisconnectErrorCode.NoConnection);
            }

            _isConnected = false;
            argument ??= new DisconnectArgument();
            LastDisconnectReason = argument.ReasonCode;
            var args = new DisconnectedEventArgs(argument.ReasonCode, argument.Exception);
            RaiseDisconnected(this, args);
            return new DisconnectResult();
        }

        protected override Task SendHandlerAsync(SendArgs args) => Task.CompletedTask;
        protected override void ReceiveDataCompletedHandle() { }
        protected override Task ReceiveDataHandleAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public void SimulateReceiveData(byte[] data)
        {
            UpdateLastMessageTimestamp();
            RaiseDataReceived(this, new DataReceivedEventArgs(new ReceiveContext(data), 0));
        }

        public override void Dispose()
        {
            Disconnect();
        }
    }

    /// <summary>
    /// 心跳包发送与超时断开检测插件（HeartbeatPlugin）单元测试
    /// </summary>
    public class HeartbeatPluginTests
    {
        [Fact]
        public void TimeoutTimer_ResetInterval_AllowsSubSecondValues()
        {
            // 旧代码将 Interval 强制限制在 interval > 1000 ? interval : 1000
            // 导致即使设置短超时，Reset 也会将其篡改为至少 1000ms
            var timer = new TimeoutTimer(500, () => { });
            timer.Reset(150);
            Assert.Equal(150, timer.Interval);

            timer.Reset(50);
            Assert.Equal(50, timer.Interval);

            timer.Dispose();
        }

        [Fact]
        public async Task HeartbeatPlugin_DetectsTimeout_AndDisconnectsClient()
        {
            var client = new MockHeartbeatClient();
            // 设置 150ms 超时检测
            client.UseHeartbeatTimeout(150);

            client.SimulateConnected();
            Assert.True(client.IsConnected);

            // 等待超过 150ms 未收到数据，心跳插件应主动以 Timeout 原因断开客户端 (最长等待 1000ms)
            for (int i = 0; i < 20 && client.IsConnected; i++)
            {
                await Task.Delay(50);
            }

            Assert.False(client.IsConnected);
            Assert.Equal(DisconnectionReasonCode.Timeout, client.LastDisconnectReason);

            client.Dispose();
        }

        [Fact]
        public async Task HeartbeatPlugin_ReceivingData_ResetsTimeoutTimer()
        {
            var client = new MockHeartbeatClient();
            // 设置 2000ms 超时，确保多线程测试环境下的绝对防抖
            client.UseHeartbeatTimeout(2000);

            client.SimulateConnected();
            Assert.True(client.IsConnected);

            // 每隔 100ms 模拟接收到一次数据，总共刷新 3 次
            // 每次接收数据都应成功重置看门狗计时器，因此客户端应始终保持在线
            for (int i = 0; i < 3; i++)
            {
                await Task.Delay(100);
                client.SimulateReceiveData(new byte[] { 0x01 });
                Assert.True(client.IsConnected, $"在第 {i + 1} 次刷新心跳时客户端应仍处于连接状态");
            }

            // 此后停止发送数据，等待超时生效并断开连接 (最长等待 4000ms)
            for (int i = 0; i < 80 && client.IsConnected; i++)
            {
                await Task.Delay(50);
            }
            Assert.False(client.IsConnected, "停止接收数据后客户端应因心跳超时而断开");
            Assert.Equal(DisconnectionReasonCode.Timeout, client.LastDisconnectReason);

            client.Dispose();
        }

        [Fact]
        public async Task HeartbeatPlugin_SendHeartbeat_PeriodicallyInvokesCallback()
        {
            var client = new MockHeartbeatClient();
            int sendHeartbeatCount = 0;

            // 启用心跳发送，间隔 60ms
            client.UseHeartbeat(c =>
            {
                Interlocked.Increment(ref sendHeartbeatCount);
            }, interval: 60);

            client.SimulateConnected();

            // 等待心跳发送触发 (最长等待 1000ms)
            for (int i = 0; i < 20 && sendHeartbeatCount < 2; i++)
            {
                await Task.Delay(50);
            }

            Assert.True(sendHeartbeatCount >= 2, $"心跳发送应被定时周期调用，实际调用次数: {sendHeartbeatCount}");

            client.Dispose();
        }
    }
}
