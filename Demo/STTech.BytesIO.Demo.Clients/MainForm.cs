using STTech.BytesIO.Core;
using STTech.BytesIO.Ipc;
using STTech.BytesIO.Serial;
using STTech.BytesIO.Tcp;
using STTech.BytesIO.Quic;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace STTech.BytesIO.Demo.Clients
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
        }

        private void tsmiCreateTcpClient_Click(object sender, EventArgs e)
        {
            tab.AddPage("TCP客户端", new ClientPanel(new TcpClient() { Port = 60000 }));
        }

        private void tsmiCreateSerialClient_Click(object sender, EventArgs e)
        {
            tab.AddPage("串口客户端", new ClientPanel(new SerialClient() { ReceiveBufferSize = 65536, SendBufferSize = 65536 }));
        }

        private void tsmiCreateUdpClient_Click(object sender, EventArgs e)
        {
            tab.AddPage("IPC客户端", new ClientPanel(new IpcClient() { PipeName = "STTech.BytesIO.Demo" }));
        }

        private void tsmiCreateQuicClient_Click(object sender, EventArgs e)
        {
            tab.AddPage("QUIC客户端", new ClientPanel(new QuicClient() { Port = 443 }));
        }
    }
}
