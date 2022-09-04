using System.ComponentModel;
using System.IO;
using System.IO.Ports;
using System.Runtime.Serialization;

namespace STTech.BytesIO.Serial
{
    public interface ISerialClient
    {
        /// <summary>
        /// 获取或设置用于通信的端口，包括但不限于所有可用的COM端口。
        /// 通信端口默认为COM1。
        /// </summary>
        string PortName { get; set; }

        /// <summary>
        /// 获取或设置串口波特率。
        /// </summary>
        int BaudRate { get; set; }

        /// <summary>
        /// 获取或设置用于串口传输数据的握手协议
        /// 默认为None。
        /// </summary>
        Handshake Handshake { get; set; }

        /// <summary>
        /// 获取或设置一个值，该值在串行通信期间启用数据终端就绪(DTR)信号。
        /// 使数据终端就绪(DTR);否则,假的。默认为false。
        /// </summary>
        bool DtrEnable { get; set; }

        /// <summary>
        /// 获取或设置一个值，该值指示在串行通信期间是否启用发送请求(RTS)信号。
        /// </summary>
        bool RtsEnable { get; set; }

        /// <summary>
        /// 获取或设置一个值，该值指示在端口和接收缓冲区之间传输时是否忽略空字节。
        /// </summary>
        bool DiscardNull { get; set; }

        /// <summary>
        /// 获取或设置用于解释对System.IO.Ports.SerialPort.ReadLine和System.IO.Ports.SerialPort.WriteLine(System.String)方法调用结束的值。
        /// 表示一行结束的值。默认是换行符(c#中的“\n”或VisualBasic中的Microsoft.VisualBasic.Constants.vbLf)。
        /// </summary>
        string NewLine { get; set; }

        /// <summary>
        /// 获取或设置当读取操作未完成时发生超时之前的毫秒数。
        /// </summary>
        int ReadTimeout { get; set; }

        /// <summary>
        /// 获取或设置当写操作未完成时发生超时之前的毫秒数。
        /// 超时发生前的毫秒数。默认为System.IO.Ports.SerialPort.InfiniteTimeout。
        /// </summary>
        int WriteTimeout { get; set; }

        /// <summary>
        /// 获取或设置输入缓冲区的大小。
        /// 缓冲区大小以字节为单位,默认值是4096。
        /// 最大值是一个正整数(2147483647)。
        /// </summary>
        int ReceiveBufferSize { get; set; }

        /// <summary>
        /// 获取或设置串行端口输出缓冲区的大小。
        /// 输出缓冲区的大小默认为2048。
        /// </summary>
        int SendBufferSize { get; set; }

        /// <summary>
        /// 获取或设置中断信号状态。
        /// 如果端口处于中断状态，则为True;否则为False。
        /// </summary>
        //[IgnoreDataMember]
        //bool BreakState { get; set; }

        /// <summary>
        /// 获取或设置每个字节的标准停止位数。
        /// </summary>
        StopBits StopBits { get; set; }

        /// <summary>
        /// 获取或设置奇偶检查协议。
        /// 默认为System.IO.Ports.Parity.None。
        /// </summary>
        Parity Parity { get; set; }

        /// <summary>
        /// 获取发送缓冲区中数据的字节数。
        /// </summary>
        //[IgnoreDataMember]
        //int BytesToWrite { get; }

        /// <summary>
        /// 获取System.IO.Ports.SerialPort对象的底层System.IO.Stream对象。
        /// </summary>
        //Stream BaseStream { get; }

        /// <summary>
        /// 获取接收缓冲区中数据的字节数。
        /// </summary>
        //[IgnoreDataMember]
        //int BytesToRead { get; }

        /// <summary>
        /// 获取或设置在System.IO.Ports.SerialPort.DataReceived事件发生之前内部输入缓冲区中的字节数。
        /// 触发System.IO.Ports.SerialPort.DataReceived事件之前内部输入缓冲区中的字节数。缺省值是1。
        /// </summary>

        int ReceivedBytesThreshold { get; set; }

        /// <summary>
        /// 获取端口的Carrier Detect行状态。
        /// </summary>
        //[IgnoreDataMember]
        //bool CDHolding { get; }

        /// <summary>
        /// 获取或设置发生奇偶校验错误时替换数据流中无效字节的字节。
        /// </summary>
        byte ParityReplace { get; set; }

        /// <summary>
        /// 获取数据集就绪(DSR)信号的状态。
        /// </summary>
        //[IgnoreDataMember]
        //bool DsrHolding { get; }

        /// <summary>
        /// 获取或设置每个字节的数据位的标准长度。
        /// 有效值范围：5~8
        /// </summary>
        int DataBits { get; set; }

        /// <summary>
        /// 获取清除到发送行的状态。
        /// 如果检测到清除发送行，则为true;否则为False。
        /// </summary>
        //[IgnoreDataMember]
        //bool CtsHolding { get; }

        /// <summary>
        /// 丢弃来自串行驱动程序的接收缓冲区的数据
        /// </summary>
        void DiscardInBuffer();

        /// <summary>
        /// 丢弃来自串行驱动程序的传输缓冲区的数据
        /// </summary>
        void DiscardOutBuffer();
    }
}
