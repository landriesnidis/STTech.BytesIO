using STTech.BytesIO.Core;
using STTech.BytesIO.Ipc.Entity;
using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;

namespace STTech.BytesIO.Ipc
{
    /// <summary>
    /// 基于命名管道(控制面) + 命名共享内存(数据面)的超大数据IPC客户端。
    /// 
    /// 设计目标：利用 Windows 共享内存机制实现超大数据的进程间零拷贝传递。
    /// - 数据面：数据写入命名共享内存区域，接收端通过 ReceiveContext 直接引用共享内存视图（零拷贝），
    ///   完全绕过 ArrayPool 与托管堆的大块数据搬运；
    /// - 控制面：命名管道仅传输定长的内部控制帧（消息元数据 + ACK 确认），对调用方不可见；
    /// - 区域布局：共享内存区域按方向划分为两个半区（客户端→服务端 与 服务端→客户端），
    ///   每个方向各为单槽位协议：发送端发布数据后必须等待接收端 ACK
    ///   （ReceiveContext 引用计数归零时触发）才能复用该半区，天然提供背压，防止"读慢写快"造成数据覆盖。
    /// 
    /// 公共 API 与 IpcClient 完全一致（Send / OnDataReceived 等）。
    /// 注意：共享内存区域容量需要通信双方配置一致（SharedMemoryRegionSize）；
    /// 每个方向的实际可用容量为 SharedMemoryRegionSize 的一半。
    /// </summary>
    public class SharedMemoryIpcClient : IpcClient
    {
        /// <summary>
        /// 共享内存映射名称
        /// 未设置时默认使用 "STTech.BytesIO.SharedMemory." + PipeName
        /// 通信双方需保持一致
        /// </summary>
        public string MapName { get; set; }

        /// <summary>
        /// 共享内存区域大小 (字节)
        /// 通信双方需配置一致；默认 1GB (每个方向各占一半)
        /// </summary>
        public long SharedMemoryRegionSize
        {
            get => _sharedMemoryRegionSize;
            set
            {
                if (value <= 0) throw new ArgumentOutOfRangeException(nameof(value), "共享内存区域大小必须大于 0");
                if (value / 2 > int.MaxValue) throw new ArgumentOutOfRangeException(nameof(value), $"单个方向的共享内存容量不能超过 int.MaxValue ({int.MaxValue} 字节)");
                _sharedMemoryRegionSize = value;
            }
        }
        private long _sharedMemoryRegionSize = 1L * 1024 * 1024 * 1024;

        /// <summary>
        /// 等待对端 ACK (共享内存槽位释放) 的超时时间 (毫秒)
        /// 小于等于 0 表示无限等待；默认 60 秒
        /// </summary>
        public int AckTimeoutMs { get; set; } = 60000;

        /// <summary>
        /// 共享内存区域是否已就绪
        /// </summary>
        public bool IsSharedMemoryReady => _regionPointer != IntPtr.Zero && _regionLength > 0;

        /// <summary>
        /// 本端(发送方向)可用的数据容量 (字节)
        /// 等于共享内存区域大小的一半
        /// </summary>
        public long DataCapacity => _halfSize > 0 ? _halfSize : (_sharedMemoryRegionSize / 2);

        /// <summary>
        /// 管道名称 (覆写属性)
        /// 共享内存映射名称依赖管道名称, 因此需要在赋值时同步确认共享内存配置
        /// (服务端封装场景下 PipeName 在构造完成后才通过对象初始化器赋值)
        /// </summary>
        public new string PipeName
        {
            get => base.PipeName;
            set
            {
                base.PipeName = value;
                MarkSharedMemoryConfigured();
            }
        }

        /// <summary>
        /// 是否为服务端角色
        /// 服务端角色使用共享区的后半区发送数据, 客户端角色使用前半区
        /// </summary>
        private readonly bool _isServerRole;

        // ==================== 共享内存资源 ====================

        private MemoryMappedFile _mappedFile;
        private MemoryMappedViewAccessor _viewAccessor;
        private IntPtr _regionPointer;
        private long _regionLength;

        /// <summary>
        /// 活跃的接收上下文计数 (防止断开时提前释放非托管内存映射导致野指针崩溃)
        /// </summary>
        private int _activeReceiveContextCount;

        /// <summary>
        /// 本端发送半区的起始偏移 (共享区内绝对偏移)
        /// </summary>
        private long _sendHalfOffset;

        /// <summary>
        /// 单个方向的可用容量 (区域大小的一半)
        /// </summary>
        private long _halfSize;

        /// <summary>
        /// 共享区域访问闸：串行化 打开/释放 与 发送拷贝，防止断开时访问已释放的原生内存
        /// </summary>
        private readonly SemaphoreSlim _regionAccessLock = new SemaphoreSlim(1, 1);

        /// <summary>
        /// 共享内存配置是否已就绪
        /// 客户端在 Connect 后即就绪；服务端需要等对象初始化器赋值 PipeName
        /// </summary>
        private volatile bool _sharedMemoryConfigured;

        // ==================== 控制面 (管道) ====================

        /// <summary>
        /// 接收循环使用的定长控制帧缓冲 (单线程循环, 无需加锁)
        /// </summary>
        private readonly byte[] _controlFrameBuffer = new byte[ControlFrame.FrameSize];

        /// <summary>
        /// 发送控制帧的缓冲 (由 _pipeWriteLock 保护)
        /// </summary>
        private readonly byte[] _writeFrameBuffer = new byte[ControlFrame.FrameSize];

        /// <summary>
        /// 管道写锁：发送泵线程与 ACK 确认线程同时写管道, 需要串行化
        /// </summary>
        private readonly SemaphoreSlim _pipeWriteLock = new SemaphoreSlim(1, 1);

        // ==================== 协议状态 ====================

        private int _sendSequence;

        /// <summary>
        /// 期望接收的下一帧序号
        /// </summary>
        private int _receiveSequence;

        /// <summary>
        /// 槽位状态锁
        /// </summary>
        private readonly object _slotLocker = new object();

        /// <summary>
        /// 已成功发布(占用槽位)的帧数
        /// </summary>
        private int _publishedCount;

        /// <summary>
        /// 已收到 ACK 的帧数
        /// </summary>
        private int _ackedCount;

        /// <summary>
        /// 下一次 ACK 到达时的信号 (有发送方等待时创建)
        /// </summary>
        private TaskCompletionSource<bool> _ackSignalTcs;

        /// <summary>
        /// 构造共享内存IPC客户端
        /// </summary>
        public SharedMemoryIpcClient()
        {
            _isServerRole = false;
        }

        /// <summary>
        /// 构造共享内存IPC客户端
        /// </summary>
        /// <param name="pipeStream">内部管道流</param>
        public SharedMemoryIpcClient(PipeStream pipeStream) : base(pipeStream)
        {
            _isServerRole = pipeStream is NamedPipeServerStream;
        }

        /// <inheritdoc/>
        public override ConnectResult Connect(ConnectArgument argument = null)
        {
            var result = base.Connect(argument);
            ApplySharedMemoryConfiguration(result);
            return result;
        }

        /// <inheritdoc/>
        public override async Task<ConnectResult> ConnectAsync(ConnectArgument argument = null)
        {
            var result = await base.ConnectAsync(argument).ConfigureAwait(false);
            ApplySharedMemoryConfiguration(result);
            return result;
        }

        /// <summary>
        /// 连接完成后确认共享内存配置并打开共享区域
        /// </summary>
        private void ApplySharedMemoryConfiguration(ConnectResult result)
        {
            _sharedMemoryConfigured = true;
            if (result.IsSuccess)
            {
                lock (_slotLocker)
                {
                    _sendSequence = 0;
                    _receiveSequence = 0;
                    _publishedCount = 0;
                    _ackedCount = 0;
                    var oldSignal = _ackSignalTcs;
                    _ackSignalTcs = null;
                    oldSignal?.TrySetResult(true);
                }

                try
                {
                    EnsureSharedMemoryOpened();
                }
                catch (Exception ex)
                {
                    RaiseExceptionOccurs(this, new ExceptionOccursEventArgs(ex));
                }
            }
        }

        /// <summary>
        /// 配置就绪回调 (由 PipeName 赋值触发, 服务端封装场景)
        /// </summary>
        private void MarkSharedMemoryConfigured()
        {
            if (!_sharedMemoryConfigured)
            {
                _sharedMemoryConfigured = true;
                if (IsConnected)
                {
                    try
                    {
                        EnsureSharedMemoryOpened();
                    }
                    catch
                    {
                        // 打开失败由接收/发送路径重试
                    }
                }
            }
        }

        /// <inheritdoc/>
        public override DisconnectResult Disconnect(DisconnectArgument argument = null)
        {
            // 先解除槽位等待, 避免发送队列悬挂
            lock (_slotLocker)
            {
                var signal = _ackSignalTcs;
                _ackSignalTcs = null;
                signal?.TrySetResult(true);
            }

            var result = base.Disconnect(argument);
            ReleaseSharedMemoryAfterDisconnect();
            return result;
        }

        /// <inheritdoc/>
        protected override void ReceiveDataCompletedHandle()
        {
            base.ReceiveDataCompletedHandle();
            ReleaseSharedMemoryAfterDisconnect();
        }

        /// <inheritdoc/>
        public override void Dispose()
        {
            base.Dispose();
            ReleaseSharedMemoryAfterDisconnect();
            _pipeWriteLock.Dispose();
        }

        // =====================================================================
        //                                   发送
        // =====================================================================

        /// <inheritdoc/>
        protected override async Task SendHandlerAsync(SendArgs args)
        {
            try
            {
                if (!IsConnected)
                {
                    return;
                }

                // 发送时配置必然已就绪, 确保共享区域已打开
                EnsureSharedMemoryOpened();

                // 1. 等待共享内存槽位释放 (上一帧的 ACK)
                await WaitForSlotAsync().ConfigureAwait(false);

                // 2. 区域访问闸: 防止与断开释放竞态
                await _regionAccessLock.WaitAsync().ConfigureAwait(false);
                try
                {
                    if (!IsConnected)
                    {
                        return;
                    }

                    if (args.Data.Length > DataCapacity)
                    {
                        throw new ArgumentOutOfRangeException(nameof(args), $"待发送数据长度 ({args.Data.Length}) 超过本方向共享内存容量 ({DataCapacity})");
                    }

                    // 3. 拷贝数据到共享内存 (全链路唯一一次用户态拷贝)
                    CopyToSharedRegion(args.Data);

                    // 4. 发布数据帧
                    // 数据先写入共享区, 再发送控制帧; 管道 FIFO 提供跨进程内存可见性保证
                    var frame = new ControlFrame
                    {
                        Type = (byte)ControlFrame.FrameType.Data,
                        Sequence = Interlocked.Increment(ref _sendSequence),
                        Length = args.Data.Length,
                        DataOffset = _sendHalfOffset,
                    };
                    await WriteFrameAsync(frame).ConfigureAwait(false);

                    // 发布成功后占用槽位, 直到收到对端 ACK
                    lock (_slotLocker)
                    {
                        _publishedCount++;
                    }

                    if (args.Options.FlushImmediately)
                    {
                        await InnerClient.FlushAsync().ConfigureAwait(false);
                    }

                    RaiseDataSent(this, new DataSentEventArgs(args.Data));

                    if (args.Options.PauseTime > 0)
                    {
                        await Task.Delay(args.Options.PauseTime).ConfigureAwait(false);
                    }
                }
                finally
                {
                    _regionAccessLock.Release();
                }
            }
            catch (Exception ex)
            {
                RaiseExceptionOccurs(this, new ExceptionOccursEventArgs(ex));
                throw;
            }
        }

        /// <summary>
        /// 等待共享内存槽位释放
        /// 槽位空闲等价于: 所有已发布的帧均已收到 ACK (ackedCount >= publishedCount)
        /// </summary>
        private async Task WaitForSlotAsync()
        {
            if (!IsConnected) return;

            Task waitTask;
            lock (_slotLocker)
            {
                if (_ackedCount >= _publishedCount)
                {
                    // 槽位当前空闲
                    waitTask = Task.CompletedTask;
                }
                else
                {
                    // 等待下一次 ACK
                    _ackSignalTcs ??= new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                    waitTask = _ackSignalTcs.Task;
                }
            }

            if (AckTimeoutMs > 0 && AckTimeoutMs != Timeout.Infinite)
            {
                var completedTask = await Task.WhenAny(waitTask, Task.Delay(AckTimeoutMs)).ConfigureAwait(false);
                if (completedTask != waitTask)
                {
                    if (!IsConnected) return;
                    throw new TimeoutException($"等待对端确认共享内存槽位释放超时 ({AckTimeoutMs}ms)");
                }
            }
            else
            {
                await waitTask.ConfigureAwait(false);
            }
        }

        /// <summary>
        /// 将数据拷贝到本端发送半区
        /// </summary>
        private unsafe void CopyToSharedRegion(byte[] data)
        {
            var destination = new Span<byte>((byte*)_regionPointer + _sendHalfOffset, (int)_halfSize);
            data.AsSpan().CopyTo(destination);
        }

        // =====================================================================
        //                                   接收
        // =====================================================================

        /// <inheritdoc/>
        protected override async Task ReceiveDataHandleAsync(CancellationToken cancellationToken)
        {
            try
            {
                while (IsConnected && !cancellationToken.IsCancellationRequested)
                {
                    // 1. 读取定长控制帧
                    await ReadExactAsync(_controlFrameBuffer, 0, ControlFrame.FrameSize, cancellationToken).ConfigureAwait(false);

                    var frame = ControlFrame.ReadFrom(_controlFrameBuffer);
                    if (frame.MagicValue != ControlFrame.Magic)
                    {
                        RaiseProtocolError($"魔术字校验失败 (0x{frame.MagicValue:X4})");
                        return;
                    }

                    switch ((ControlFrame.FrameType)frame.Type)
                    {
                        case ControlFrame.FrameType.Data:
                            {
                                // 服务端封装场景下, 管道名称等配置在构造完成后才赋值, 需等待配置就绪
                                await WaitUntilConfiguredAsync(cancellationToken).ConfigureAwait(false);
                                EnsureSharedMemoryOpened();

                                if (!IsSharedMemoryReady)
                                {
                                    RaiseProtocolError("共享内存区域未就绪");
                                    return;
                                }

                                // 帧序号必须严格递增 (单槽位协议)
                                if (frame.Sequence != _receiveSequence + 1)
                                {
                                    RaiseProtocolError($"帧序号不连续 (期望 {_receiveSequence + 1}, 实际 {frame.Sequence})");
                                    return;
                                }
                                if (frame.Length < 0 || frame.Length > _halfSize ||
                                    frame.DataOffset < _receiveHalfOffset ||
                                    frame.DataOffset + frame.Length > _receiveHalfOffset + _halfSize)
                                {
                                    RaiseProtocolError($"数据范围越界 (DataOffset={frame.DataOffset}, Length={frame.Length}, ReceiveHalf=[{_receiveHalfOffset}, {_receiveHalfOffset + _halfSize}))");
                                    return;
                                }

                                _receiveSequence = frame.Sequence;

                                // 2. 零拷贝构建 ReceiveContext (原生内存后端, 直接引用共享区)
                                var context = CreateSharedMemoryReceiveContext(frame.DataOffset, frame.Length, frame.Sequence);
                                InvokeDataReceivedEventCallback(context);
                                break;
                            }
                        case ControlFrame.FrameType.Ack:
                            {
                                // 槽位释放信号
                                lock (_slotLocker)
                                {
                                    _ackedCount++;
                                    var signal = _ackSignalTcs;
                                    _ackSignalTcs = null;
                                    signal?.TrySetResult(true);
                                }
                                break;
                            }
                        default:
                            {
                                RaiseProtocolError($"未知的帧类型 ({frame.Type})");
                                return;
                            }
                    }
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (EndOfStreamException)
            {
                // 管道对端关闭
                if (!cancellationToken.IsCancellationRequested)
                {
                    Disconnect(new DisconnectArgument(DisconnectionReasonCode.Passive));
                }
            }
            catch (Exception ex)
            {
                if (!cancellationToken.IsCancellationRequested)
                {
                    RaiseExceptionOccurs(this, new ExceptionOccursEventArgs(ex));
                    Disconnect(new DisconnectArgument(DisconnectionReasonCode.Passive, ex));
                }
            }
        }

        /// <summary>
        /// 本端接收半区的起始偏移
        /// </summary>
        private long _receiveHalfOffset => _isServerRole ? 0 : _halfSize;

        /// <summary>
        /// 等待共享内存配置就绪
        /// </summary>
        private async Task WaitUntilConfiguredAsync(CancellationToken cancellationToken)
        {
            while (!_sharedMemoryConfigured && IsConnected && !cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(1, cancellationToken).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// 协议错误处理: 上报异常并断开连接
        /// </summary>
        private void RaiseProtocolError(string message)
        {
            RaiseExceptionOccurs(this, new ExceptionOccursEventArgs(new InvalidDataException($"共享内存IPC协议错误: {message}")));
            Disconnect(new DisconnectArgument(DisconnectionReasonCode.Passive));
        }

        /// <summary>
        /// 零拷贝构建原生内存后端的接收上下文
        /// 释放 (引用计数归零) 时自动向对端发送 ACK, 解锁共享槽位
        /// </summary>
        private unsafe ReceiveContext CreateSharedMemoryReceiveContext(long offset, long length, int sequence)
        {
            var pointer = new IntPtr((byte*)_regionPointer + offset);
            var manager = new NativeMemoryManager(pointer, (int)length);
            Interlocked.Increment(ref _activeReceiveContextCount);
            return new ReceiveContext(manager, () =>
            {
                try
                {
                    SendAck(sequence);
                }
                finally
                {
                    if (Interlocked.Decrement(ref _activeReceiveContextCount) == 0 && !IsConnected)
                    {
                        ReleaseSharedMemoryAfterDisconnect();
                    }
                }
            });
        }

        /// <summary>
        /// 从管道精确读取指定长度的数据
        /// </summary>
        private async Task ReadExactAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            int read = 0;
            while (read < count)
            {
                int len = await InnerClient.ReadAsync(buffer, offset + read, count - read, cancellationToken).ConfigureAwait(false);
                if (len == 0)
                {
                    throw new EndOfStreamException("管道对端已关闭");
                }
                read += len;
            }
        }

        // =====================================================================
        //                                  控制帧
        // =====================================================================

        /// <summary>
        /// 向对端发送控制帧 (线程安全)
        /// </summary>
        private async Task WriteFrameAsync(ControlFrame frame)
        {
            frame.MagicValue = ControlFrame.Magic;

            await _pipeWriteLock.WaitAsync().ConfigureAwait(false);
            try
            {
                if (IsConnected)
                {
                    frame.WriteTo(_writeFrameBuffer);
                    await InnerClient.WriteAsync(_writeFrameBuffer, 0, ControlFrame.FrameSize).ConfigureAwait(false);
                    await InnerClient.FlushAsync().ConfigureAwait(false);
                }
            }
            finally
            {
                _pipeWriteLock.Release();
            }
        }

        /// <summary>
        /// 发送 ACK 确认帧 (在 ReceiveContext 释放回调中触发, 不可失败)
        /// </summary>
        private void SendAck(int sequence)
        {
            _ = SendAckCoreAsync(sequence);
        }

        private async Task SendAckCoreAsync(int sequence)
        {
            try
            {
                var frame = new ControlFrame
                {
                    Type = (byte)ControlFrame.FrameType.Ack,
                    Sequence = sequence,
                };
                await WriteFrameAsync(frame).ConfigureAwait(false);
            }
            catch
            {
                // ACK 发送失败时忽略 (连接已断开等场景)
            }
        }

        // =====================================================================
        //                                共享内存管理
        // =====================================================================

        /// <summary>
        /// 确保共享内存区域已打开 (线程安全)
        /// </summary>
        private void EnsureSharedMemoryOpened()
        {
            _regionAccessLock.Wait();
            try
            {
                OpenSharedMemoryCore();
            }
            finally
            {
                _regionAccessLock.Release();
            }
        }

        /// <summary>
        /// 打开(或创建)命名共享内存映射
        /// 调用方需持有 _regionAccessLock
        /// </summary>
        private void OpenSharedMemoryCore()
        {
            if (_mappedFile != null)
            {
                return;
            }

            var mapName = MapName ?? BuildDefaultMapName();
            _mappedFile = MemoryMappedFile.CreateOrOpen(mapName, SharedMemoryRegionSize, MemoryMappedFileAccess.ReadWrite);
            try
            {
                long viewLength = SharedMemoryRegionSize;
                try
                {
                    _viewAccessor = _mappedFile.CreateViewAccessor(0, viewLength, MemoryMappedFileAccess.ReadWrite);
                }
                catch
                {
                    // 配置容量大于实际映射容量时, 按实际容量打开
                    _viewAccessor = _mappedFile.CreateViewAccessor();
                    viewLength = _viewAccessor.Capacity;
                }

                _regionLength = viewLength;
                _halfSize = _regionLength / 2;
                _sendHalfOffset = _isServerRole ? _halfSize : 0;

                unsafe
                {
                    byte* pointer = null;
                    _viewAccessor.SafeMemoryMappedViewHandle.AcquirePointer(ref pointer);
                    _regionPointer = new IntPtr(pointer);
                }
            }
            catch
            {
                ReleaseSharedMemoryResourcesCore();
                throw;
            }
        }

        /// <summary>
        /// 释放共享内存资源
        /// 调用方需持有 _regionAccessLock
        /// </summary>
        private void ReleaseSharedMemoryResourcesCore()
        {
            try
            {
                if (_viewAccessor != null && !_viewAccessor.SafeMemoryMappedViewHandle.IsClosed)
                {
                    _viewAccessor.SafeMemoryMappedViewHandle.ReleasePointer();
                }
            }
            catch
            {
            }
            finally
            {
                _regionPointer = IntPtr.Zero;
                _regionLength = 0;
                _halfSize = 0;
                _sendHalfOffset = 0;
                try { _viewAccessor?.Dispose(); } catch { }
                _viewAccessor = null;
                try { _mappedFile?.Dispose(); } catch { }
                _mappedFile = null;
            }
        }

        /// <summary>
        /// 断开后释放共享内存资源 (等待在途发送完成)
        /// 若仍有活跃的接收上下文未释放，则推迟释放直到最后一个上下文释放完毕
        /// </summary>
        private void ReleaseSharedMemoryAfterDisconnect()
        {
            _regionAccessLock.Wait();
            try
            {
                if (_activeReceiveContextCount <= 0)
                {
                    ReleaseSharedMemoryResourcesCore();
                }
            }
            finally
            {
                _regionAccessLock.Release();
            }
        }

        /// <summary>
        /// 默认映射名称
        /// </summary>
        private string BuildDefaultMapName() => "STTech.BytesIO.SharedMemory." + PipeName;
    }
}
