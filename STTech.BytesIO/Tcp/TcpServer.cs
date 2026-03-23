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
using System.Collections.Concurrent;

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

        /// <summary>
        /// 服务器状态
        /// </summary>
        ServerState State { get; }

        /// <summary>
        /// 是否在运行
        /// </summary>
        bool IsRunning { get; }
    }

    /// <summary>
    /// TCP服务端
    /// </summary>
    public partial class TcpServer : TcpServer<TcpClient>
    {
        /// <summary>
        /// 构造 TCP 服务端
        /// </summary>
        public TcpServer()
        {
            EncapsulateSocket = socket => new TcpClient(socket);
        }
    }


    /// <summary>
    /// TCP 服务端基类
    /// </summary>
    /// <typeparam name="T">客户端类型</typeparam>
    public abstract partial class TcpServer<T> where T : TcpClient
    {
        /// <summary>
        /// 是否使用 SSL 通信
        /// </summary>
        public bool UseSsl { get; set; }

        /// <summary>
        /// 服务器证书名称
        /// </summary>
        public string ServerCertificateName { get; set; }

        /// <summary>
        /// 服务器证书对象
        /// </summary>
        public X509Certificate Certificate { get; set; }

        /// <summary>
        /// SSL 协议类型
        /// </summary>
        public SslProtocols SslProtocol { get; set; }
    }

    public abstract partial class TcpServer<T> : BytesServer<T>, ITcpServer where T : TcpClient
    {
        private Socket socket;

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
        /// <inheritdoc/>
        /// </summary>
        public override Task StartAsync()
        {
            if (IsListening)
            {
                return Task.FromResult(0);
            }

            lock (ServerStateLocker)
            {
                // 初始化监听Socket
                socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                IPAddress ipAddress = IPAddress.Parse(Host);
                IPEndPoint ipEndPoint = new IPEndPoint(ipAddress, Port);
                socket.Bind(ipEndPoint);
                socket.Listen(Backlog);

                // 修改服务器状态
                State = ServerState.Listening;

                // 触发事件
                OnStarted(EventArgs.Empty);
            }

            // 启动异步接收循环
            _ = AcceptLoopAsync();
            return Task.FromResult(0);
        }

        private async Task AcceptLoopAsync()
        {
            while (IsListening)
            {
                Socket serverSocket = socket;
                if (serverSocket == null) break;

                Socket clientSocket = null;
                try
                {
                    clientSocket = await Task.Factory.FromAsync(serverSocket.BeginAccept, serverSocket.EndAccept, null).ConfigureAwait(false);
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
                catch (NullReferenceException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    RaiseExceptionOccurs(ex);
                    continue;
                }

                if (clientSocket != null)
                {
                    // 若服务器暂停或达到最大连接数，不再粗暴停止监听，而是使用安全拒绝策略
                    if (State == ServerState.Paused || (MaxConnections > 0 && InternalClients.Count >= MaxConnections))
                    {
                        try
                        {
                            clientSocket.Disconnect(false);
                            clientSocket.Close();
                            clientSocket.Dispose();
                        }
                        catch { }
                        continue;
                    }

                    // 异步处理新客户端，极速返回 AcceptLoop 避免死锁
                    _ = ProcessNewClientAsync(clientSocket);
                }
            }
        }

        private async Task ProcessNewClientAsync(Socket clientSocket)
        {
            try
            {
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
                            // SSL 握手可能导致阻塞或重度耗时，放入后台 Task 中以保障网络核心吞吐量
                            await Task.Run(() => client.InitializeSslStream()).ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            throw new Exception($"Description Failed to establish SSL communication between the server and client. (Client RemoteEndPoint: {client.RemoteEndPoint})", ex);
                        }
                    }

                    // 触发连接事件
                    OnClientConnected(client);
                }
                else
                {
                    clientSocket.Disconnect(false);
                    clientSocket.Dispose();
                }
            }
            catch (Exception ex)
            {
                RaiseExceptionOccurs(ex);
            }
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public override Task CloseAsync()
        {
            if (!IsRunning)
            {
                return Task.FromResult(0);
            }

            var task = Task.Run(() =>
            {
                lock (ServerStateLocker)
                {
                    try { socket?.Close(); } catch (Exception) { }
                    try { socket?.Dispose(); } catch (Exception) { }
                    socket = null;
                    State = ServerState.Closed;

                    var activeClients = InternalClients.Keys.ToList();
                    InternalClients.Clear();
                    foreach (var client in activeClients)
                    {
                        try
                        {
                            client.Disconnect();
                        }
                        catch (Exception) { }
                    }
                    OnClosed(EventArgs.Empty);
                }
            });

            return task;
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public override Task StopAsync()
        {
            if (!IsListening)
            {
                return Task.FromResult(0);
            }

            var task = Task.Run(() =>
            {
                lock (ServerStateLocker)
                {
                    try
                    {
                        socket?.Close();
                        socket?.Dispose();
                    }
                    catch (Exception)
                    {
                    }
                    finally
                    {
                        State = ServerState.Paused;
                        OnPaused(EventArgs.Empty);
                    }
                }
            });

            return task;
        }
    }
}
