using STTech.BytesIO.Serial;

namespace STTech.BytesIO.SerialPortStream
{
    public static class SerialClientExtensions
    {
        /// <summary>
        /// 修改当前 SerialClient 通信的底层驱动为工业界高容错高可靠稳定版的 SerialPortStream 增强支持库。
        /// 它将完全抛弃自带存在部分顽疾断连现象的 System.IO.Ports.SerialPort。
        /// 注意：这不仅会替换通信引擎，同时会返回一个专用的新衍生拓展代理类实例。
        /// </summary>
        /// <param name="client">原版的客户端（其仅作为参数配置供给体，实际底层流将切换）</param>
        /// <returns>拥有极其健壮生命周期的扩展版串口通讯客户端</returns>
        public static SerialPortStreamClient UseSerialPortStream(this SerialClient client)
        {
            var extensionClient = new SerialPortStreamClient()
            {
                PortName = client.PortName,
                BaudRate = client.BaudRate,
                DataBits = client.DataBits,
                Parity = client.Parity,
                StopBits = client.StopBits,
                Handshake = client.Handshake,
                DtrEnable = client.DtrEnable,
                DiscardNull = client.DiscardNull,
                ReceiveBufferSize = client.ReceiveBufferSize,
                ReceiveTimeout = client.ReceiveTimeout,
            };

            return extensionClient;
        }
    }
}
