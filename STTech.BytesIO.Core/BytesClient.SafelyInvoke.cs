using STTech.BytesIO.Core.Entity;
using STTech.BytesIO.Core.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace STTech.BytesIO.Core
{
    // ===============================================================================
    // 
    //                                安全执行回调
    // 
    // ===============================================================================

    public abstract partial class BytesClient
    {
        /// <summary>
        /// 安全的执行委托回调(或被重写的方法)
        /// 捕捉回调中的异常，避免因回调代码中的异常导致通信中断
        /// </summary>
        /// <param name="action"></param>
        protected void SafelyInvokeCallback(Action action)
        {
            try { action.Invoke(); }
            catch (Exception ex) { RaiseExceptionOccurs(this, new ExceptionOccursEventArgs(new PerformCallbackException(ex))); }
        }
    }
}
