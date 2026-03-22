using RJCP.IO.Ports;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using STTech.BytesIO.Core;
using STTech.BytesIO.Core;
using STTech.BytesIO.Serial;

namespace STTech.BytesIO.SerialPortStream
{
    /// <summary>
    /// 基于 RJCP.IO.Ports.SerialPortStream 驱动的高级稳定版串口客户端。
    /// 可以作为原生系统 SerialPort 的极佳平替方案来解决部分特殊硬件在 Windows/.NET 下的通讯断连顽疾。
    /// </summary>
    public class SerialPortStreamClient : SerialClient
    {
        /// <summary>
        /// 隐藏基类的系统原生通信对象，暴露并挂载稳定版的第三方串口流机制。
        /// </summary>
        public new RJCP.IO.Ports.SerialPortStream InnerClient { get; private set; }

        public SerialPortStreamClient()
        {
            InnerClient = new RJCP.IO.Ports.SerialPortStream();
        }

        public override ConnectResult Connect(ConnectArgument argument = null)
        {
            return ConnectAsync(argument).GetAwaiter().GetResult();
        }

        public override DisconnectResult Disconnect(DisconnectArgument argument = null)
        {
            if (argument == null) argument = new DisconnectArgument();
            
            if (argument.GracefulShutdown) WaitAndDrainSendQueue(3000);

            try
            {
                if (InnerClient != null && InnerClient.IsOpen)
                {
                    CancelReceiveDataTask();
                    InnerClient.Close();
                }

                // 派发断开完成事件
                RaiseDisconnected(this, new DisconnectedEventArgs(argument.ReasonCode, argument.Exception));

                return new DisconnectResult();
            }
            catch (Exception ex)
            {
                return new DisconnectResult(DisconnectErrorCode.Error, ex);
            }
        }

        public override async Task<ConnectResult> ConnectAsync(ConnectArgument argument = null)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            if (argument == null) argument = new ConnectArgument();

            try
            {
                // 把基类的暴露配置转化为 RJCP 的专属底层枚举：
                InnerClient.PortName = base.PortName;
                InnerClient.BaudRate = base.BaudRate;
                InnerClient.DataBits = base.DataBits;
                InnerClient.Parity = BuildParity(base.Parity);
                InnerClient.StopBits = BuildStopBits(base.StopBits);
                InnerClient.Handshake = BuildHandshake(base.Handshake);
                InnerClient.DtrEnable = base.DtrEnable;
                InnerClient.DiscardNull = base.DiscardNull;
                InnerClient.ReadBufferSize = base.ReceiveBufferSize;

                // 打开串行端口
                InnerClient.Open();

                // 产生全局唯一连接凭证
                GenerateNewConnectionId();

                sw.Stop();
                // 执行连接成功回调系统
                RaiseConnectedSuccessfully(this, new ConnectedSuccessfullyEventArgs() { CostTime = sw.Elapsed, State = argument.State });

                // 启动零拷贝并发接收总线，由于我们隐藏了基类的 InnerClient，这里的收发重写至关重要
                StartReceiveDataTask();

                return new ConnectResult() { CostTime = sw.Elapsed };
            }
            catch (Exception ex)
            {
                sw.Stop();
                if (InnerClient != null && InnerClient.IsOpen)
                {
                    InnerClient.Close();
                }
                var failedArgs = new ConnectionFailedEventArgs(ConnectErrorCode.Error, ex) { CostTime = sw.Elapsed, State = argument.State };
                RaiseConnectionFailed(this, failedArgs);
                return new ConnectResult(ConnectErrorCode.Error, ex) { CostTime = sw.Elapsed };
            }
        }

        /// <summary>
        /// 重写基类的基础发生网关，拦截数据流并送入稳定版的 SerialPortStream。
        /// </summary>
        protected override async Task SendHandlerAsync(SendArgs args)
        {
            try
            {
                var data = args.Data;
                // 原生流式异步下发
                await InnerClient.WriteAsync(data, 0, data.Length).ConfigureAwait(false);
                if (args.Options.FlushImmediately)
                {
                    await InnerClient.FlushAsync().ConfigureAwait(false);
                }

                // 发起通讯完成回调网络
                RaiseDataSent(this, new DataSentEventArgs(data));

                // 异步延时中断
                await Task.Delay(args.Options.PauseTime).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                RaiseExceptionOccurs(this, new ExceptionOccursEventArgs(ex));
            }
        }

        /// <summary>
        /// 重写接收引擎，桥接 Native Async 流与超时装配机制。
        /// </summary>
        protected override async Task ReceiveDataHandleAsync(CancellationToken cancellationToken)
        {
            var sp = InnerClient;

            int len, offset = 0;
            DateTime? startFrameTimestamp = null;
            byte[] buffer = null;

            try
            {
                while (IsConnected && !cancellationToken.IsCancellationRequested)
                {
                    if (offset == 0)
                    {
                        buffer = RentBuffer();
                    }

                    // Native Async Await 挂靠
                    len = await sp.ReadAsync(buffer, offset, ReceiveBufferSize - offset, cancellationToken).ConfigureAwait(false);

                    if (len == 0 && sp.BytesToRead == 0)
                    {
                        await Task.Delay(10, cancellationToken).ConfigureAwait(false);
                        continue;
                    }

                    if (startFrameTimestamp == null) startFrameTimestamp = ReceiveTimeout > 0 ? DateTime.Now : (DateTime?)null;

                    if (ReceiveTimeout > 0)
                    {
                        var diffTime = (DateTime.Now - startFrameTimestamp.Value).TotalMilliseconds;
                        if (diffTime < ReceiveTimeout)
                        {
                            await Task.Delay((int)Math.Ceiling(ReceiveTimeout / 10.0), cancellationToken).ConfigureAwait(false);
                            if (sp.BytesToRead > 0)
                            {
                                offset += len;
                                continue;
                            }
                        }
                    }

                    var context = CreateReceiveContext(buffer, 0, offset + len);

                    InvokeDataReceivedEventCallback(context);
                    startFrameTimestamp = null;
                    offset = 0;
                }
            }
            catch (Exception ex)
            {
                if (buffer != null && offset == 0)
                {
                    System.Buffers.ArrayPool<byte>.Shared.Return(buffer);
                }

                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                if (!IsConnected)
                {
                    Disconnect(new DisconnectArgument(DisconnectionReasonCode.Passive, ex));
                }
                else
                {
                    RaiseExceptionOccurs(this, new ExceptionOccursEventArgs(ex));
                    Disconnect(new DisconnectArgument(DisconnectionReasonCode.Error, ex));
                }
            }
        }

        /// <summary>
        /// 当释放资源时确保卸载掉底层的组件。
        /// </summary>
        public override void Dispose()
        {
            InnerClient?.Dispose();
            InnerClient = null;
        }

        // ================= 枚举系统无痕翻译桥梁 =================

        private RJCP.IO.Ports.Parity BuildParity(System.IO.Ports.Parity parity)
        {
            switch (parity)
            {
                case System.IO.Ports.Parity.None: return RJCP.IO.Ports.Parity.None;
                case System.IO.Ports.Parity.Odd: return RJCP.IO.Ports.Parity.Odd;
                case System.IO.Ports.Parity.Even: return RJCP.IO.Ports.Parity.Even;
                case System.IO.Ports.Parity.Mark: return RJCP.IO.Ports.Parity.Mark;
                case System.IO.Ports.Parity.Space: return RJCP.IO.Ports.Parity.Space;
                default: return RJCP.IO.Ports.Parity.None;
            }
        }

        private RJCP.IO.Ports.StopBits BuildStopBits(System.IO.Ports.StopBits stopBits)
        {
            switch (stopBits)
            {
                case System.IO.Ports.StopBits.None: return RJCP.IO.Ports.StopBits.One;
                case System.IO.Ports.StopBits.One: return RJCP.IO.Ports.StopBits.One;
                case System.IO.Ports.StopBits.Two: return RJCP.IO.Ports.StopBits.Two;
                case System.IO.Ports.StopBits.OnePointFive: return RJCP.IO.Ports.StopBits.One5;
                default: return RJCP.IO.Ports.StopBits.One;
            }
        }

        private RJCP.IO.Ports.Handshake BuildHandshake(System.IO.Ports.Handshake handshake)
        {
            switch (handshake)
            {
                case System.IO.Ports.Handshake.None: return RJCP.IO.Ports.Handshake.None;
                case System.IO.Ports.Handshake.XOnXOff: return RJCP.IO.Ports.Handshake.XOn;
                case System.IO.Ports.Handshake.RequestToSend: return RJCP.IO.Ports.Handshake.Rts;
                case System.IO.Ports.Handshake.RequestToSendXOnXOff: return RJCP.IO.Ports.Handshake.RtsXOn;
                default: return RJCP.IO.Ports.Handshake.None;
            }
        }
    }
}
