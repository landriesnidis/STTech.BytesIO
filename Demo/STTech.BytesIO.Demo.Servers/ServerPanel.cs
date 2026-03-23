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

            // 绑定通用事件
            if (server is BytesServer<STTech.BytesIO.Tcp.TcpClient> tcpServer)
            {
                tcpServer.ClientConnected += (s, e) => Log($"[TCP] 客户端已连接: {e.Client.RemoteEndPoint}");
                tcpServer.ClientDisconnected += (s, e) => Log($"[TCP] 客户端已断开: {e.Client.RemoteEndPoint} ({e.ReasonCode})");
            }
            else if (server is BytesServer<STTech.BytesIO.Ipc.IpcClient> ipcServer)
            {
                ipcServer.ClientConnected += (s, e) => Log($"[IPC] 客户端已连接: {e.Client.PipeName}");
                ipcServer.ClientDisconnected += (s, e) => Log($"[IPC] 客户端已断开: {e.Client.PipeName} ({e.ReasonCode})");
            }

            server.Started += (s, e) => Log("服务已启动");
            server.Closed += (s, e) => Log("服务已关闭");
            server.Paused += (s, e) => Log("服务已暂停监听");
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
}
