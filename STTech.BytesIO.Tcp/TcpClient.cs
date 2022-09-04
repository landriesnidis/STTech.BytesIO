using STTech.BytesIO.Core;
using STTech.BytesIO.Core.Entity;
using STTech.BytesIO.Tcp.Entity;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace STTech.BytesIO.Tcp
{
    /// <summary>
    /// TCP通信客户端
    /// </summary>
    public partial class TcpClient : BytesClient, ITcpClient
    {
        /// <summary>
        /// 内部TCP客户端
        /// </summary>
        protected System.Net.Sockets.TcpClient InnerClient { get; set; }
        public System.Net.Sockets.TcpClient GetInnerClient() => InnerClient;
        /// <summary>
        /// 接受缓存区
        /// </summary>
        protected byte[] socketDataReceiveBuffer = null;

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public override bool IsConnected => InnerClient != null && InnerClient.Client != null && InnerClient.Client.Connected;

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public override int ReceiveBufferSize { get; set; } = 65536;

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public override int SendBufferSize { get; set; } = 32768;


        /// <summary>
        /// 构造TCP客户端
        /// </summary>
        public TcpClient()
        {
            InnerClient = new System.Net.Sockets.TcpClient();
        }

        /// <summary>
        /// 构造TCP客户端
        /// </summary>
        /// <param name="innerClient">内部的TCP客户端（<c>System.Net.Sockets.TcpClient</c>）</param>
        public TcpClient(System.Net.Sockets.TcpClient innerClient)
        {
            this.InnerClient = innerClient;

            if (innerClient.Connected)
            {
                IPEndPoint point = (IPEndPoint)innerClient.Client.RemoteEndPoint;
                Host = point.Address.ToString();
                Port = point.Port;
                socketDataReceiveBuffer = new byte[ReceiveBufferSize];
                // 启动接收数据的异步任务
                StartReceiveDataTask();
            }
        }

        /// <summary>
        /// 构造TCP客户端
        /// </summary>
        /// <param name="socket">内部的Socket对象</param>
        internal TcpClient(Socket socket) : this(new System.Net.Sockets.TcpClient { Client = socket })
        {
        }

        /// <summary>
        /// 异步建立连接
        /// </summary>
        public override void Connect()
        {

            // 如果client已经连接了，则此次连接无效
            if (InnerClient.Connected)
                return;

            try
            {
                // 创建数据接收缓冲区
                if (socketDataReceiveBuffer == null || ReceiveBufferSize != socketDataReceiveBuffer.Length)
                {
                    socketDataReceiveBuffer = null;
                    socketDataReceiveBuffer = new byte[ReceiveBufferSize];
                }

                // 建立连接
                InnerClient.ReceiveBufferSize = ReceiveBufferSize;
                InnerClient.SendBufferSize = SendBufferSize;
                InnerClient.Connect(Host, Port);

                // 是否使用SSL/TLS通信
                if (UseSsl)
                {
                    try
                    {
                        // 创建SSL流
                        SslStream = new SslStream(InnerClient.GetStream(), false, RemoteCertificateValidationHandle ?? RemoteCertificateValidateCallback, LocalCertificateSelectionHandle ?? LocalCertificateSelectionCallback, EncryptionPolicy.AllowNoEncryption);
                        SslStream.AuthenticateAsClient(ServerCertificateName, new X509CertificateCollection(new X509Certificate[] { Certificate }), SslProtocol, false);

                        // 执行TLS通信验证通过的回调事件
                        PerformTlsVerifySuccessfully(this, new TlsVerifySuccessfullyEventArgs(SslStream));
                    }
                    catch (Exception ex)
                    {
                        throw new Exception("SSL certificate validation failed.", ex);
                    }
                }

                // 执行连接成功回调事件
                RaiseConnectedSuccessfully(this, new ConnectedSuccessfullyEventArgs());

                // 启动接收数据的异步任务
                StartReceiveDataTask();
            }
            catch (Exception ex)
            {
                // 连接失败
                RaiseConnectionFailed(this, new ConnectionFailedEventArgs(ex.Message));

                // 重置tcp客户端
                InnerClient = new System.Net.Sockets.TcpClient();

                // 释放缓冲区
                // socketDataReceiveBuffer = null;
            }
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <param name="code"></param>
        /// <param name="ex"></param>
        public override void Disconnect(DisconnectionReasonCode code = DisconnectionReasonCode.Active, Exception ex = null)
        {
            // TODO: 主动关闭时这里被调用两次，需要找到第二次调用的原因

            // 如果TcpClient没有关闭，则关闭连接
            if (InnerClient.Connected)
            {
                // 关闭异步任务
                CancelReceiveDataTask();

                // 关闭内部Socket客户端
                InnerClient.Close();

                // 重置tcp客户端
                InnerClient = new System.Net.Sockets.TcpClient();

                // 执行通信已断开的回调事件 
                RaiseDisconnected(this, new DisconnectedEventArgs() { ReasonCode = code });
            }
            else
            {
                return;
            }
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <param name="data"></param>
        protected override void SendHandler(byte[] data)
        {
            try
            {
                if (UseSsl)
                {
                    SslStream.Write(data);
                    SslStream.Flush();
                }
                else
                {
                    // 发送数据
                    InnerClient.Client.Send(data);
                }
                // 执行数据已发送的回调事件
                RaiseDataSent(this, new DataSentEventArgs(data));
            }
            catch (Exception ex)
            {
                // 通信异常
                RaiseExceptionOccurs(this, new ExceptionOccursEventArgs(ex));
            }
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        protected override void ReceiveDataHandle()
        {
            try
            {
                int CheckTimes = 0;
                Stream stream = UseSsl ? SslStream : InnerClient.GetStream();
                while (IsConnected)
                {
                    // 获取数据长度
                    int len = stream.Read(socketDataReceiveBuffer, 0, socketDataReceiveBuffer.Length);

                    // 截取有效数据
                    byte[] data = socketDataReceiveBuffer.Take(len).ToArray();

                    if (data.Length == 0)
                    {
                        // 连续5次接收到空数据 则看作通信已断开
                        if (++CheckTimes > 5)
                            return;
                        else
                            continue;
                    }
                    else
                    {
                        CheckTimes = 0;
                    }

                    Task.Factory.StartNew(() =>
                    {
                        // 更新时间戳
                        UpdateLastMessageTimestamp();

                        // 执行接收到数据的回调事件
                        RaiseDataReceived(this, new Core.Entity.DataReceivedEventArgs(data));
                    });
                }
            }
            catch (Exception ex)
            {
                // 如果关闭了通信，不回调异常
                if (!InnerClient.Connected) return;

                // 回调异常事件
                RaiseExceptionOccurs(this, new ExceptionOccursEventArgs(ex));
            }
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        protected override void ReceiveDataCompletedHandle()
        {
            Disconnect(DisconnectionReasonCode.Passive);
        }
    }

    public partial class TcpClient : ITcpClient
    {
        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public string Host { get; set; } = "127.0.0.1";

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public int Port { get; set; } = 8086;

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public int LocalPort => IsConnected ? ((IPEndPoint)InnerClient.Client.LocalEndPoint).Port : 0;

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public IPEndPoint RemoteEndPoint => (IPEndPoint)InnerClient.Client.RemoteEndPoint;
    }
}
