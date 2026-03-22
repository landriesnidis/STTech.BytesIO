namespace STTech.BytesIO.Ipc
{
    /// <summary>
    /// IPC通信客户端接口
    /// </summary>
    public interface IIpcClient
    {
        /// <summary>
        /// 管道名称
        /// </summary>
        string PipeName { get; set; }

        /// <summary>
        /// 服务端名称（默认为 "."，表示本地）
        /// </summary>
        string ServerName { get; set; }
    }
}
