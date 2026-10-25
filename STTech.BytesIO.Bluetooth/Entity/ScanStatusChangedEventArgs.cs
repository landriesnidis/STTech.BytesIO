using System;

namespace STTech.BytesIO.Bluetooth.Entity
{
    /// <summary>
    /// 蓝牙扫描状态改变事件参数
    /// </summary>
    public class ScanStatusChangedEventArgs : EventArgs
    {
        /// <summary>
        /// 当前是否处于扫描状态
        /// </summary>
        public bool IsScanning { get; }

        /// <summary>
        /// 构造扫描状态改变事件参数
        /// </summary>
        /// <param name="isScanning">是否处于扫描中</param>
        public ScanStatusChangedEventArgs(bool isScanning)
        {
            IsScanning = isScanning;
        }
    }
}
