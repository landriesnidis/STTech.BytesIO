using STTech.BytesIO.Core;
using STTech.BytesIO.Core.Exceptions;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace STTech.BytesIO.Core.Tests
{
    internal class ReplyMockClient : BytesClient
    {
        private bool _connected = true;
        public override bool IsConnected => _connected;

        public override ConnectResult Connect(ConnectArgument? argument = null)
        {
            _connected = true;
            GenerateNewConnectionId();
            return new ConnectResult();
        }

        public override DisconnectResult Disconnect(DisconnectArgument? argument = null)
        {
            _connected = false;
            RaiseDisconnected(this, new DisconnectedEventArgs(DisconnectionReasonCode.Passive));
            return new DisconnectResult();
        }

        public override void Dispose() => Disconnect();

        protected override Task SendHandlerAsync(SendArgs args)
        {
            return Task.CompletedTask;
        }

        protected override void ReceiveDataCompletedHandle() { }

        protected override Task ReceiveDataHandleAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public void SimulateReceive(byte[] data)
        {
            RaiseDataReceived(this, new DataReceivedEventArgs(new ReceiveContext(data), 1));
        }
    }

    public class DummyResponse : Response
    {
        public DummyResponse(byte[] originalData) : base(originalData) { }
    }

    public class AnotherResponse : Response
    {
        public AnotherResponse(byte[] originalData) : base(originalData) { }
    }

    public class ReplyTests
    {
        [Fact]
        public async Task SendAsync_MatchHandlerSucceeds_ShouldReturnCompletedReply()
        {
            using var client = new ReplyMockClient();
            byte[] req = new byte[] { 0x01, 0x02 };
            byte[] expectedResp = new byte[] { 0x01, 0x82 };

            var replyTask = client.SendAsync(req, timeout: 2000, matchHandler: (send, recv) =>
            {
                return recv.Length == 2 && recv[0] == send[0] && recv[1] == 0x82;
            });

            // 模拟收到无关的心跳包
            client.SimulateReceive(new byte[] { 0xFF, 0xFF });
            Assert.False(replyTask.IsCompleted);

            // 模拟收到目标响应
            client.SimulateReceive(expectedResp);

            var reply = await replyTask;
            Assert.True(reply.IsCompleted);
            Assert.False(reply.IsTimeout);
            Assert.False(reply.IsInterrupted);
            Assert.Equal(expectedResp, reply.GetBytes().ToArray());
        }

        [Fact]
        public async Task SendAsync_WhenNoResponseBeforeTimeout_ShouldReturnTimeoutReply()
        {
            using var client = new ReplyMockClient();
            byte[] req = new byte[] { 0xAA };

            var reply = await client.SendAsync(req, timeout: 50, matchHandler: (send, recv) => true);

            Assert.True(reply.IsTimeout);
            Assert.False(reply.IsCompleted);
            Assert.Equal(ReplyStatus.Timeout, reply.Status);
        }

        [Fact]
        public async Task SendAsync_WhenDisconnectedDuringWaiting_ShouldReturnInterruptedReply()
        {
            using var client = new ReplyMockClient();
            byte[] req = new byte[] { 0x01 };

            var replyTask = client.SendAsync(req, timeout: 5000, matchHandler: (send, recv) => true);

            // 在等待响应期间主动断开连接
            client.Disconnect();

            var reply = await replyTask;
            Assert.True(reply.IsInterrupted);
            Assert.False(reply.IsCompleted);
            Assert.Equal(ReplyStatus.Interrupted, reply.Status);
        }

        [Fact]
        public void ReplyConversion_ConvertTo_ShouldWrapTargetResponseType()
        {
            using var client = new ReplyMockClient();
            var originData = new byte[] { 1, 2, 3 };
            var baseReply = new Reply<DummyResponse>(client, new DummyResponse(originData));

            var converted = baseReply.ConvertTo(resp => new AnotherResponse(resp.GetOriginalData()));

            Assert.True(converted.IsCompleted);
            Assert.IsType<AnotherResponse>(converted.GetResponse());
            Assert.Equal(originData, converted.GetResponse().GetOriginalData());
        }

        [Fact]
        public void ReplyConversion_ConvertToGeneric_ShouldInstantiateThroughConstructor()
        {
            using var client = new ReplyMockClient();
            var originData = new byte[] { 9, 8, 7 };
            var baseReply = new Reply<DummyResponse>(client, new DummyResponse(originData));

            var converted = baseReply.ConvertTo<AnotherResponse>();

            Assert.True(converted.IsCompleted);
            Assert.Equal(originData, converted.GetResponse().GetOriginalData());
        }

        [Fact]
        public void ReplyConversion_FailedStatus_ShouldPreserveErrorStatus()
        {
            using var client = new ReplyMockClient();
            var baseReply = new Reply<DummyResponse>(client, ReplyStatus.Timeout, new TimeoutException());

            var converted = baseReply.ConvertTo<AnotherResponse>();

            Assert.True(converted.IsTimeout);
            Assert.Equal(ReplyStatus.Timeout, converted.Status);
        }
    }
}
