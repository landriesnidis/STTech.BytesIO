using System;
using System.IO.Pipes;

namespace STTech.BytesIO.Ipc
{
    /// <summary>
    /// 共享内存IPC服务端 (基于命名管道 + 命名共享内存)
    /// </summary>
    public class SharedMemoryIpcServer : SharedMemoryIpcServer<SharedMemoryIpcClient>
    {
    }

    /// <summary>
    /// 共享内存IPC服务端基类
    /// </summary>
    /// <typeparam name="T">客户端类型</typeparam>
    public abstract class SharedMemoryIpcServer<T> : IpcServer<T> where T : SharedMemoryIpcClient
    {
        /// <summary>
        /// 共享内存映射名称
        /// 未设置时默认使用 "STTech.BytesIO.SharedMemory." + PipeName
        /// 需与客户端保持一致
        /// </summary>
        public string MapName { get; set; }

        /// <summary>
        /// 共享内存区域大小 (字节)
        /// 需与客户端保持一致；默认 1GB
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
        /// 构造共享内存IPC服务端
        /// </summary>
        public SharedMemoryIpcServer()
        {
            // 共享内存单槽位协议天然面向 1:1 通信, 默认只接受单客户端
            MaxConnections = 1;

            EncapsulateStream = pipeStream => EncapsulateClient(pipeStream);
        }

        /// <summary>
        /// 封装客户端实例
        /// 注意：MapName / SharedMemoryRegionSize 需在 PipeName 之前赋值,
        /// 因为 PipeName 的赋值会触发共享内存区域的初始化
        /// </summary>
        protected virtual T EncapsulateClient(NamedPipeServerStream pipeStream)
        {
            var client = (T)Activator.CreateInstance(typeof(T), pipeStream);
            client.SharedMemoryRegionSize = SharedMemoryRegionSize;
            client.MapName = MapName ?? "STTech.BytesIO.SharedMemory." + PipeName;
            client.PipeName = PipeName;
            return client;
        }
    }
}

