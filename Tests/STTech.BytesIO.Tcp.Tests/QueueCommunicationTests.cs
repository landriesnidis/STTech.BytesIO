using STTech.BytesIO.Core;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace STTech.BytesIO.Tcp.Tests
{
    public class MockBytesClient : BytesClient
    {
        public override bool IsConnected => true;

        public bool SimulateDelay { get; set; } = false;
        public int DelayMs { get; set; } = 1000;
        public int SendCount { get; private set; }

        public override ConnectResult Connect(ConnectArgument argument = null)
        {
            return new ConnectResult();
        }

        public override DisconnectResult Disconnect(DisconnectArgument argument = null)
        {
            return new DisconnectResult();
        }

        public override void Dispose()
        {
        }

        protected override async Task SendHandlerAsync(SendArgs data)
        {
            SendCount++;
            if (SimulateDelay)
            {
                await Task.Delay(DelayMs);
            }
        }

        protected override Task ReceiveDataHandleAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        protected override void ReceiveDataCompletedHandle()
        {
        }
    }

    public class QueueCommunicationTests
    {
        [Fact]
        public async Task TestImmediateCancellation()
        {
            var client = new MockBytesClient();
            client.SimulateDelay = true;
            client.DelayMs = 2000;

            // Start sending first packet (takes 2 seconds)
            var sendTask1 = client.SendAsync(new byte[] { 1 });

            // Enqueue second packet with CancellationToken
            var cts = new CancellationTokenSource();
            var options = new SendOptions { CancellationToken = cts.Token };
            var sendTask2 = client.SendAsync(new byte[] { 2 }, options);

            // Verify both are not completed yet
            Assert.False(sendTask1.IsCompleted);
            Assert.False(sendTask2.IsCompleted);

            // Cancel the second one immediately
            cts.Cancel();

            // The second sendTask should complete immediately as Canceled
            var completedTask = await Task.WhenAny(sendTask2, Task.Delay(500));
            Assert.Same(sendTask2, completedTask);
            Assert.True(sendTask2.IsCanceled);

            // First task should still be running
            Assert.False(sendTask1.IsCompleted);
        }

        [Fact]
        public async Task TestClearSendQueue()
        {
            var client = new MockBytesClient();
            client.SimulateDelay = true;
            client.DelayMs = 2000;

            // Start first sending
            var sendTask1 = client.SendAsync(new byte[] { 1 });

            // Enqueue subsequent send tasks
            var sendTask2 = client.SendAsync(new byte[] { 2 });
            var sendTask3 = client.SendAsync(new byte[] { 3 });

            // Clear the send queue
            client.ClearSendQueue();

            // The pending tasks should immediately be canceled
            var completed2 = await Task.WhenAny(sendTask2, Task.Delay(500));
            var completed3 = await Task.WhenAny(sendTask3, Task.Delay(500));

            Assert.Same(sendTask2, completed2);
            Assert.Same(sendTask3, completed3);

            Assert.True(sendTask2.IsCanceled);
            Assert.True(sendTask3.IsCanceled);

            // Send task 1 was already in progress (SendHandlerAsync invoked), so it is not in the queue and won't be canceled
            Assert.False(sendTask1.IsCanceled);
        }
    }
}
