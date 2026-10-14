using STTech.BytesIO.Core;
using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace STTech.BytesIO.Core.Tests
{
    /// <summary>
    /// 模拟测试客户端：用于精确验证数据发送管线与异常抛出行为
    /// </summary>
    internal class TestSenderClient : BytesClient
    {
        private bool _isConnected;
        public Exception? ExceptionToThrow { get; set; }
        public int SentCount { get; private set; }

        public override bool IsConnected => _isConnected;

        public void SetConnected(bool connected)
        {
            _isConnected = connected;
            if (connected)
            {
                GenerateNewConnectionId();
                RaiseConnectedSuccessfully(this, new ConnectedSuccessfullyEventArgs());
            }
        }

        public override ConnectResult Connect(ConnectArgument? argument = null)
        {
            _isConnected = true;
            GenerateNewConnectionId();
            return new ConnectResult();
        }

        public override DisconnectResult Disconnect(DisconnectArgument? argument = null)
        {
            _isConnected = false;
            return new DisconnectResult();
        }

        public override void Dispose()
        {
            Disconnect();
        }

        protected override Task SendHandlerAsync(SendArgs args)
        {
            if (!IsConnected)
            {
                throw new InvalidOperationException("客户端未连接，无法发送数据。");
            }

            if (ExceptionToThrow != null)
            {
                // 通信异常重新抛出，传递给框架任务与上层调用者
                throw ExceptionToThrow;
            }

            SentCount++;
            return Task.CompletedTask;
        }

        protected override void ReceiveDataCompletedHandle() { }

        protected override Task ReceiveDataHandleAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    /// <summary>
    /// 客户端数据发送器（DataSender）与异常传递机制单元测试
    /// </summary>
    public class DataSenderTests
    {
        [Fact]
        public async Task SendAsync_WhenDisconnected_ThrowsInvalidOperationException()
        {
            var client = new TestSenderClient();
            Assert.False(client.IsConnected);

            // 未连接时发送，底层不得静默吞并，必须抛出 InvalidOperationException
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await client.SendAsync(new byte[] { 1, 2, 3 });
            });
        }

        [Fact]
        public void Send_WhenDisconnected_ThrowsInvalidOperationException()
        {
            var client = new TestSenderClient();
            Assert.False(client.IsConnected);

            // 同步发送未连接时也必须抛出异常
            Assert.Throws<InvalidOperationException>(() =>
            {
                client.Send(new byte[] { 1, 2, 3 });
            });
        }

        [Fact]
        public async Task SendAsync_WhenUnderlyingSendFails_RethrowsSocketException_WithoutSwallowing()
        {
            var client = new TestSenderClient();
            client.SetConnected(true);
            Assert.True(client.IsConnected);

            // 配置底层发送故障
            var expectedEx = new SocketException((int)SocketError.ConnectionAborted);
            client.ExceptionToThrow = expectedEx;

            // 验证异常直接传递给调用方任务，而非被静默忽略
            var thrown = await Assert.ThrowsAsync<SocketException>(async () =>
            {
                await client.SendAsync(new byte[] { 0xAA, 0xBB });
            });

            Assert.Equal(expectedEx.SocketErrorCode, thrown.SocketErrorCode);
        }

        [Fact]
        public async Task SendAsync_WhenUnderlyingSendThrowsIOException_PropagatesToCaller()
        {
            var client = new TestSenderClient();
            client.SetConnected(true);

            client.ExceptionToThrow = new IOException("底层网络流已被断开。");

            await Assert.ThrowsAsync<IOException>(async () =>
            {
                await client.SendAsync(new byte[] { 0x01, 0x02 });
            });
        }

        [Fact]
        public async Task SendAsync_WhenConnectedAndNormal_SuccessfullyIncrementsCount()
        {
            var client = new TestSenderClient();
            client.SetConnected(true);

            await client.SendAsync(new byte[] { 0x10, 0x20 });
            client.Send(new byte[] { 0x30, 0x40 });

            Assert.Equal(2, client.SentCount);
        }
    }
}
