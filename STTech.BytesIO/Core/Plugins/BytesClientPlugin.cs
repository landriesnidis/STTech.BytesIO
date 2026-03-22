using System;

namespace STTech.BytesIO.Core
{
    /// <summary>
    /// BytesClient 插件抽象基类。
    /// 插件拥有完整的生命周期回调，可通过 Client 属性访问所属客户端。
    /// </summary>
    public abstract class BytesClientPlugin : IDisposable
    {
        /// <summary>
        /// 所属客户端（挂载时自动设置）
        /// </summary>
        public BytesClient Client { get; internal set; }

        /// <summary>
        /// 是否已启用
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        // ==================== 生命周期 ====================

        /// <summary>
        /// 插件被挂载到客户端时调用
        /// </summary>
        protected internal virtual void OnAttached() { }

        /// <summary>
        /// 插件从客户端卸载时调用
        /// </summary>
        protected internal virtual void OnDetached() { }

        // ==================== 连接状态回调 ====================

        /// <summary>
        /// 连接成功时调用
        /// </summary>
        protected internal virtual void OnConnectedSuccessfully(ConnectedSuccessfullyEventArgs e) { }

        /// <summary>
        /// 连接失败时调用
        /// </summary>
        protected internal virtual void OnConnectionFailed(ConnectionFailedEventArgs e) { }

        /// <summary>
        /// 连接断开时调用
        /// </summary>
        protected internal virtual void OnDisconnected(DisconnectedEventArgs e) { }

        // ==================== 数据回调 ====================

        /// <summary>
        /// 接收到数据时调用
        /// </summary>
        protected internal virtual void OnDataReceived(DataReceivedEventArgs e) { }

        /// <summary>
        /// 数据发送完成时调用
        /// </summary>
        protected internal virtual void OnDataSent(DataSentEventArgs e) { }

        // ==================== 异常回调 ====================

        /// <summary>
        /// 异常发生时调用
        /// </summary>
        protected internal virtual void OnExceptionOccurs(ExceptionOccursEventArgs e) { }

        // ==================== 释放 ====================

        /// <inheritdoc/>
        public virtual void Dispose() { }
    }
}
