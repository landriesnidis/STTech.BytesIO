using STTech.BytesIO.Core.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace STTech.BytesIO.Core
{
    /// <summary>
    /// 虚拟客户端
    /// </summary>
    public class VirtualClient : VirtualClient<BytesClient>
    {

    }

    /// <summary>
    /// 虚拟客户端
    /// </summary>
    /// <typeparam name="T">内部客户端类型</typeparam>
    public class VirtualClient<T> : BytesClient where T : BytesClient
    {
        /// <summary>
        /// 内部客户端
        /// </summary>
        public T InnerClient
        {
            get { return innerClient; }
            set
            {
                if (innerClient == value)
                {
                    return;
                }

                if (innerClient != null)
                {
                    innerClient.OnConnectedSuccessfully -= InnerClient_OnConnectedSuccessfully;
                    innerClient.OnConnectionFailed -= InnerClient_OnConnectionFailed;
                    innerClient.OnDisconnected -= InnerClient_OnDisconnected;
                    innerClient.OnDataReceived -= InnerClient_OnDataReceived;
                    innerClient.OnDataSent -= InnerClient_OnDataSent;
                    innerClient.OnExceptionOccurs -= InnerClient_OnExceptionOccurs;
                }

                innerClient = value;
                if (innerClient != null)
                {
                    innerClient.OnConnectedSuccessfully += InnerClient_OnConnectedSuccessfully;
                    innerClient.OnConnectionFailed += InnerClient_OnConnectionFailed;
                    innerClient.OnDisconnected += InnerClient_OnDisconnected;
                    innerClient.OnDataReceived += InnerClient_OnDataReceived;
                    innerClient.OnDataSent += InnerClient_OnDataSent;
                    innerClient.OnExceptionOccurs += InnerClient_OnExceptionOccurs;
                }
            }
        }
        private T innerClient;

        /// <summary>
        /// 构造虚拟客户端
        /// </summary>
        public VirtualClient() { }

        /// <summary>
        /// 构造虚拟客户端
        /// </summary>
        /// <param name="innerClient">内部客户端类型</param>
        public VirtualClient(T innerClient) : this()
        {
            InnerClient = innerClient;
        }

        private void InnerClient_OnExceptionOccurs(object sender, ExceptionOccursEventArgs e) => RaiseExceptionOccurs(sender, e);
        private void InnerClient_OnDataSent(object sender, DataSentEventArgs e) => RaiseDataSent(sender, e);
        private void InnerClient_OnDataReceived(object sender, DataReceivedEventArgs e) => RaiseDataReceived(sender, e);
        private void InnerClient_OnDisconnected(object sender, DisconnectedEventArgs e) => RaiseDisconnected(sender, e);
        private void InnerClient_OnConnectionFailed(object sender, ConnectionFailedEventArgs e) => RaiseConnectionFailed(sender, e);
        private void InnerClient_OnConnectedSuccessfully(object sender, ConnectedSuccessfullyEventArgs e) => RaiseConnectedSuccessfully(sender, e);

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public override void Connect()
        {
            InnerClient.Connect();
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public override void Disconnect(DisconnectionReasonCode code = DisconnectionReasonCode.Active, Exception ex = null)
        {
            InnerClient.Disconnect(code, ex);
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        protected override void SendHandler(byte[] data)
        {
            InnerClient.Send(data);
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        protected override void ReceiveDataCompletedHandle()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        protected override void ReceiveDataHandle()
        {
            throw new NotImplementedException();
        }


    }
}
