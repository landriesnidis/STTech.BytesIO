using System;
using System.Collections.Generic;
using System.Text;

namespace STTech.BytesIO.Core.Entity
{
    public abstract class Response
    {
        protected Response(IEnumerable<byte> bytes) { }
    }

    public interface IRequest
    {
        byte[] GetBytes();
    }
}
