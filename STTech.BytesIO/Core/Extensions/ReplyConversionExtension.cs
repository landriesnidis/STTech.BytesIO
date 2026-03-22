using STTech.BytesIO.Core.Exceptions;
using System;

namespace STTech.BytesIO.Core
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
        /// <param name="reply"></param>
        /// <param name="convertHandler">转换实现回调</param>
        /// <returns></returns>
        /// <exception cref="ReplyConversionException"></exception>
        public static Reply<TOut> ConvertTo<TOut>(this Reply reply, Func<Response, TOut> convertHandler)
            where TOut : Response
        {
            Reply<TOut> newReply;

            if (reply.Status == ReplyStatus.Completed)
            {
                TOut resp = convertHandler.Invoke(reply.GetResponse());
                newReply = new Reply<TOut>(reply.Client, resp);
            }
            else
            {
                newReply = new Reply<TOut>(reply.Client, reply.Status, reply.Exception);
            }
            return newReply;
        }

        /// <summary>
        /// Reply内数据响应类型转格式
        /// </summary>
        /// <typeparam name="TOut">输出类型</typeparam>
        /// <param name="reply"></param>
        /// <returns></returns>
        public static Reply<TOut> ConvertTo<TOut>(this Reply reply)
            where TOut : Response
        {
            return reply.ConvertTo(respIn => {
                var bytes = reply.GetResponse().GetOriginalData();
                var respOut = (TOut)Activator.CreateInstance(typeof(TOut), bytes);
                return respOut;
            });

        }

        /// <summary>
        /// 直接强制类型转换（低分配），适用于解包器已构造了正确目标类型的场景
        /// </summary>
        /// <typeparam name="TOut">输出类型</typeparam>
        /// <param name="reply"></param>
        /// <returns></returns>
        public static Reply<TOut> CastTo<TOut>(this Reply reply) where TOut : Response
        {
            if (reply.Status == ReplyStatus.Completed)
            {
                var resp = reply.GetResponse();
                if (resp is TOut typed)
                    return new Reply<TOut>(reply.Client, typed);
                return reply.ConvertTo<TOut>();
            }
            return new Reply<TOut>(reply.Client, reply.Status, reply.Exception);
        }
    }
}
