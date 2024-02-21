using STTech.BytesIO.Core;
using STTech.BytesIO.Tcp.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace STTech.BytesIO.Tcp
{
    public interface ITcpServer : IDisposable
    {
        /// <summary>
        /// 开放网络地址
        /// </summary>
        string Host { get; set; }

        /// <summary>
        /// 开放端口号
        /// </summary>
        int Port { get; set; }

        /// <summary>
        /// 启动监听
        /// </summary>
        /// <returns></returns>
        Task StartAsync();

        /// <summary>
        /// 停止监听
        /// </summary>
        /// <returns></returns>
        Task StopAsync();

        /// <summary>
        /// 关闭服务
        /// </summary>
        /// <returns></returns>
        Task CloseAsync();
    }

    /// <summary>
    /// TCP服务端
    /// </summary>
    public partial class TcpServer : TcpServer<TcpClient>
    {
        public TcpServer()
        {
            EncapsulateSocket = socket => new TcpClient(socket);
        }
    }


    public abstract partial class TcpServer<T> where T : TcpClient
    {
        public bool UseSsl { get; set; }
        public string ServerCertificateName { get; set; }
        public X509Certificate Certificate { get; set; }
        public SslProtocols SslProtocol { get; set; }
    }

    public abstract partial class TcpServer<T> : ITcpServer where T : TcpClient
    {
        private Socket socket;
        private List<T> clients = new List<T>();

        /// <summary>
        /// 服务器状态
        /// </summary>
        public ServerState State { get; private set; }

        /// <summary>
        /// 是否在运行
        /// </summary>
        public bool IsRunning => State != ServerState.Closed;

        /// <summary>
        /// 是否停止监听新客户端的加入
        /// </summary>
        public bool IsPaused => State == ServerState.Paused;

        /// <summary>
        /// 是否正在监听新客户端的连接
        /// </summary>
        public bool IsListening => State == ServerState.Listening;

        /// <summary>
        /// 接受客户端连接时
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public delegate bool ClientConnectionAcceptedCallback(object sender, ClientAcceptedEventArgs args);

        /// <summary>
        /// 封装Socket处理过程
        /// </summary>
        /// <param name="clientSocket"></param>
        /// <returns></returns>
        public delegate T EncapsulateSocketHandler(Socket clientSocket);

        /// <summary>
        /// 挂起连接队列的最大长度
        /// </summary>
        public int Backlog { get; set; } = 10;

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public string Host { get; set; } = "0.0.0.0";

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public int Port { get; set; } = 8086;

        /// <summary>
        /// 客户端列表
        /// </summary>
        public TcpClient[] Clients => clients.ToArray();

        /// <summary>
        /// 接收客户端连接时的处理过程
        /// 默认允许连接
        /// </summary>
        public ClientConnectionAcceptedCallback ClientConnectionAcceptedHandle { get; set; } = (s, e) => true;

        /// <summary>
        /// 封装Socket对象的方法
        /// 将客户端Socket封装称为基于TcpClient实现的类型
        /// </summary>
        protected EncapsulateSocketHandler EncapsulateSocket { get; set; }

        /// <summary>
        /// 客户端建立连接事件
        /// </summary>
        public virtual event EventHandler<ClientConnectedEventArgs> ClientConnected;

        /// <summary>
        /// 客户端断开连接事件
        /// </summary>
        public virtual event EventHandler<ClientDisconnectedEventArgs> ClientDisconnected;

        /// <summary>
        /// 在产生异常时发生
        /// </summary>
        public virtual event EventHandler<ExceptionOccursEventArgs> OnExceptionOccurs;

        /// <summary>
        /// 服务器启动事件
        /// </summary>
        public virtual event EventHandler Started;

        /// <summary>
        /// 服务器关闭事件
        /// </summary>
        public virtual event EventHandler Closed;

        /// <summary>
        /// 服务器暂停监听事件
        /// </summary>
        public virtual event EventHandler Paused;

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public Task StartAsync()
        {
            if (IsListening)
            {
                return Task.FromResult(0);
            }

            // 初始化监听Socket
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPAddress ipAddress = IPAddress.Parse(Host);
            IPEndPoint ipEndPoint = new IPEndPoint(ipAddress, Port);
            socket.Bind(ipEndPoint);
            socket.Listen(Backlog);

            // 修改服务器状态
            State = ServerState.Listening;

            // 触发事件
            Started?.BeginInvoke(this, EventArgs.Empty, null, null);

            return Task.Run(() =>
            {
                ManualResetEvent manualResetEvent = new ManualResetEvent(false);

                while (IsListening)
                {
                    manualResetEvent.Reset();
                    try
                    {


                        socket.BeginAccept((result) =>
                        {
                            lock (manualResetEvent)
                            {
                                try
                                {
                                    manualResetEvent?.Set();

                                    Socket serverSocket = (Socket)result.AsyncState;
                                    Socket clientSocket;

                                    try
                                    {
                                        clientSocket = serverSocket.EndAccept(result);
                                    }
                                    catch (Exception ex)
                                    {
                                        return;
                                    }

                                    if (ClientConnectionAcceptedHandle(this, new ClientAcceptedEventArgs(clientSocket)))
                                    {
                                        T client = EncapsulateSocket(clientSocket);

                                        if (client.IsConnected && UseSsl)
                                        {
                                            try
                                            {
                                                client.UseSsl = UseSsl;
                                                client.SslProtocol = SslProtocol;
                                                client.Certificate ??= Certificate;
                                                client.ServerCertificateName ??= ServerCertificateName;
                                                client.InitializeSslStream();
                                            }
                                            catch (Exception ex)
                                            {
                                                throw new Exception($"Description Failed to establish SSL communication between the server and client. (Client RemoteEndPoint: {client.RemoteEndPoint})", ex);
                                            }
                                        }

                                        clients.Add(client);
                                        client.OnDisconnected += TcpClient_OnDisconnected;
                                        // 触发事件
                                        ClientConnected?.BeginInvoke(this, new ClientConnectedEventArgs(clientSocket, client), null, null);
                                    }
                                    else
                                    {
                                        clientSocket.Disconnect(false);
                                        clientSocket.Dispose();
                                    }
                                }
                                catch (Exception ex)
                                {
                                    OnExceptionOccurs?.BeginInvoke(this, new ExceptionOccursEventArgs(ex), null, null);
                                }
                            }
                        }, socket);
                    }
                    catch (ObjectDisposedException ex) { }
                    manualResetEvent.WaitOne();
                }
            });
        }

        private void TcpClient_OnDisconnected(object sender, Core.DisconnectedEventArgs e)
        {
            lock (clients)
            {
                T client = (T)sender;
                client.OnDisconnected -= TcpClient_OnDisconnected;
                clients.Remove(client);
                ClientDisconnected?.BeginInvoke(this, new ClientDisconnectedEventArgs(client), null, null);
            }
        }

        /// <summary>
        /// 销毁
        /// </summary>
        public void Dispose()
        {
            CloseAsync();
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public Task CloseAsync()
        {
            if (!IsRunning)
            {
                return Task.FromResult(0);
            }

            var task = Task.Run(() =>
            {
                while (clients.Any())
                {
                    try
                    {
                        clients[0].Disconnect();
                    }
                    catch (Exception) { }
                }
                clients.Clear();
                socket?.Close();
                socket?.Dispose();
            });

            task.ContinueWith(t =>
            {
                State = ServerState.Closed;
                Closed?.BeginInvoke(this, EventArgs.Empty, null, null);
            });

            return task;
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public Task StopAsync()
        {
            if (!IsListening)
            {
                return Task.FromResult(0);
            }

            var task = Task.Run(() =>
            {
                try
                {
                    socket?.Close();
                    socket?.Dispose();
                }
                catch (Exception ex)
                {
                }
                finally
                {
                    State = ServerState.Paused;
                    Paused?.BeginInvoke(this, EventArgs.Empty, null, null);
                }
            });

            return task;
        }
    }

    /// <summary>
    /// 服务器状态
    /// </summary>
    public enum ServerState
    {
        /// <summary>
        /// 服务器处于关闭状态
        /// </summary>
        Closed,
        /// <summary>
        /// 正在监听新客户端的加入
        /// </summary>
        Listening,
        /// <summary>
        /// 听见监听新客户端的连接，保留现有客户端的通信
        /// </summary>
        Paused,
    }



    //public class SslException : BytesIOException
    //{
    //    public SslException(string message, Exception ex) : base(message, ex) { }
    //}
}
