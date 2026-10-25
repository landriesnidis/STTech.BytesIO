namespace STTech.BytesIO.Bluetooth.Entity
{
    /// <summary>
    /// 蓝牙类型
    /// </summary>
    public enum BluetoothType
    {
        /// <summary>
        /// 经典蓝牙 (BR/EDR, 支持 RFCOMM / SPP 串行端口规范)
        /// </summary>
        Classic,

        /// <summary>
        /// 低功耗蓝牙 (BLE, 支持 GATT 服务与特征值通信)
        /// </summary>
        Ble
    }
}
