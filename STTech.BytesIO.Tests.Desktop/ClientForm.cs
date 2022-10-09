using STTech.BytesIO.Core;
using STTech.BytesIO.Udp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace STTech.BytesIO.Tests.Desktop
{
    public partial class ClientForm : Form
    {
        private UdpClient client;

        public ClientForm()
        {
            InitializeComponent();
            CheckForIllegalCrossThreadCalls = false;

            propertyGrid.SelectedObject = client = new UdpClient() { Port = 60000};

            client.OnDataReceived += Client_OnDataReceived;
            client.OnUdpDataReceived += Client_OnUdpDataReceived;
            client.OnConnectedSuccessfully += Client_OnConnectedSuccessfully;
            client.OnDisconnected += Client_OnDisconnected;
            client.OnDataSent += Client_OnDataSent;
            client.OnExceptionOccurs += Client_OnExceptionOccurs;
        }

        private void Client_OnUdpDataReceived(object sender, UdpDataReceivedEventArgs e)
        {
            Print($"收到来自[{e.ReceiveResult.RemoteEndPoint}]的数据：{e.Data.EncodeToString()}");
        }

        private void Print(string msg)
        {
            tbLog.AppendText($"[{DateTime.Now}] {msg}\r\n");
        }

        private void Client_OnDataReceived(object sender, Core.Entity.DataReceivedEventArgs e)
        {
            Print($"收到数据：{e.Data.EncodeToString()}");
        }

        private void Client_OnExceptionOccurs(object sender, STTech.BytesIO.Core.Entity.ExceptionOccursEventArgs e)
        {
            Print($"发生了一个异常：{e.Exception.Message}");
        }

        private void Client_OnDataSent(object sender, STTech.BytesIO.Core.Entity.DataSentEventArgs e)
        {
            Print($"发送数据：{e.Data.EncodeToString("GBK")}");
        }

        private void Client_OnDisconnected(object sender, STTech.BytesIO.Core.Entity.DisconnectedEventArgs e)
        {
            Print($"已断开({e.ReasonCode})");
        }

        private void Client_OnConnectedSuccessfully(object sender, STTech.BytesIO.Core.Entity.ConnectedSuccessfullyEventArgs e)
        {
            Print("连接成功");
        }

        private void btnConnect_Click(object sender, EventArgs e)
        {
            client.Connect();
        }

        private void btnDisconnect_Click(object sender, EventArgs e)
        {
            client.Disconnect();
        }

        private void btnSend_Click(object sender, EventArgs e)
        {
            client.Send(tbSend.Text.GetBytes());
        }
    }
}
