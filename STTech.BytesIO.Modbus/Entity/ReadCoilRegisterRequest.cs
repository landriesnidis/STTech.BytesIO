using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace STTech.BytesIO.Modbus
{
    /// <summary>
    /// 读线圈寄存器请求
    /// </summary>
    public class ReadCoilRegisterRequest : ReadRegisterRequest
    {
        /// <summary>
        /// 构造读线圈寄存器请求
        /// </summary>
        public ReadCoilRegisterRequest() : base(FunctionCode.ReadCoilRegister)
        {
        }
    }

}
