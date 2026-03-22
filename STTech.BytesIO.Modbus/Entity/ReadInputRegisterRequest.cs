using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace STTech.BytesIO.Modbus
{
    /// <summary>
    /// 读输入寄存器请求
    /// </summary>
    public class ReadInputRegisterRequest : ReadRegisterRequest
    {
        /// <summary>
        /// 构造读输入寄存器请求
        /// </summary>
        public ReadInputRegisterRequest() : base(FunctionCode.ReadInputRegister)
        {

        }
    }
}
