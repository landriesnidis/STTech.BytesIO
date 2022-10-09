using STTech.BytesIO.Core;
using STTech.BytesIO.Core.Entity;
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
        public int LocalPort { get; set; } = 8080;

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public override bool IsConnected => InnerClient != null;

        /// <summary>
        /// 在接收到数据时发生
        /// </summary>
        public event EventHandler<UdpDataReceivedEventArgs> OnUdpDataReceived;


        public UdpClient()
        {
            SendBufferSize = 65536;
            ReceiveBufferSize = 65536;
        }

        public override void Connect()
        {
            if (InnerClient == null)
            {
                try
                {
                    InnerClient = new System.Net.Sockets.UdpClient(LocalPort);
                    StartReceiveDataTask();

                    RaiseConnectedSuccessfully(this, new ConnectedSuccessfullyEventArgs());
                }
                catch (Exception ex)
                {
                    RaiseConnectionFailed(this, new ConnectionFailedEventArgs(ex));
                    throw;
                }
            }
        }

        public override void Disconnect(DisconnectionReasonCode code = DisconnectionReasonCode.Active, Exception ex = null)
        {
            if (InnerClient != null)
            {
                InnerClient?.Close();
                InnerClient = null;

                // 关闭异步任务
                CancelReceiveDataTask();

                RaiseDisconnected(this, new DisconnectedEventArgs() { ReasonCode = DisconnectionReasonCode.Active });
            }
        }

        protected override void ReceiveDataCompletedHandle()
        {
            Disconnect(DisconnectionReasonCode.Active);
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
            RaiseDataReceived(sender, e);
            SafelyInvokeCallback(() => { OnUdpDataReceived?.Invoke(sender, e); });
        }
    }

    /// <summary>
    /// UDP数据接收事件参数
    /// </summary>
    public class UdpDataReceivedEventArgs : DataReceivedEventArgs
    {
        /// <summary>
        /// 接收到的字节数组
        /// </summary>
        public override byte[] Data => ReceiveResult.Buffer;

        /// <summary>
        /// UDP接收结果
        /// </summary>
        public UdpReceiveResult ReceiveResult { get; }

        public UdpDataReceivedEventArgs(UdpReceiveResult receiveResult)
        {
            ReceiveResult = receiveResult;
        }
    }
}
