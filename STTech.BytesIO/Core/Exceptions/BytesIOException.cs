using System;

namespace STTech.BytesIO.Core.Exceptions
{
    public class BytesIOException : Exception
    {
        public BytesIOException(string errorMessage,Exception ex) : base(errorMessage, ex) { }
    }
}
