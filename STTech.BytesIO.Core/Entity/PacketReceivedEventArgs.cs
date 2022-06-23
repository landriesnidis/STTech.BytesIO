using System;
using System.Text;

namespace STTech.BytesIO.Core.Entity
{

    public class PacketReceivedEventArgs<T> : EventArgs
    {
        public T Data { get; }

        public PacketReceivedEventArgs(T data)
        {
            Data = data;
        }
    }
}
