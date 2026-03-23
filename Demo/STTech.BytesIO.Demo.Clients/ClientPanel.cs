using Demo.BytesIO.ChatProtocol;
using Demo.BytesIO.ChatSdk;
using Newtonsoft.Json;
using STTech.BytesIO.Core;
using STTech.BytesIO.Serial;
using STTech.BytesIO.Tcp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Demo.BytesIO.Client
{
    public partial class ClientPanel : UserControl
    {
        private BytesClient client;

        private ClientPanel()
        {
            InitializeComponent();
            CheckForIllegalCrossThreadCalls = false;
        }

        public ClientPanel(BytesClient client) : this()
        {
            this.client = client;
            propertyGrid.SelectedObject = client;

            client.OnConnectedSuccessfully += Client_OnConnectedSuccessfully;
            client.OnDisconnected += Client_OnDisconnected;
            client.OnConnectionFailed += Client_OnConnectionFailed;
            client.OnExceptionOccurs += Client_OnExceptionOccurs;
            client.OnDataReceived += Client_OnDataReceived;
            client.OnDataSent += Client_OnDataSent;
        }

        private void Client_OnDataSent(object sender, DataSentEventArgs e)
        {
            Print($"发送：{e.Data.ToHexString()}");
        }

        private void Client_OnDataReceived(object sender, STTech.BytesIO.Core.DataReceivedEventArgs e)
        {
            Print($"接收：{e.Data.ToHexString()}");
        }

        private void Client_OnConnectionFailed(object sender, ConnectionFailedEventArgs e)
        {
            Print($"连接失败：{e.Message}");
        }

        private void Client_OnExceptionOccurs(object sender, STTech.BytesIO.Core.ExceptionOccursEventArgs e)
        {
            Print($"发生了一个异常：{e.Exception.Message}");
        }

        private void Client_OnDisconnected(object sender, STTech.BytesIO.Core.DisconnectedEventArgs e)
        {
            Print($"已断开({e.ReasonCode})");
        }

        private void Client_OnConnectedSuccessfully(object sender, STTech.BytesIO.Core.ConnectedSuccessfullyEventArgs e)
        {
            Print("连接成功");
        }

        private void btnConnect_Click(object sender, EventArgs e)
        {
            client.Connect(10);
        }

        private void btnDisconnect_Click(object sender, EventArgs e)
        {
            client.Disconnect();
        }

        private void btnSend_Click(object sender, EventArgs e)
        {
            client.Send(tbSend.Text.GetBytes());
        }

        private void Print(string msg)
        {
            tbRecv.AppendText($"[{DateTime.Now}] {msg}\r\n");
        }
    }
}
