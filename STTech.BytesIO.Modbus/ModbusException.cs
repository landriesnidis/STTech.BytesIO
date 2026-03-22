using System;

namespace STTech.BytesIO.Modbus
{
    /// <summary>
    /// Modbus 异常基类
    /// </summary>
    public class ModbusException : Exception
    {
        public ModbusException(string message) : base(message) { }

        public ModbusException(string message, Exception innerException) : base(message, innerException) { }
    }

    /// <summary>
    /// Modbus 错误码异常
    /// </summary>
    public class ModbusErrorCodeException : ModbusException
    {
        /// <summary>
        /// 构造 Modbus 错误码异常
        /// </summary>
        /// <param name="code">错误码</param>
        public ModbusErrorCodeException(ModbusErrorCode code) : base(code.GetErrorDescription())
        {
            ErrorCode = code;
        }

        /// <summary>
        /// 错误码
        /// </summary>
        public ModbusErrorCode ErrorCode { get; }
    }
}
