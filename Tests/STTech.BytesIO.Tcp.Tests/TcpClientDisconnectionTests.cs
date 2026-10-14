using STTech.BytesIO.Core;
using STTech.BytesIO.Tcp;
using System;
using System.Threading.Tasks;
using Xunit;

namespace STTech.BytesIO.Tcp.Tests
{
    /// <summary>
    /// TCP客户端断开连接、幂等性、防重入与发送状态检查单元测试
    /// </summary>
    public class TcpClientDisconnectionTests
    {
        [Fact]
        public void TestRepeatedDisconnectReturnsNoConnectionWithoutRecursion()
        {
            var client = new TcpClient();
            Assert.False(client.IsConnected);

            // 多次重复调用 Disconnect 必须是绝对安全的幂等操作，且返回 NoConnection
            for (int i = 0; i < 50; i++)
            {
                var result = client.Disconnect();
                Assert.False(result.IsSuccess);
                Assert.Equal(DisconnectErrorCode.NoConnection, result.ErrorCode);
            }
        }

        [Fact]
        public void TestReentrantDisconnectInEventHandlerDoesNotCauseStackOverflow()
        {
            var client = new TcpClient();

            // 模拟用户或插件在 OnDisconnected 事件处理中再次触发 client.Disconnect()
            int callbackCount = 0;
            client.OnDisconnected += (s, e) =>
            {
                callbackCount++;
                // 再次调用 Disconnect，内部防重入机制必须阻止死循环递归调用
                client.Disconnect();
            };

            // 首次断开未连接客户端
            var result = client.Disconnect();
            Assert.Equal(DisconnectErrorCode.NoConnection, result.ErrorCode);

            // 回调不应被死循环调用
            Assert.True(callbackCount <= 1);
        }

        [Fact]
        public void TestDisconnectResetsInnerStatusSafely()
        {
            var client = new TcpClient();
            // 验证 Disconnect 发生后对象处于干净的断开态，可再次安全释放与调用
            client.Disconnect();
            Assert.False(client.IsConnected);
            client.Dispose();
        }

        [Fact]
        public async Task TestSendAsyncWhenDisconnectedThrowsInvalidOperationException()
        {
            var client = new TcpClient();
            Assert.False(client.IsConnected);

            // 客户端在未连接状态下调用 SendAsync 必须抛出 InvalidOperationException 而不能静默成功
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await client.SendAsync(new byte[] { 1, 2, 3 });
            });
        }

        [Fact]
        public void TestSendWhenDisconnectedThrowsInvalidOperationException()
        {
            var client = new TcpClient();
            Assert.False(client.IsConnected);

            // 客户端在未连接状态下调用同步 Send 必须向调用方抛出异常
            Assert.Throws<InvalidOperationException>(() =>
            {
                client.Send(new byte[] { 1, 2, 3 });
            });
        }
    }
}
