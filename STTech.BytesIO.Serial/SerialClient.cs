using STTech.BytesIO.Core;
using STTech.BytesIO.Core.Entity;
using System;
using System.Collections;
using System.ComponentModel;
using System.IO;
using System.IO.Ports;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;

namespace STTech.BytesIO.Serial
{
    public class SerialClient : BytesClient, ISerialClient
    {
        /// <summary>
        /// 串口通信对象
        /// </summary>
        protected SerialPort innerClient = null;

        /// <summary>
        /// 获取或设置用于串口传输数据的握手协议
        /// 默认为None。
        /// </summary>
        public Handshake Handshake { get => innerClient.Handshake; set => innerClient.Handshake = value; }

        /// <summary>
        /// 获取或设置文本传输前和传输后转换的字节编码。
        /// 默认为System.Text.ASCIIEncoding。
        /// </summary>
        protected Encoding Encoding { get => innerClient.Encoding; set => innerClient.Encoding = value; }

        /// <summary>
        /// 获取或设置一个值，该值在串行通信期间启用数据终端就绪(DTR)信号。
        /// 使数据终端就绪(DTR);否则,假的。默认为false。
        /// </summary>
        public bool DtrEnable { get => innerClient.DiscardNull; set => innerClient.DiscardNull = value; }

        /// <summary>
        /// 获取清除到发送行的状态。
        /// 如果检测到清除发送行，则为true;否则为False。
        /// </summary>
        [IgnoreDataMember]
        public bool CtsHolding => innerClient.CtsHolding;
        /// <summary>
        /// 获取或设置一个值，该值指示在端口和接收缓冲区之间传输时是否忽略空字节。
        /// </summary>
        public bool DiscardNull { get => innerClient.DiscardNull; set => innerClient.DiscardNull = value; }

        /// <summary>
        /// 获取或设置每个字节的数据位的标准长度。
        /// 有效值范围：5~8
        /// </summary>
        public int DataBits { get => innerClient.DataBits; set => innerClient.DataBits = value; }

        /// <summary>
        /// 获取一个指示打开或关闭状态的值
        /// </summary>
        [IgnoreDataMember]
        public override bool IsConnected => innerClient.IsOpen;

        /// <summary>
        /// 获取数据集就绪(DSR)信号的状态。
        /// </summary>
        [IgnoreDataMember]
        public bool DsrHolding => innerClient.DsrHolding;

        /// <summary>
        /// 获取或设置用于解释对System.IO.Ports.SerialPort.ReadLine和System.IO.Ports.SerialPort.WriteLine(System.String)方法调用结束的值。
        /// 表示一行结束的值。默认是换行符(c#中的“\n”或VisualBasic中的Microsoft.VisualBasic.Constants.vbLf)。
        /// </summary>
        public string NewLine { get => innerClient.NewLine; set => innerClient.NewLine = value; }

        /// <summary>
        /// 获取或设置输入缓冲区的大小。
        /// 缓冲区大小以字节为单位,默认值是4096。
        /// 最大值是一个正整数(2147483647)。
        /// </summary>
        public override int ReceiveBufferSize { get => innerClient.ReadBufferSize; set => innerClient.ReadBufferSize = value; }

        /// <summary>
        /// 获取或设置发生奇偶校验错误时替换数据流中无效字节的字节。
        /// </summary>
        public byte ParityReplace { get => innerClient.ParityReplace; set => innerClient.ParityReplace = value; }

        /// <summary>
        /// 获取或设置用于通信的端口，包括但不限于所有可用的COM端口。
        /// 通信端口默认为COM1。
        /// </summary>
        public string PortName { get => innerClient.PortName; set => innerClient.PortName = value; }

        /// <summary>
        /// 获取端口的Carrier Detect行状态。
        /// </summary>
        [IgnoreDataMember]
        public bool CDHolding => innerClient.CDHolding;

        /// <summary>
        /// 获取或设置当读取操作未完成时发生超时之前的毫秒数。
        /// </summary>
        public int ReadTimeout { get => innerClient.ReadTimeout; set => innerClient.ReadTimeout = value; }

        /// <summary>
        /// 获取或设置在System.IO.Ports.SerialPort.DataReceived事件发生之前内部输入缓冲区中的字节数。
        /// 触发System.IO.Ports.SerialPort.DataReceived事件之前内部输入缓冲区中的字节数。缺省值是1。
        /// </summary>
        public int ReceivedBytesThreshold { get; set; }
        /// <summary>
        /// 获取或设置一个值，该值指示在串行通信期间是否启用发送请求(RTS)信号。
        /// </summary>
        public bool RtsEnable { get => innerClient.RtsEnable; set => innerClient.RtsEnable = value; }

        /// <summary>
        /// 获取或设置每个字节的标准停止位数。
        /// </summary>
        public StopBits StopBits { get => innerClient.StopBits; set => innerClient.StopBits = value; }

        /// <summary>
        /// 获取或设置串行端口输出缓冲区的大小。
        /// 输出缓冲区的大小默认为2048。
        /// </summary>
        public override int SendBufferSize { get => innerClient.WriteBufferSize; set => innerClient.WriteBufferSize = value; }

        /// <summary>
        /// 获取或设置当写操作未完成时发生超时之前的毫秒数。
        /// 超时发生前的毫秒数。默认为System.IO.Ports.SerialPort.InfiniteTimeout。
        /// </summary>
        public int WriteTimeout { get => innerClient.WriteTimeout; set => innerClient.WriteTimeout = value; }

        /// <summary>
        /// 获取或设置奇偶检查协议。
        /// 默认为System.IO.Ports.Parity.None。
        /// </summary>
        public Parity Parity { get => innerClient.Parity; set => innerClient.Parity = value; }

        /// <summary>
        /// 获取发送缓冲区中数据的字节数。
        /// </summary>
        [IgnoreDataMember]
        public int BytesToWrite => innerClient.BytesToWrite;

        /// <summary>
        /// 获取或设置串口波特率。
        /// </summary>
        public int BaudRate { get => innerClient.BaudRate; set => innerClient.BaudRate = value; }

        [Description("标准波特率(常用值)")]
        public StandardBaudRate BaudRateStandard
        {
            get
            {
                var arr = Enum.GetValues(typeof(StandardBaudRate));
                foreach (int br in arr)
                {
                    if (br == BaudRate) return (StandardBaudRate)BaudRate;
                }
                return StandardBaudRate.NonStandard;
            }
            set
            {
                BaudRate = (int)value;
            }
        }

        /// <summary>
        /// 获取或设置中断信号状态。
        /// 如果端口处于中断状态，则为True;否则为False。
        /// </summary>
        [IgnoreDataMember]
        public bool BreakState { get => innerClient.BreakState; set => innerClient.BreakState = value; }

        /// <summary>
        /// 获取System.IO.Ports.SerialPort对象的底层System.IO.Stream对象。
        /// </summary>
        [IgnoreDataMember]
        public Stream BaseStream => innerClient.BaseStream;

        /// <summary>
        /// 获取接收缓冲区中数据的字节数。
        /// </summary>
        [IgnoreDataMember]
        public int BytesToRead => innerClient.BytesToRead;

        public SerialClient()
        {
            // 初始化
            innerClient = new SerialPort();

            // 添加数据接收回调事件
            innerClient.DataReceived += InnerClient_DataReceived;
        }

        /// <summary>
        /// 建立串口通信
        /// </summary>
        public override void Connect()
        {
            if (innerClient.IsOpen) return;

            try
            {
                innerClient.Open();

                // 执行连接成功回调事件
                RaiseConnectedSuccessfully(this, new ConnectedSuccessfullyEventArgs());

                // 启动接收数据的异步任务
                // 注：SerialPort已提供了DataReceived事件，不需要再实现异步接收
                // StartReceiveDataTask();
            }
            catch (Exception ex)
            {
                // 连接失败
                RaiseConnectionFailed(this, new ConnectionFailedEventArgs(ex.Message));
            }
        }

        /// <summary>
        /// 关闭串口通信
        /// </summary>
        public override void Disconnect(DisconnectionReasonCode code = DisconnectionReasonCode.Active, Exception ex = null)
        {
            if (!innerClient.IsOpen) return;

            // 关闭异步任务
            // 注：SerialPort已提供了DataReceived事件，不需要再实现异步接收
            // CancelReceiveDataTask();

            // 关闭串口
            innerClient.Close();

            // 执行通信已断开的回调事件 
            RaiseDisconnected(this, new DisconnectedEventArgs() { ReasonCode = DisconnectionReasonCode.Active});
        }

        /// <summary>
        /// 数据接收回调事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void InnerClient_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            // 判断缓冲区是否有数据
            int len = BytesToRead;
            if (len > 0)
            {
                // 获取数据
                var bytes = new byte[len];
                innerClient.Read(bytes, 0, len);

                // 更新时间戳
                UpdateLastMessageTimestamp();

                // 执行接收到数据的回调事件
                RaiseDataReceived(this, new DataReceivedEventArgs(bytes));
            }
        }

        /// <summary>
        /// 向串口发送数据
        /// </summary>
        /// <param name="data"></param>
        public override void Send(byte[] data)
        {
            try
            {
                // 发送数据
                innerClient.Write(data, 0, data.Length);

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

        /// <summary>
        /// 丢弃来自串行驱动程序的接收缓冲区的数据
        /// </summary>
        public void DiscardInBuffer()
        {
            innerClient.DiscardInBuffer();
        }

        /// <summary>
        /// 丢弃来自串行驱动程序的传输缓冲区的数据
        /// </summary>
        public void DiscardOutBuffer()
        {
            innerClient.DiscardOutBuffer();
        }
    }

    /// <summary>
    /// 标准波特率
    /// </summary>
    public enum StandardBaudRate : int
    {
        NonStandard = 0,
        BR_115200 = 115200,
        BR_57600 = 57600,
        BR_56000 = 56000,
        BR_43000 = 43000,
        BR_38400 = 38400,
        BR_19200 = 19200,
        BR_9600 = 9600,
        BR_4800 = 4800,
    }
}
