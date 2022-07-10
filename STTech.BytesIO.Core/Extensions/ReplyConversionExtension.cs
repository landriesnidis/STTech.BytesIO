using STTech.BytesIO.Core.Exceptions;
using System;

namespace STTech.BytesIO.Core.Entity
{
    /// <summary>
    /// 同步通信结果(Reply)类型转换
    /// </summary>
    public static class ReplyConversionExtension
    {
        /// <summary>
        /// Reply内数据响应类型转格式
        /// </summary>
        /// <typeparam name="TOut">输出类型</typeparam>
        /// <typeparam name="TIn">当前类型</typeparam>
        /// <param name="reply"></param>
        /// <param name="convertHandler">转换实现回调</param>
        /// <returns></returns>
        /// <exception cref="ReplyConversionException"></exception>
        public static Reply<TOut> ConvertTo<TOut, TIn>(this Reply<TIn> reply, Func<TIn, TOut> convertHandler = null)
            where TOut : Response
            where TIn : Response
        {
            Reply<TOut> newReply;

            if (reply.Status == ReplyStatus.Completed)
            {
                var bytes = reply.Data.OriginalData;
                TOut resp;
                if (convertHandler == null)
                {
                    try
                    {
                        resp = (TOut)Activator.CreateInstance(typeof(TOut), bytes);
                    }
                    catch (MissingMethodException ex)
                    {
                        throw new ReplyConversionException($"Target type({typeof(TOut).FullName}) of the conversion should contain a constructor of `ctor(IEnumerable<byte> bytes)`.", ex);
                    }
                }
                else
                {
                    resp = convertHandler.Invoke(reply.Data);
                }
                newReply = new Reply<TOut>(reply.Client, reply.Status, resp);
            }
            else
            {
                newReply = new Reply<TOut>(reply.Client, reply.Status, reply.Exception);
            }

            return newReply;
        }
    }
}
