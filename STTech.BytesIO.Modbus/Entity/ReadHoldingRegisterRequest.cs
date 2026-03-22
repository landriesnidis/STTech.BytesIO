using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace STTech.BytesIO.Modbus
{
    /// <summary>
    /// 读保持寄存器请求
    /// </summary>
    public class ReadHoldingRegisterRequest: ReadRegisterRequest
    {
        /// <summary>
        /// 构造读保持寄存器请求
        /// </summary>
        public ReadHoldingRegisterRequest() : base(FunctionCode.ReadHoldingRegister)
        {

        }
    }
}
