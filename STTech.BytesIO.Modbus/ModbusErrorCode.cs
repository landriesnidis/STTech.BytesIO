using System;
using System.ComponentModel;
using System.Reflection;

namespace STTech.BytesIO.Modbus
{
    /// <summary>
    /// Modbus错误码
    /// </summary>
    public enum ModbusErrorCode
    {
        /// <summary> 
        /// 没有错误<br/> 
        /// Default value to indicate no error 
        /// </summary> 
        [Description("没有错误 | Default value to indicate no error")]
        NoError = 0x00,

        /// <summary>
        /// 非法功能码<br/>
        /// The function code received in the query is not recognized or allowed by the device
        /// </summary>
        [Description("非法功能码 | The function code received in the query is not recognized or allowed by the device")]
        IllegalFunction = 0x01,

        /// <summary>
        /// 非法数据地址<br/>
        /// The data address received in the query is not allowed by the device
        /// </summary>
        [Description("非法数据地址 | The data address received in the query is not allowed by the device")]
        IllegalDataAddress = 0x02,

        /// <summary>
        /// 非法数据值<br/>
        /// A value contained in the query data field is not an allowable value for the device
        /// </summary>
        [Description("非法数据值 | A value contained in the query data field is not an allowable value for the device")]
        IllegalDataValue = 0x03,

        /// <summary>
        /// 从设备故障<br/>
        /// An unrecoverable error occurred while the slave was attempting to perform the requested action
        /// </summary>
        [Description("从设备故障 | An unrecoverable error occurred while the slave was attempting to perform the requested action")]
        SlaveDeviceFailure = 0x04,

        /// <summary>
        /// 应答<br/>
        /// Specialized use in conjunction with programming commands. The device has accepted the request and is processing it
        /// </summary>
        [Description("应答 | Specialized use in conjunction with programming commands. The device has accepted the request and is processing it")]
        Acknowledge = 0x05,

        /// <summary>
        /// 从设备忙<br/>
        /// 设备已经接收到请求，但需要更多时间来完成请求。
        /// 这个错误码通常在从设备（例如传感器或执行器）因为一些原因无法立即响应主站设备（如控制器或监视器）时产生。
        /// 当主站设备发送请求并收到Acknowledge错误码时，它意识到从设备已经收到请求并正在处理，但需要额外的时间。
        /// 这通常发生在从设备需要进行一些计算或处理数据的情况下，或者如果从设备正在处理其他请求时。
        /// 主站设备通常会等待一段时间，然后重新发送请求，或者根据具体的情况采取其他操作。<br/>
        /// The device is engaged in processing a long-duration program command
        /// </summary>
        [Description("从设备忙 | The device is engaged in processing a long-duration program command")]
        SlaveDeviceBusy = 0x06,

        /// <summary>
        /// 存储器奇偶校验错<br/>
        /// The parity of the memory module is not consistent with the memory module state
        /// </summary>
        [Description("存储器奇偶校验错 | The parity of the memory module is not consistent with the memory module state")]
        MemoryParityError = 0x08,

        /// <summary>
        /// 网关路径不可用<br/>
        /// Specialized for Modbus gateways. Indicates a misconfigured gateway
        /// </summary>
        [Description("网关路径不可用 | Specialized for Modbus gateways. Indicates a misconfigured gateway")]
        GatewayPathUnavailable = 0x0A,

        /// <summary>
        /// 网关目标设备未响应<br/>
        /// Specialized for Modbus gateways. No response was received from the target device
        /// </summary>
        [Description("网关目标设备未响应 | Specialized for Modbus gateways. No response was received from the target device")]
        GatewayTargetDeviceFailedToRespond = 0x0B,
    }

    /// <summary>
    /// Modbus 错误码扩展方法
    /// </summary>
    public static class ModbusErrorCodeExtensions
    {
        /// <summary>
        /// 获取错误码的描述信息
        /// </summary>
        /// <param name="code">错误码</param>
        /// <returns>描述字符串</returns>
        public static string GetErrorDescription(this ModbusErrorCode code)
        {
            Type enumType = typeof(ModbusErrorCode);
            FieldInfo fieldInfo = enumType.GetField(code.ToString());
            DescriptionAttribute attr = fieldInfo.GetCustomAttribute<DescriptionAttribute>();

            return attr == null ? string.Empty : attr.Description;
        }
    }
}
