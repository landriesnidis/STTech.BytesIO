using System.Diagnostics;

namespace STTech.BytesIO.Core
{
    /// <summary>
    /// 单调高精度时钟，不受系统调时、NTP 阶跃回拨影响，兼容全版本 .NET
    /// </summary>
    internal static class MonotonicClock
    {
        private static readonly double TimestampToMilliseconds = 1000.0 / Stopwatch.Frequency;

        /// <summary>
        /// 获取当前单调毫秒时间戳
        /// </summary>
        public static long NowMs => (long)(Stopwatch.GetTimestamp() * TimestampToMilliseconds);
    }
}
