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
    public partial class ChatClientPanel : UserControl
    {
        private ChatClient client;

        private ChatClientPanel()
        {
            InitializeComponent();
            CheckForIllegalCrossThreadCalls = false;
        }

        public ChatClientPanel(ChatClient client) : this()
        {
            this.client = client;
            propertyGrid.SelectedObject = client.InnerClient;

            client.OnConnectedSuccessfully += Client_OnConnectedSuccessfully;
            client.OnDisconnected += Client_OnDisconnected;
            client.OnConnectionFailed += Client_OnConnectionFailed;
            client.OnExceptionOccurs += Client_OnExceptionOccurs;

            client.FileAccept += Client_FileAccept;
            client.FileReceived += Client_FileReceived;
            client.FileSending += Client_FileSending;
            client.FileSent += Client_FileSent;
            client.ShakeReceived += Client_ShakeReceived;
            client.TextReceived += Client_TextReceived;
        }

        private void Client_OnConnectionFailed(object sender, ConnectionFailedEventArgs e)
        {
            Print($"连接失败：{e.Message}");
        }

        private void Client_TextReceived(object sender, ChatSdk.Entitiy.TextReceivedEventArgs e)
        {
            Print($"收到消息：{e.Text}");
        }

        private void Client_ShakeReceived(object sender, ChatSdk.Entitiy.ShakeReceivedEventArgs e)
        {
            ParentForm.Shake();
            Print("收到一个窗口抖动");
        }

        private void Client_FileSent(object sender, ChatSdk.Entitiy.FileSentEventArgs e)
        {
            Print($"文件已发送：{e.FileName}");
        }

        private void Client_FileSending(object sender, ChatSdk.Entitiy.FileSendingEventArgs e)
        {
            Print($"正在发送文件：{e.FileName}[{100.0 * e.SentLen / e.FileSize}%]");
        }

        private void Client_FileReceived(object sender, ChatSdk.Entitiy.FileReceivedEventArgs e)
        {
            var fi = new FileInfo(e.FilePath);
            Print($"文件接收成功：{fi.FullName}");
            Process.Start("Explorer.exe", $"/select,{fi.FullName}");
        }

        private void Client_FileAccept(object sender, ChatSdk.Entitiy.FileAcceptEventArgs e)
        {
            Print($"开始接收文件：{e.FilePath}");
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
            client.Connect(500);
        }

        private void btnDisconnect_Click(object sender, EventArgs e)
        {
            client.Disconnect();
        }

        private void btnSend_Click(object sender, EventArgs e)
        {
            client.SendText(tbSend.Text);
        }

        private void Print(string msg)
        {
            tbRecv.AppendText($"[{DateTime.Now}] {msg}\r\n");
        }

        private void btnSendFile_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "All files (*.*)|*.*";
                openFileDialog.RestoreDirectory = true;

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    var filePath = openFileDialog.FileName;
                    Task.Run(() => client.SendFile(filePath));
                }
            }
        }

        private void btnSendShake_Click(object sender, EventArgs e)
        {
            client.SendShake();
            Print("发送一个窗口抖动");
        }
    }
}
