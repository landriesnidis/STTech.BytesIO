using NUnit.Framework;
using STTech.BytesIO.Tcp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace STTech.BytesIO.Test
{
    public class Tests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void TcpConnectSendData()
        {
            AutoResetEvent evt = new AutoResetEvent(false);
            TcpClient client = new TcpClient() { Host = "127.0.0.1", Port = 60000 };
            client.OnConnectedSuccessfully += (s, e) => Debug.WriteLine("连接成功");
            client.OnConnectionFailed += (s, e) => Debug.WriteLine("连接失败");
            client.OnDisconnected += (s, e) => { Debug.WriteLine("连接断开"); evt.Reset(); };
            client.OnDataSent += (s, e) => Debug.WriteLine($"发送数据：{e.Data.ToHexString()} ({e.Data.EncodeToString()})");
            client.OnDataReceived += (s, e) => Debug.WriteLine($"收到数据：{e.Data.ToHexString()} ({e.Data.EncodeToString()})");
            client.ConnectAsync();
            client.ConnectAsync();
            client.ConnectAsync();

            Thread.Sleep(1000);

            if (client.IsConnected)
            {
                client.SendAsync("Hello World.".GetBytes());
                evt.WaitOne();
            }

            Assert.Pass();
        }
    }
}