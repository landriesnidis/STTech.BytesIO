using System;

namespace STTech.BytesIO.Core
{
    public class ExceptionOccursEventArgs : EventArgs
    {
        public Exception Exception { get; }
        public ExceptionOccursEventArgs(Exception ex)
        {
            Exception = ex;
        }
    }
}
