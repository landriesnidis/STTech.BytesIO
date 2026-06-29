using STTech.BytesIO.Core;
using STTech.BytesIO.Serial;
using STTech.BytesIO.Tcp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace HighFrequencySendTest
{
    public partial class Form1 : Form
    {
        private BytesClient client;
        private byte[] data = new byte[] { 0x55, 0x0A, 0x00, 0xAA };

        public Form1()
        {
            InitializeComponent();

            pgTimer.SelectedObject = timerSend;
        }

        private void SetClient(BytesClient client)
        {
            if (this.client != null)
            {
                if (this.client.IsConnected)
                {
                    this.client.Disconnect();
                }
                this.client.OnConnectedSuccessfully -= Client_OnConnectedSuccessfully;
                this.client.OnDisconnected -= Client_OnDisconnected;
                this.client.OnConnectionFailed -= Client_OnConnectionFailed;
                this.client.Dispose();
            }

            this.client = client;
            this.client.OnConnectedSuccessfully += Client_OnConnectedSuccessfully;
            this.client.OnDisconnected += Client_OnDisconnected;
            this.client.OnConnectionFailed += Client_OnConnectionFailed;

            pgClient.SelectedObject = null;
            pgClient.SelectedObject = client;
        }

        private void Client_OnConnectionFailed(object sender, ConnectionFailedEventArgs e) => UpdateClientStatus("Failed");
        private void Client_OnDisconnected(object sender, DisconnectedEventArgs e) => UpdateClientStatus($"Disconnected({e.ReasonCode})");
        private void Client_OnConnectedSuccessfully(object sender, ConnectedSuccessfullyEventArgs e) => UpdateClientStatus("Connected");

        private void UpdateClientStatus(string str)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<string>(UpdateClientStatus), str);
                return;
            }
            labClientStatus.Text = str;
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            timerSend.Enabled = true;
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            timerSend.Enabled = false;
        }

        private void btnTestTcp_Click(object sender, EventArgs e)
        {
            SetClient(new TcpClient());
        }

        private void btnTestSerialPort_Click(object sender, EventArgs e)
        {
            SetClient(new SerialClient());
        }

        private void timerSend_Tick(object sender, EventArgs e)
        {
            if (client == null || !client.IsConnected)
            {
                timerSend.Enabled = false;
                return;
            }

            client.SendAsync(data);
        }

        private void btnConnect_Click(object sender, EventArgs e)
        {
            client?.Connect();
        }

        private void btnDisconnect_Click(object sender, EventArgs e)
        {
            client?.Disconnect();
        }
    }
}
