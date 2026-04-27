using STTech.BytesIO.Tcp;
using STTech.BytesIO.Ipc;
using STTech.BytesIO.Quic;
using STTech.BytesIO.P2P;
using System;
using System.Windows.Forms;

namespace STTech.BytesIO.Demo.Servers
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
        }

        private void tsmiCreateTcpServer_Click(object sender, EventArgs e)
        {
            tab.AddPage("TCP服务端", new ServerPanel(new TcpServer() { Port = 60000 }));
        }

        private void tsmiCreateIpcServer_Click(object sender, EventArgs e)
        {
            tab.AddPage("IPC服务端", new ServerPanel(new IpcServer() { PipeName = "STTech.BytesIO.Demo" }));
        }

        private void tsmiCreateQuicServer_Click(object sender, EventArgs e)
        {
            tab.AddPage("QUIC服务端", new ServerPanel(new QuicServer() { Port = 443 }));
        }

        private void tsmiCreateP2PServer_Click(object sender, EventArgs e)
        {
            tab.AddPage("P2P种子节点", new ServerPanel(new BootstrapServer()));
        }

    }
}
