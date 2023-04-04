using STTech.BytesIO.Core;
using System;
using System.Collections;
using System.ComponentModel;
using System.IO;
using System.IO.Ports;
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

            // 添加数据接收回调事件
            InnerClient.DataReceived += InnerClient_DataReceived;
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
                // 注：SerialPort已提供了DataReceived事件，不需要再实现异步接收
                // StartReceiveDataTask();

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

            if (!InnerClient.IsOpen)
            {
                return new DisconnectResult(DisconnectErrorCode.NoConnection);
            }

            // 关闭异步任务
            // 注：SerialPort已提供了DataReceived事件，不需要再实现异步接收
            // CancelReceiveDataTask();

            try
            {
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

        private readonly object _lockDataReceived = new object();

        /// <summary>
        /// 数据接收回调事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void InnerClient_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            lock (_lockDataReceived)
            {
                // 判断缓冲区是否有数据
                int len = InnerClient.BytesToRead;
                if (len > 0)
                {
                    if (ReceiveTimeout > 0)
                    {
                        Task.Delay(ReceiveTimeout).Wait();
                    }

                    // 获取数据
                    var bytes = new byte[len];
                    InnerClient.Read(bytes, 0, len);

                    // 执行接收到数据的回调事件
                    InvokeDataReceivedEventCallback(bytes);
                }
            }
        }

        /// <summary>
        /// 向串口发送数据
        /// </summary>
        /// <param name="data"></param>
        protected override void SendHandler(byte[] data)
        {
            try
            {
                // 发送数据
                InnerClient.Write(data, 0, data.Length);

                // 执行数据已发送的回调事件
                RaiseDataSent(this, new DataSentEventArgs(data));
            }
            catch (Exception ex)
            {
                // 通信异常
                RaiseExceptionOccurs(this, new ExceptionOccursEventArgs(ex));
            }
        }

        /// <summary>
        /// 获取当前计算机的串行端口名称的数组。
        /// </summary>
        public string[] GetPortNames()
        {
            return SerialPort.GetPortNames();
        }

        protected override void ReceiveDataCompletedHandle() { }

        protected override void ReceiveDataHandle() { }
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
        //[IgnoreDataMember] 
        //public bool CtsHolding => InnerClient.CtsHolding;

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
        //[IgnoreDataMember] 
        //public bool DsrHolding => InnerClient.DsrHolding;

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
        //[IgnoreDataMember] 
        //public bool CDHolding => InnerClient.CDHolding;

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
        public int ReceiveTimeout { get; set; }

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
