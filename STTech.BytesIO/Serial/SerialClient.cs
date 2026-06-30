using STTech.BytesIO.Core;
using System;
using System.Buffers;
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
    /// <summary>
    /// 串口通信客户端
    /// </summary>
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

        /// <inheritdoc/>
        [IgnoreDataMember]
        public override bool IsConnected => InnerClient?.IsOpen ?? false;

        /// <summary>
        /// 构造串口通信客户端
        /// </summary>
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
            var sw = System.Diagnostics.Stopwatch.StartNew();
            argument ??= new ConnectArgument();

            // 如果串口已经打开了，则此次连接无效
            if (InnerClient.IsOpen)
            {
                sw.Stop();
                var failedArgs = new ConnectionFailedEventArgs(ConnectErrorCode.IsConnected) { CostTime = sw.Elapsed, State = argument.State };
                RaiseConnectionFailed(this, failedArgs);
                return new ConnectResult(ConnectErrorCode.IsConnected) { CostTime = sw.Elapsed };
            }

            try
            {
                InnerClient.Open();

                // 注册系统级流转 ID
                GenerateNewConnectionId();

                sw.Stop();
                // 执行连接成功回调事件
                RaiseConnectedSuccessfully(this, new ConnectedSuccessfullyEventArgs() { CostTime = sw.Elapsed, State = argument.State });

                // 启动接收数据的异步任务
                StartReceiveDataTask();

                return new ConnectResult() { CostTime = sw.Elapsed };
            }
            catch (Exception ex)
            {
                sw.Stop();
                // 连接失败
                var failedArgs = new ConnectionFailedEventArgs(ConnectErrorCode.Error, ex) { CostTime = sw.Elapsed, State = argument.State };
                RaiseConnectionFailed(this, failedArgs);
                return new ConnectResult(ConnectErrorCode.Error, ex) { CostTime = sw.Elapsed };
            }
        }

        /// <summary>
        /// 关闭串口通信
        /// </summary>
        public override DisconnectResult Disconnect(DisconnectArgument argument = null)
        {
            argument ??= new DisconnectArgument();

            if (argument.GracefulShutdown) WaitAndDrainSendQueue(3000);

            if (argument.ReasonCode == DisconnectionReasonCode.Active && !InnerClient.IsOpen)
            {
                return new DisconnectResult(DisconnectErrorCode.NoConnection);
            }

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
        protected override async Task SendHandlerAsync(SendArgs args)
        {
            try
            {
                var data = args.Data;

                // 异步发送数据
                await InnerClient.BaseStream.WriteAsync(data, 0, data.Length).ConfigureAwait(false);
                if (args.Options.FlushImmediately)
                {
                    await InnerClient.BaseStream.FlushAsync().ConfigureAwait(false);
                }

                // 执行数据已发送的回调事件
                RaiseDataSent(this, new DataSentEventArgs(data));

                // 异步延时
                await Task.Delay(args.Options.PauseTime).ConfigureAwait(false);
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
        protected override async Task ReceiveDataHandleAsync(CancellationToken cancellationToken)
        {
            SerialPort sp = InnerClient;

            int len, offset = 0;
            DateTime? frameDeadline = null;
            byte[] buffer = null;
            try
            {
                var baseStream = sp.BaseStream;
                while (IsConnected && !cancellationToken.IsCancellationRequested)
                {
                    if (offset == 0)
                    {
                        buffer = RentBuffer();
                    }

                    // 原生异步获取数据，利用硬件中断恢复线程，释放 CPU
                    len = await baseStream.ReadAsync(buffer, offset, ReceiveBufferSize - offset, cancellationToken).ConfigureAwait(false);

                    if (len == 0 && sp.BytesToRead == 0)
                    {
                        // 零散断连检测与兑底
                        await Task.Delay(10, cancellationToken).ConfigureAwait(false);
                        continue;
                    }

                    // 收到首字节时记录本帧的绝对截止时间
                    if (frameDeadline == null && ReceiveTimeout > 0)
                    {
                        frameDeadline = DateTime.Now.AddMilliseconds(ReceiveTimeout);
                    }

                    // 延迟等待(粘包接收缓冲)
                    if (ReceiveTimeout > 0 && frameDeadline.HasValue)
                    {
                        var remaining = (frameDeadline.Value - DateTime.Now).TotalMilliseconds;
                        if (remaining > 0)
                        {
                            // 尚未到截止时间：短暂等待后继续累积
                            await Task.Delay((int)Math.Ceiling(ReceiveTimeout / 10.0), cancellationToken).ConfigureAwait(false);
                            offset += len;
                            if (sp.BytesToRead > 0)
                            {
                                continue;
                            }
                            // 超时期间缓冲区已空，直接派发
                            len = 0;
                        }
                        else
                        {
                            // 已超过截止时间，直接派发当前读到的数据
                            offset += len;
                            len = 0;
                        }
                    }

                    // 创建 ReceiveContext（零拷贝）
                    var context = CreateReceiveContext(buffer, 0, offset + len);

                    InvokeDataReceivedEventCallback(context);
                    frameDeadline = null;
                    offset = 0;
                }
            }
            catch (Exception ex)
            {
                // 归还未使用的缓冲区
                if (buffer != null && offset == 0)
                {
                    ArrayPool<byte>.Shared.Return(buffer);
                }

                // 如果是主动关闭的连接，则不触发异常回调
                if (cancellationToken.IsCancellationRequested)
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
        public virtual Handshake Handshake { get => InnerClient.Handshake; set => InnerClient.Handshake = value; }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public virtual bool DtrEnable { get => InnerClient.DtrEnable; set => InnerClient.DtrEnable = value; }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public virtual bool DiscardNull { get => InnerClient.DiscardNull; set => InnerClient.DiscardNull = value; }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public virtual int DataBits { get => InnerClient.DataBits; set => InnerClient.DataBits = value; }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public virtual string NewLine { get => InnerClient.NewLine; set => InnerClient.NewLine = value; }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public override int ReceiveBufferSize { get => InnerClient.ReadBufferSize; set => InnerClient.ReadBufferSize = value; }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public virtual byte ParityReplace { get => InnerClient.ParityReplace; set => InnerClient.ParityReplace = value; }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public virtual string PortName { get => InnerClient.PortName; set => InnerClient.PortName = value; }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public virtual int ReadTimeout { get => InnerClient.ReadTimeout; set => InnerClient.ReadTimeout = value; }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public virtual int ReceivedBytesThreshold { get; set; }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public virtual bool RtsEnable { get => InnerClient.RtsEnable; set => InnerClient.RtsEnable = value; }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public virtual StopBits StopBits { get => InnerClient.StopBits; set => InnerClient.StopBits = value; }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public override int SendBufferSize { get => InnerClient.WriteBufferSize; set => InnerClient.WriteBufferSize = value; }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public virtual int WriteTimeout { get => InnerClient.WriteTimeout; set => InnerClient.WriteTimeout = value; }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public virtual Parity Parity { get => InnerClient.Parity; set => InnerClient.Parity = value; }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public virtual int BaudRate { get => InnerClient.BaudRate; set => InnerClient.BaudRate = value; }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public virtual int ReceiveTimeout { get; set; } = 50;

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public virtual void DiscardInBuffer()
        {
            InnerClient.DiscardInBuffer();
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public virtual void DiscardOutBuffer()
        {
            InnerClient.DiscardOutBuffer();
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public override void Dispose()
        {
            InnerClient?.Dispose();
        }
    }
}
