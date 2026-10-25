namespace STTech.BytesIO.Bluetooth.Entity
{
    /// <summary>
    /// BLE 特征值写入选项
    /// </summary>
    public enum BleWriteOption
    {
        /// <summary>
        /// 带响应写入 (Write With Response / Request，对端确认后返回，可靠性高)
        /// </summary>
        WriteWithResponse,

        /// <summary>
        /// 无响应写入 (Write Without Response / Command，无需对端确认，吞吐率高)
        /// </summary>
        WriteWithoutResponse
    }
}
