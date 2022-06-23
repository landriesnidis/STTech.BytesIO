using System;
using System.Collections.Generic;
using System.Text;

namespace STTech.BytesIO.Core.Exceptions
{
    public class PerformCallbackException : Exception
    {
        public PerformCallbackException(Exception ex):base($"An exception occurred while the callback method was executing. {ex.Message}", ex)
        {
        }
    }
}
