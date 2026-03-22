using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace STTech.BytesIO.Modbus
{
    /// <summary>
    /// 读离散输入寄存器请求
    /// </summary>
    public class ReadDiscreteInputRegisterRequest : ReadRegisterRequest
    {
        /// <summary>
        /// 构造读离散输入寄存器请求
        /// </summary>
        public ReadDiscreteInputRegisterRequest() : base(FunctionCode.ReadDiscreteInputRegister)
        {

        }
    }
}
