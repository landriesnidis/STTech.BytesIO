using STTech.BytesIO.Core;
using STTech.BytesIO.Tcp.Entity;
using System;
using System.Collections.Concurrent;
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
        protected Socket InnerClient { get; set; }

        /// <summary>
        /// 获取内部的TCP客户端
        /// </summary>
        /// <returns></returns>
        public Socket GetInnerClient() => InnerClient;

        /// <summary>
        /// 接受缓存区
        /// </summary>
        protected byte[] socketDataReceiveBuffer = null;

        /// <inheritdoc/>
        public override bool IsConnected => InnerClient != null && InnerClient != null && InnerClient.Connected;

        /// <inheritdoc/>
        public override int ReceiveBufferSize { get; set; } = 65536;

        /// <inheritdoc/>
        public override int SendBufferSize { get; set; } = 32768;

        /// <summary>
        /// 内部状态
        /// </summary>
        private InnerStatus innerStatus = InnerStatus.Free;

        /// <summary>
        /// 状态锁
        /// </summary>
        private object lockerStatus = new object();

        /// <summary>
        /// 构造TCP客户端
        /// </summary>
        public TcpClient()
        {
            InnerClient = CreateDefaultSocket();
        }

        private Socket CreateDefaultSocket() => new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
        {

        };

        /// <summary>
        /// 构造TCP客户端
        /// </summary>
        /// <param name="innerClient">内部的TCP客户端（<c>System.Net.Sockets.TcpClient</c>）</param>
        public TcpClient(Socket socket)
        {
            this.InnerClient = socket;

            if (socket.Connected)
            {
                // 设置内部状态为忙碌
                innerStatus = InnerStatus.Busy;

                IPEndPoint point = (IPEndPoint)socket.RemoteEndPoint;
                Host = point.Address.ToString();
                Port = point.Port;
                socketDataReceiveBuffer = new byte[ReceiveBufferSize];
                // 启动接收数据的异步任务
                StartReceiveDataTask();
            }
        }

        /// <summary>
        /// 异步建立连接
        /// </summary>
        public override ConnectResult Connect(ConnectArgument argument = null)
        {
            argument ??= new ConnectArgument();

            Socket socket;

            lock (lockerStatus)
            {
                // 如果client已经连接了，则此次连接无效
                if (InnerClient.Connected || innerStatus == InnerStatus.Busy)
                {
                    RaiseConnectionFailed(this, new ConnectionFailedEventArgs(ConnectErrorCode.IsConnected));
                    return new ConnectResult(ConnectErrorCode.IsConnected);
                }

                // 设置内部状态为忙碌
                innerStatus = InnerStatus.Busy;

                socket = InnerClient;
            }

            try
            {
                // 创建数据接收缓冲区
                if (socketDataReceiveBuffer == null || ReceiveBufferSize != socketDataReceiveBuffer.Length)
                {
                    socketDataReceiveBuffer = null;
                    socketDataReceiveBuffer = new byte[ReceiveBufferSize];
                }

                // 建立连接
                socket.ReceiveBufferSize = ReceiveBufferSize;
                socket.SendBufferSize = SendBufferSize;

                // 连接是否完成
                var connectTask = Task.Run(() =>
                {
                    try
                    {
                        socket.Connect(Host, Port);
                    }
                    catch (SocketException ex)
                    {
                        return ex;
                    }
                    return null;
                });

                var completedTaskIndex = Task.WaitAny(connectTask, Task.Delay(argument.Timeout));

                // 如果超时，则返回超时结果
                if (completedTaskIndex == 1)
                {
                    connectTask.ContinueWith(t =>
                    {
                        try
                        {
                            if (t.Result == null && socket.Connected)
                            {
                                socket.Disconnect(false);
                            }
                        }
                        catch (Exception ex)
                        {

                        }
                    });

                    // 设置内部状态为空闲
                    innerStatus = InnerStatus.Free;

                    RaiseConnectionFailed(this, new ConnectionFailedEventArgs(ConnectErrorCode.Timeout));
                    return new ConnectResult(ConnectErrorCode.Timeout);
                }

                // 如果连接失败了则抛出异常信息
                if (connectTask.Result != null)
                {
                    throw connectTask.Result;
                }

                // 是否使用SSL/TLS通信
                if (UseSsl)
                {
                    try
                    {
                        InitializeSslStream();
                    }
                    catch (Exception ex)
                    {
                        RaiseConnectionFailed(this, new ConnectionFailedEventArgs(ConnectErrorCode.Error, ex));
                        return new ConnectResult(ConnectErrorCode.Error, ex);
                    }
                }

                // 执行连接成功回调事件
                RaiseConnectedSuccessfully(this, new ConnectedSuccessfullyEventArgs());

                // 启动接收数据的异步任务
                StartReceiveDataTask();

                // 设置内部状态为空闲
                innerStatus = InnerStatus.Free;

                return new ConnectResult();
            }
            catch (Exception ex)
            {
                // 重置tcp客户端
                ResetInnerClient();

                // 设置内部状态为空闲
                innerStatus = InnerStatus.Free;

                // 返回操作错误结果
                if (ex is SocketException socketEx)
                {
                    switch (socketEx.SocketErrorCode)
                    {
                        case SocketError.HostNotFound:
                            RaiseConnectionFailed(this, new ConnectionFailedEventArgs(ConnectErrorCode.ConnectionParameterError, ex));
                            return new ConnectResult(ConnectErrorCode.ConnectionParameterError, ex);
                        default:
                            RaiseConnectionFailed(this, new ConnectionFailedEventArgs(ConnectErrorCode.Error, ex));
                            return new ConnectResult(ConnectErrorCode.Error, ex);
                    }
                }
                else
                {
                    RaiseConnectionFailed(this, new ConnectionFailedEventArgs(ConnectErrorCode.Error, ex));
                    return new ConnectResult(ConnectErrorCode.Error, ex);
                }
            }
        }

        /// <summary>
        /// 初始化SSL通信流
        /// </summary>
        public void InitializeSslStream()
        {
            if (SslStream != null)
            {
                SslStream.Close();
                SslStream.Dispose();
                SslStream = null;
            }

            // 创建SSL流
            SslStream = new SslStream(new NetworkStream(InnerClient), false, RemoteCertificateValidationHandle ?? RemoteCertificateValidateCallback, LocalCertificateSelectionHandle ?? LocalCertificateSelectionCallback, EncryptionPolicy.AllowNoEncryption);
            SslStream.AuthenticateAsClient(ServerCertificateName, new X509CertificateCollection(new X509Certificate[] { Certificate }), SslProtocol, false);

            // 执行TLS通信验证通过的回调事件
            PerformTlsVerifySuccessfully(this, new TlsVerifySuccessfullyEventArgs(SslStream));
        }

        /// <summary>
        /// 重置内部客户端
        /// </summary>
        private void ResetInnerClient()
        {
            SslStream?.Dispose();
            SslStream = null;
            InnerClient?.Dispose();
            InnerClient = CreateDefaultSocket();
        }

        /// <inheritdoc/>
        public override DisconnectResult Disconnect(DisconnectArgument argument = null)
        {
            argument ??= new DisconnectArgument();

            lock (lockerStatus)
            {
                // 如果TcpClient没有关闭，则关闭连接
                if (InnerClient.Connected || innerStatus == InnerStatus.Busy)
                {
                    // 关闭异步任务
                    CancelReceiveDataTask();

                    // 关闭内部Socket客户端
                    InnerClient.Close();

                    // 重置TCP客户端
                    ResetInnerClient();

                    // 重置内部状态为空闲
                    innerStatus = InnerStatus.Free;

                    // 执行通信已断开的回调事件 
                    RaiseDisconnected(this, new DisconnectedEventArgs(argument.ReasonCode, argument.Exception));

                    return new DisconnectResult();
                }
                else
                {
                    // 当前无连接
                    return new DisconnectResult(DisconnectErrorCode.NoConnection);
                }
            }
        }

        /// <inheritdoc/>
        protected override void SendHandler(SendArgs sendArgs)
        {
            try
            {
                if (UseSsl)
                {
                    SslStream.Write(sendArgs.Data);
                    SslStream.Flush();
                }
                else
                {
                    // 发送数据
                    InnerClient.Send(sendArgs.Data);
                }
                // 执行数据已发送的回调事件
                RaiseDataSent(this, new DataSentEventArgs(sendArgs.Data));

                // 延时
                Task.Delay(sendArgs.Options.PauseTime).Wait();
            }
            catch (Exception ex)
            {
                // 通信异常
                RaiseExceptionOccurs(this, new ExceptionOccursEventArgs(ex));
            }
        }

        /// <inheritdoc/>
        protected override void ReceiveDataHandle()
        {
            Socket socket = InnerClient;
            Stream stream = null;
            try
            {
                int CheckTimes = 0;
                stream = UseSsl ? SslStream : new NetworkStream(socket);
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

                    InvokeDataReceivedEventCallback(data);
                }
            }
            catch (Exception ex)
            {
                // 如果关闭了通信，不回调异常
                if (!socket.Connected)
                {
                    if (ex is IOException && ex.InnerException != null)
                    {
                        if (ex.InnerException is SocketException ex2)
                        {
                            switch (ex2.SocketErrorCode)
                            {
                                case SocketError.ConnectionReset:   // TODO: 待解决问题
                                case SocketError.Interrupted:       // TODO: 待解决问题"WSACancelBlockingCall"
                                    return;
                            }
                        }
                    }
                }

                // 回调异常事件
                RaiseExceptionOccurs(this, new ExceptionOccursEventArgs(ex));
            }
            finally
            {
                stream?.Dispose();
            }
        }

        /// <inheritdoc/>
        protected override void ReceiveDataCompletedHandle()
        {
            Disconnect(new DisconnectArgument(DisconnectionReasonCode.Passive));

            SslStream = null;
            // 重置TCP客户端
            ResetInnerClient();
        }

        /// <inheritdoc/>
        public override void Dispose()
        {
#if NETSTANDARD || NETCOREAPP
            InnerClient?.Dispose();
#elif NETFRAMEWORK
            InnerClient?.Close();
#endif
            InnerClient = null;
        }

        /// <summary>
        /// 内部客户端的连接状态
        /// </summary>
        private enum InnerStatus
        {
            Free,
            Busy,
        }
    }

    public partial class TcpClient : ITcpClient
    {
        /// <inheritdoc/>
        public string Host { get; set; } = "127.0.0.1";

        /// <inheritdoc/>
        public int Port { get; set; } = 8086;

        /// <inheritdoc/>
        public int LocalPort => IsConnected ? ((IPEndPoint)InnerClient.LocalEndPoint).Port : 0;

        /// <inheritdoc/>
        public IPEndPoint RemoteEndPoint => (IPEndPoint)InnerClient.RemoteEndPoint;
    }
}
