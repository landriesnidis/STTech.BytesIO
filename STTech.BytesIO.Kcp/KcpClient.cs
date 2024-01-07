using KcpSharp;
using STTech.BytesIO.Core;
using System;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using System.Net.Security;
using System.Threading.Tasks;

namespace STTech.BytesIO.Kcp
{
    public interface IKcpClient
    {
        /// <summary>
        ///远程主机网络地址
        /// </summary>
        string Host { get; set; }

        /// <summary>
        /// 远程主机端口号
        /// </summary>
        int Port { get; set; }
        int ConversationId { get; set; }
    }
    public partial class KcpClient : BytesClient
    {
        /// <inheritdoc/>
        public override bool IsConnected => InnerClient != null;

        /// <summary>
        /// 内部KCP连接
        /// </summary>
        protected KcpConversation InnerClient { get; set; }

        public override ConnectResult Connect(ConnectArgument argument = null)
        {
            try
            {
                using var socket = new Socket(SocketType.Dgram, ProtocolType.Udp);

                EndPoint endPoint = new IPEndPoint(IPAddress.Parse(Host), Port);

                socket.Connect(endPoint);
                using var transport = KcpSocketTransport.CreateConversation(socket, endPoint, ConversationId, null);
                transport.Start();

                InnerClient = transport.Connection;

                return new ConnectResult();
            }
            catch (Exception ex)
            {
                return new ConnectResult(ConnectErrorCode.Error, ex);
            }
        }

        public override DisconnectResult Disconnect(DisconnectArgument argument = null)
        {
            if (InnerClient == null)
            {
                return new DisconnectResult(DisconnectErrorCode.NoConnection);
            }

            try
            {
                InnerClient.SetTransportClosed();
                return new DisconnectResult();
            }
            catch (Exception ex)
            {
                return new DisconnectResult(DisconnectErrorCode.Error, ex);
            }
            finally
            {
                InnerClient = null;
            }
        }

        public override void Dispose()
        {
            Disconnect();
            InnerClient?.Dispose();
        }

        protected override void ReceiveDataCompletedHandle()
        {
            Disconnect(new DisconnectArgument(DisconnectionReasonCode.Passive));
        }

        protected override async void ReceiveDataHandle()
        {
            try
            {
                byte[] buffer = new byte[MaxMessageSize];
                while (IsConnected)
                {
                    var result = await InnerClient.ReceiveAsync(buffer, CancellationToken.None);

                    if (result.TransportClosed)
                    {
                        break;
                    }

                    var data = buffer.AsMemory(0, result.BytesReceived).ToArray();
                    InvokeDataReceivedEventCallback(data);
                }
            }
            catch (Exception ex)
            {
                // 回调异常事件
                RaiseExceptionOccurs(this, new ExceptionOccursEventArgs(ex));

                return;
            }
        }

        protected override void SendHandler(SendArgs args)
        {
            try
            {
                InnerClient.SendAsync(args.Data).AsTask().Wait();

                // 执行数据已发送的回调事件
                RaiseDataSent(this, new DataSentEventArgs(args.Data));

                // 延时
                Task.Delay(args.Options.PauseTime).Wait();
            }
            catch (Exception ex)
            {
                // 通信异常
                RaiseExceptionOccurs(this, new ExceptionOccursEventArgs(ex));
            }
        }
    }

    public partial class KcpClient : IKcpClient
    {
        public int ConversationId { get; set; } = 1;
        public string Host { get; set; } = "127.0.0.1";
        public int Port { get; set; } = 5000;

        private const int MaxMessageSize = 16384; // limit of the maximum message size.

        public override int ReceiveBufferSize
        {
            get => base.ReceiveBufferSize;
            set
            {
                if (value < 50 || value > MaxMessageSize)
                {
                    throw new ArgumentException($"KCP ReceiveBufferSize: [50,{MaxMessageSize}].");
                }
                base.ReceiveBufferSize = value;
            }
        }
    }
}
