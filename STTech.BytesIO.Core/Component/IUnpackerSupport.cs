using STTech.BytesIO.Core;

namespace STTech.BytesIO.Core.Component
{
    public interface IUnpackerSupport<T> where T : Response
    {
        /// <summary>
        /// 解包器
        /// </summary>
        Unpacker<T> Unpacker { get; }
    }
}
