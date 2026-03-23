using STTech.BytesIO.Core;
using System;
using System.Threading.Tasks;

namespace STTech.BytesIO.Ipc
{
    /// <summary>
    /// IPC服务端接口
    /// </summary>
    public interface IIpcServer : IDisposable
    {
        /// <summary>
        /// 管道名称
        /// </summary>
        string PipeName { get; set; }

        /// <summary>
        /// 启动监听
        /// </summary>
        Task StartAsync();

        /// <summary>
        /// 停止监听
        /// </summary>
        Task StopAsync();

        /// <summary>
        /// 关闭服务
        /// </summary>
        Task CloseAsync();

        /// <summary>
        /// 服务器状态
        /// </summary>
        ServerState State { get; }

        /// <summary>
        /// 是否在运行
        /// </summary>
        bool IsRunning { get; }

        /// <summary>
        /// 是否暂停监听新客户端的加入
        /// </summary>
        bool IsPaused { get; }

        /// <summary>
        /// 是否正在监听新客户端的连接
        /// </summary>
        bool IsListening { get; }

        /// <summary>
        /// 服务器暂停监听事件
        /// </summary>
        event EventHandler Paused;
    }
}
