using STTech.BytesIO.Core;
using STTech.BytesIO.Tcp.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
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
    public class TcpServer : TcpServer<TcpClient>
    {
        public TcpServer()
        {
            EncapsulateSocket = socket => new TcpClient(new System.Net.Sockets.TcpClient() { Client = socket });
        }
    }

    public abstract class TcpServer<T> : ITcpServer where T : TcpClient
    {
        private Socket socket;
        private List<T> clients = new List<T>();
        private bool isRun;

        /// <summary>
        /// 是否在运行
        /// </summary>
        public bool IsRunning { get; private set; }

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
        public IEnumerable<TcpClient> Clients => clients.ToArray();

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
        public event EventHandler<ClientConnectedEventArgs> ClientConnected;

        /// <summary>
        /// 客户端断开连接事件
        /// </summary>
        public event EventHandler<ClientDisconnectedEventArgs> ClientDisconnected;

        /// <summary>
        /// 服务器启动事件
        /// </summary>
        public event EventHandler Started;

        /// <summary>
        /// 服务器关闭事件
        /// </summary>
        public event EventHandler Closed;

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public Task StartAsync()
        {
            if (isRun)
            {
                return Task.FromResult(0);
            }

            isRun = true;
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPAddress ipAddress = IPAddress.Parse(Host);
            IPEndPoint ipEndPoint = new IPEndPoint(ipAddress, Port);
            socket.Bind(ipEndPoint);
            socket.Listen(Backlog);

            Started?.Invoke(this, EventArgs.Empty);

            return Task.Run(() =>
            {
                ManualResetEvent manualResetEvent = new ManualResetEvent(false);

                IsRunning = true;

                while (isRun)
                {
                    manualResetEvent.Reset();
                    socket.BeginAccept((result) =>
                    {
                        lock (manualResetEvent)
                        {

                            try
                            {
                                manualResetEvent?.Set();

                                Socket serverSocket = (Socket)result.AsyncState;
                                Socket clientSocket = serverSocket.EndAccept(result);

                                if (ClientConnectionAcceptedHandle(this, new ClientAcceptedEventArgs(clientSocket)))
                                {
                                    T client = EncapsulateSocket(clientSocket);
                                    clients.Add(client);
                                    client.OnDisconnected += TcpClient_OnDisconnected;
                                    // 触发事件
                                    ClientConnected?.Invoke(this, new ClientConnectedEventArgs(clientSocket, client));
                                }
                                else
                                {
                                    clientSocket.Disconnect(false);
                                    clientSocket.Dispose();
                                }
                            }
                            catch (Exception) { }
                        }
                    }, socket);
                    manualResetEvent.WaitOne();
                }

                CloseAsync();
                Closed?.Invoke(this, EventArgs.Empty);
                IsRunning = false;
            });
        }

        private void TcpClient_OnDisconnected(object sender, Core.DisconnectedEventArgs e)
        {
            lock (clients)
            {
                T client = (T)sender;
                client.OnDisconnected -= TcpClient_OnDisconnected;
                clients.Remove(client);
                ClientDisconnected?.Invoke(this, new ClientDisconnectedEventArgs(client));
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
            isRun = false;
            return Task.Run(() =>
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
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public Task StopAsync()
        {
            isRun = false;
            return Task.Run(() =>
            {
                socket?.Close();
                socket?.Dispose();
            });
        }
    }
}
