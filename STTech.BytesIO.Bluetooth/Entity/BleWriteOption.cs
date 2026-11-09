namespace STTech.BytesIO.Bluetooth.Entity
{
    /// <summary>
    /// BLE 特征值写入选项
    /// </summary>
    public enum BleWriteOption
    {
        /// <summary>
        /// 自适应模式（自动检测特征值权限，优先采用无响应写入以获得高吞吐率；若硬件仅支持带响应写入则自动降级）
        /// </summary>
        Auto = 0,

        /// <summary>
        /// 带响应写入 (Write With Response / Request，对端确认后返回，可靠性高)
        /// </summary>
        WriteWithResponse = 1,

        /// <summary>
        /// 无响应写入 (Write Without Response / Command，无需对端确认，吞吐率高)
        /// </summary>
        WriteWithoutResponse = 2
    }
}
