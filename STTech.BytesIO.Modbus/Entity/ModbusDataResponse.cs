using System;

namespace STTech.BytesIO.Modbus
{
    /// <summary>
    /// 包含强类型数值转换结果的 Modbus 响应实体
    /// </summary>
    /// <typeparam name="T">强类型数据 (如 float, int, uint, double, long, string 等)</typeparam>
    public class ModbusDataResponse<T> : ModbusResponse
    {
        /// <summary>
        /// 转换后的具体业务数值
        /// </summary>
        public T Value { get; }

        /// <summary>
        /// 原始底层保持寄存器响应
        /// </summary>
        public ReadHoldingRegisterResponse RawResponse { get; }

        public ModbusDataResponse(T value, ReadHoldingRegisterResponse raw) 
            : base(raw?.SlaveId ?? 0, raw?.FunctionCode ?? FunctionCode.ReadHoldingRegister, raw?.ProtocolFormat ?? ModbusProtocolFormat.RTU)
        {
            Value = value;
            RawResponse = raw;
            if (raw != null)
            {
                IsSuccess = raw.IsSuccess;
                ErrorCode = raw.ErrorCode;
                TransactionId = raw.TransactionId;
                ProtocolId = raw.ProtocolId;
            }
        }

        /// <summary>
        /// 隐式转换：直接转换为具体数值
        /// </summary>
        public static implicit operator T(ModbusDataResponse<T> response) => response != null ? response.Value : default;

        public override string ToString()
        {
            return Value?.ToString() ?? string.Empty;
        }
    }
}
