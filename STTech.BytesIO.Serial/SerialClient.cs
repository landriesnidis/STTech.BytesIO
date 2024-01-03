using STTech.BytesIO.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Net.Security;
using System.Net.Sockets;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace STTech.BytesIO.Serial
{
    public partial class SerialClient : BytesClient
    {
        /// <summary>
        /// 串口通信对象
        /// </summary>
        protected SerialPort InnerClient { get; set; }

        /// <summary>
        /// 获取内部的串口通信对象
        /// </summary>
        /// <returns></returns>
        public SerialPort GetInnerClient() => InnerClient;

        /// <summary>
        /// 获取一个指示打开或关闭状态的值
        /// </summary>
        [IgnoreDataMember]
        public override bool IsConnected => InnerClient.IsOpen;

        public SerialClient()
        {
            // 初始化
            InnerClient = new SerialPort();
        }

        /// <summary>
        /// 建立串口通信
        /// </summary>
        public override ConnectResult Connect(ConnectArgument argument = null)
        {
            // 如果串口已经打开了，则此次连接无效
            if (InnerClient.IsOpen)
            {
                RaiseConnectionFailed(this, new ConnectionFailedEventArgs(ConnectErrorCode.IsConnected));
                return new ConnectResult(ConnectErrorCode.IsConnected);
            }

            try
            {
                InnerClient.Open();

                // 执行连接成功回调事件
                RaiseConnectedSuccessfully(this, new ConnectedSuccessfullyEventArgs());

                // 启动接收数据的异步任务
                StartReceiveDataTask();

                return new ConnectResult();
            }
            catch (Exception ex)
            {
                // 连接失败
                RaiseConnectionFailed(this, new ConnectionFailedEventArgs(ex));
                return new ConnectResult(ConnectErrorCode.Error, ex);
            }
        }

        /// <summary>
        /// 关闭串口通信
        /// </summary>
        public override DisconnectResult Disconnect(DisconnectArgument argument = null)
        {
            argument ??= new DisconnectArgument();

            if (argument.ReasonCode == DisconnectionReasonCode.Active && !InnerClient.IsOpen)
            {
                return new DisconnectResult(DisconnectErrorCode.NoConnection);
            }

            // 关闭异步任务
            // 注：SerialPort已提供了DataReceived事件，不需要再实现异步接收
            // CancelReceiveDataTask();

            try
            {
                CancelReceiveDataTask();

                // 关闭串口
                InnerClient.Close();

                // 执行通信已断开的回调事件 
                RaiseDisconnected(this, new DisconnectedEventArgs(argument.ReasonCode, argument.Exception));

                return new DisconnectResult();
            }
            catch (Exception ex)
            {
                return new DisconnectResult(DisconnectErrorCode.Error, ex);
            }
        }

        /// <inheritdoc/>
        protected override void SendHandler(SendArgs args)
        {
            try
            {
                var data = args.Data;

                // 发送数据
                InnerClient.Write(data, 0, data.Length);

                // 执行数据已发送的回调事件
                RaiseDataSent(this, new DataSentEventArgs(data));

                // 延时
                Task.Delay(args.Options.PauseTime).Wait();
            }
            catch (Exception ex)
            {
                // 通信异常
                RaiseExceptionOccurs(this, new ExceptionOccursEventArgs(ex));
            }
        }

        /// <summary>
        /// 获取当前计算机的串行端口名称的数组
        /// </summary>
        public string[] GetPortNames()
        {
            return SerialPort.GetPortNames();
        }

        /// <inheritdoc/>
        protected override void ReceiveDataCompletedHandle() { }

        /// <inheritdoc/>
        protected override void ReceiveDataHandle()
        {
            SerialPort sp = InnerClient;

            byte[] buffer = new byte[0];
            List<byte> recv = new();
            int len;
            DateTime? startFrameTimestamp = null;
            try
            {
                while (IsConnected)
                {
                    if (buffer.Length != ReceiveBufferSize)
                    {
                        buffer = new byte[ReceiveBufferSize];
                    }

                    // 获取数据长度
                    len = sp.Read(buffer, 0, ReceiveBufferSize);

                    // 接收到首帧的时间戳
                    startFrameTimestamp ??= ReceiveTimeout > 0 ? DateTime.Now : null;

                    // 将读取到的数据保存
                    recv.AddRange(buffer.Take(len).ToArray());

                    // 延迟等待
                    if (ReceiveTimeout > 0)
                    {
                        var diffTime = (DateTime.Now - startFrameTimestamp.Value).TotalMilliseconds;
                        if (diffTime < ReceiveTimeout)
                        {
                            Task.Delay((int)Math.Ceiling(ReceiveTimeout / 10.0)).Wait();
                            if (sp.BytesToRead > 0)
                            {
                                continue;
                            }
                        }
                    }

                    InvokeDataReceivedEventCallback(recv.ToArray());
                    startFrameTimestamp = null;
                    recv.Clear();
                }
            }
            catch (Exception ex)
            {
                // 如果是主动关闭的连接，则不触发异常回调
                if (ReceiveTaskCancellationTokenSource.IsCancellationRequested)
                {
                    return;
                }

                // 如果通信已经被关闭了，则说明是被动关闭导致的；否则代表数据接收时出现错误；
                if (!IsConnected)
                {
                    Disconnect(new DisconnectArgument(DisconnectionReasonCode.Passive, ex));
                }
                else
                {
                    // 回调异常事件
                    RaiseExceptionOccurs(this, new ExceptionOccursEventArgs(ex));
                    Disconnect(new DisconnectArgument(DisconnectionReasonCode.Error, ex));
                }


            }
        }
    }

    public partial class SerialClient : ISerialClient
    {
        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public Handshake Handshake { get => InnerClient.Handshake; set => InnerClient.Handshake = value; }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public bool DtrEnable { get => InnerClient.DiscardNull; set => InnerClient.DiscardNull = value; }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public bool DiscardNull { get => InnerClient.DiscardNull; set => InnerClient.DiscardNull = value; }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public int DataBits { get => InnerClient.DataBits; set => InnerClient.DataBits = value; }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public string NewLine { get => InnerClient.NewLine; set => InnerClient.NewLine = value; }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public override int ReceiveBufferSize { get => InnerClient.ReadBufferSize; set => InnerClient.ReadBufferSize = value; }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public byte ParityReplace { get => InnerClient.ParityReplace; set => InnerClient.ParityReplace = value; }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public string PortName { get => InnerClient.PortName; set => InnerClient.PortName = value; }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public int ReadTimeout { get => InnerClient.ReadTimeout; set => InnerClient.ReadTimeout = value; }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public int ReceivedBytesThreshold { get; set; }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public bool RtsEnable { get => InnerClient.RtsEnable; set => InnerClient.RtsEnable = value; }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public StopBits StopBits { get => InnerClient.StopBits; set => InnerClient.StopBits = value; }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public override int SendBufferSize { get => InnerClient.WriteBufferSize; set => InnerClient.WriteBufferSize = value; }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public int WriteTimeout { get => InnerClient.WriteTimeout; set => InnerClient.WriteTimeout = value; }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public Parity Parity { get => InnerClient.Parity; set => InnerClient.Parity = value; }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public int BaudRate { get => InnerClient.BaudRate; set => InnerClient.BaudRate = value; }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public int ReceiveTimeout { get; set; } = 50;

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public void DiscardInBuffer()
        {
            InnerClient.DiscardInBuffer();
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public void DiscardOutBuffer()
        {
            InnerClient.DiscardOutBuffer();
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public override void Dispose()
        {
#if NETSTANDARD || NETCOREAPP
            InnerClient?.Dispose();
#elif NETFRAMEWORK
            InnerClient?.Close();
            InnerClient = null;
#endif
        }
    }
}
