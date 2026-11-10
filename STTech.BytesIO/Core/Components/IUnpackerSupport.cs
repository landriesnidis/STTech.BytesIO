using STTech.BytesIO.Core;

namespace STTech.BytesIO.Core.Component
{
    /// <summary>
    /// 提供解包器支持的接口
    /// </summary>
    /// <typeparam name="T">响应实体类型</typeparam>
    public interface IUnpackerSupport<T> where T : Response
    {
        /// <summary>
        /// 解包器
        /// </summary>
        Unpacker<T> Unpacker { get; }
    }
}
