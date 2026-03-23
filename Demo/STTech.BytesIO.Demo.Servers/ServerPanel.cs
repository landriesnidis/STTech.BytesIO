using STTech.BytesIO.Core;
using System;
using System.Windows.Forms;

namespace STTech.BytesIO.Demo.Servers
{
    public partial class ServerPanel : UserControl
    {
        private IBytesServer server;

        public ServerPanel()
        {
            InitializeComponent();
            CheckForIllegalCrossThreadCalls = false;
        }

        public ServerPanel(IBytesServer server) : this()
        {
            this.server = server;
            propertyGrid.SelectedObject = server;

            // 初始化策略下拉框
            cbStrategy.Items.Add("无操作");
            cbStrategy.Items.Add("原消息返回");
            cbStrategy.Items.Add("消息转发");
            cbStrategy.SelectedIndex = 2;

            // 绑定通用事件
            if (server is BytesServer<STTech.BytesIO.Tcp.TcpClient> tcpServer)
            {
                tcpServer.ClientConnected += (s, e) =>
                {
                    Log($"[TCP] 客户端已连接: {e.Client.RemoteEndPoint}");
                    SubscribeToClientEvents(e.Client);
                };
                tcpServer.ClientDisconnected += (s, e) => Log($"[TCP] 客户端已断开: {e.Client.RemoteEndPoint} ({e.ReasonCode}) {e.Exception?.Message}");
            }
            else if (server is BytesServer<STTech.BytesIO.Ipc.IpcClient> ipcServer)
            {
                ipcServer.ClientConnected += (s, e) =>
                {
                    Log($"[IPC] 客户端已连接: {e.Client.PipeName}");
                    SubscribeToClientEvents(e.Client);
                };
                ipcServer.ClientDisconnected += (s, e) => Log($"[IPC] 客户端已断开: {e.Client.PipeName} ({e.ReasonCode}) {e.Exception?.Message}");
            }

            server.Started += (s, e) => Log("服务已启动");
            server.Closed += (s, e) => Log("服务已关闭");
            server.Paused += (s, e) => Log("服务已暂停监听");
            server.OnExceptionOccurs += (s, e) => Log($"[异常] {e.Exception.Message}");
        }

        private void SubscribeToClientEvents(BytesClient client)
        {
            client.OnDataReceived += (s, e) =>
            {
                // 获取当前策略
                int strategyIndex = cbStrategy.SelectedIndex;
                if (strategyIndex == 1) // Echo
                {
                    client.Send(e.Data.ToArray());
                }
                else if (strategyIndex == 2) // Broadcast
                {
                    foreach (var otherClient in server.GetClients())
                    {
                        if (otherClient != client)
                        {
                            otherClient.Send(e.Data.ToArray());
                        }
                    }
                }
            };
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            server.StartAsync();
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            server.StopAsync();
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            server.CloseAsync();
        }

        private void Log(string msg)
        {
            tbLog.AppendText($"[{DateTime.Now:HH:mm:ss.fff}] {msg}\r\n");
            tbLog.ScrollToCaret();
        }
    }

    /// <summary>
    /// 消息处理策略
    /// </summary>
    public enum MessageHandlingStrategy
    {
        /// <summary>
        /// 无操作
        /// </summary>
        None,

        /// <summary>
        /// 原消息返回
        /// </summary>
        Echo,

        /// <summary>
        /// 转发给其他所有客户端
        /// </summary>
        Broadcast,
    }
}
