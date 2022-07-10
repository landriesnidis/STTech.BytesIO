using System;
using System.Collections.Generic;
using System.Text;

namespace STTech.BytesIO.Core.Entity
{
    public abstract class Response
    {
        public IEnumerable<byte> OriginalData { get; }

        protected Response(IEnumerable<byte> bytes)
        {
            OriginalData = bytes;
        }
    }

    public interface IRequest
    {
        byte[] GetBytes();
    }
}
