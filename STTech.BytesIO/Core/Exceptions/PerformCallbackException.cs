using System;
using System.Collections.Generic;
using System.Text;

namespace STTech.BytesIO.Core.Exceptions
{
    public class PerformCallbackException : BytesIOException
    {
        private const string errorMessage = "An exception occurred while the callback method was executing.";
        public PerformCallbackException(Exception ex) : base(errorMessage, ex) { }
    }
}
