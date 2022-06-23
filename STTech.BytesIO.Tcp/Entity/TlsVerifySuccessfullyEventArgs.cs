using System;
using System.Collections.Generic;
using System.Net.Security;
using System.Security.Authentication;
using System.Text;

namespace STTech.BytesIO.Tcp.Entity
{
    public class TlsVerifySuccessfullyEventArgs : EventArgs
    {
        public CipherAlgorithmType CipherAlgorithm { get; }
        public int CipherStrength { get; }
        public HashAlgorithmType HashAlgorithm { get; }
        public int HashStrength { get; }
        public ExchangeAlgorithmType KeyExchangeAlgorithm { get; }
        public int KeyExchangeStrength { get; }
        public SslProtocols SslProtocol { get; }

        public TlsVerifySuccessfullyEventArgs(SslStream stream)
        {
            CipherAlgorithm = stream.CipherAlgorithm;
            CipherStrength = stream.CipherStrength;
            HashAlgorithm = stream.HashAlgorithm;
            HashStrength = stream.HashStrength;
            KeyExchangeAlgorithm = stream.KeyExchangeAlgorithm;
            KeyExchangeStrength = stream.KeyExchangeStrength;
            SslProtocol = stream.SslProtocol;
        }
    }
}
