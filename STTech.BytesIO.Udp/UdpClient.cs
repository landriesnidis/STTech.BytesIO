using STTech.BytesIO.Core;
using System;
using System.Net.Sockets;

namespace STTech.BytesIO.Udp
{

    public class UdpClient : BytesClient, IUdpClient
    {
        /// <summary>
        /// 内部UDP客户端
        /// </summary>
        protected System.Net.Sockets.UdpClient InnerClient { get; set; }

        public System.Net.Sockets.UdpClient GetInnerClient() => InnerClient;

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public string Host { get; set; } = "127.0.0.1";

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public int Port { get; set; } = 8080;

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public int LocalPort { get; set; } = 0;

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public override bool IsConnected => InnerClient != null;

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public bool AllowReceivingDataFromAnyIP { get; set; } = false;

        /// <summary>
        /// 在接收到数据时发生
        /// </summary>
        public event EventHandler<UdpDataReceivedEventArgs> OnUdpDataReceived;


        public UdpClient()
        {
            SendBufferSize = 65536;
            ReceiveBufferSize = 65536;
        }

        public override ConnectResult Connect(ConnectArgument argument = null)
        {
            if (InnerClient == null)
            {
                try
                {
                    InnerClient = new System.Net.Sockets.UdpClient(LocalPort);
                    StartReceiveDataTask();

                    RaiseConnectedSuccessfully(this, new ConnectedSuccessfullyEventArgs());

                    return new ConnectResult();
                }
                catch (Exception ex)
                {
                    RaiseConnectionFailed(this, new ConnectionFailedEventArgs(ex));

                    return new ConnectResult(ConnectErrorCode.Error, ex);
                }
            }
            else
            {
                return new ConnectResult(ConnectErrorCode.IsConnected);
            }
        }

        public override DisconnectResult Disconnect(DisconnectArgument argument = null)
        {
            if (InnerClient != null)
            {
                InnerClient?.Close();
                InnerClient = null;

                // 关闭异步任务
                CancelReceiveDataTask();

                RaiseDisconnected(this, new DisconnectedEventArgs(argument.ReasonCode, argument.Exception));

                return new DisconnectResult();
            }
            else
            {
                return new DisconnectResult(DisconnectErrorCode.NoConnection);
            }
        }

        protected override void ReceiveDataCompletedHandle()
        {
            Disconnect();
        }

        protected override void ReceiveDataHandle()
        {
            while (true)
            {
                try
                {
#if NET6_0_OR_GREATER
                    var task = InnerClient.ReceiveAsync(ReceiveTaskCancellationTokenSource.Token);
#else
                    var task = InnerClient.ReceiveAsync();
                    task.Wait(ReceiveTaskCancellationTokenSource.Token);
#endif
                    if (task.IsCanceled)
                    {
                        break;
                    }
                    else if (task.IsCompleted)
                    {
                        RaiseUdpDataReceived(this, new UdpDataReceivedEventArgs(task.Result));
                    }
                    else if (task.IsFaulted)
                    {
#if NET6_0_OR_GREATER
                        RaiseExceptionOccurs(this, new ExceptionOccursEventArgs(new Exception("An exception occurred while receiving data.")));
#else
                        RaiseExceptionOccurs(this, new ExceptionOccursEventArgs(task.Exception));
#endif
                    }
                }
                catch (OperationCanceledException ex)
                {
                    break;
                }
            }
        }

        protected override void SendHandler(byte[] data)
        {
            InnerClient.Send(data, data.Length, Host, Port);
        }

        /// <summary>
        /// 触发数据已接受事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected virtual void RaiseUdpDataReceived(object sender, UdpDataReceivedEventArgs e)
        {
            if (!AllowReceivingDataFromAnyIP)
            {
                if (e.ReceiveResult.RemoteEndPoint.Address.ToString() != Host)
                {
                    // 消息被过滤
                    return;
                }
            }

            RaiseDataReceived(sender, e);
            SafelyInvokeCallback(() => { OnUdpDataReceived?.Invoke(sender, e); });
        }
    }
}
