using System;
using System.Collections.Generic;
using System.Net.Security;
using System.Security.Authentication;
using System.Text;

namespace STTech.BytesIO.Tcp.Entity
{
    public class TlsVerifySuccessfullyEventArgs : EventArgs
    {
        /// <summary>
        /// 批量加密算法
        /// </summary>
        public CipherAlgorithmType CipherAlgorithm { get; }

        /// <summary>
        /// 密码算法的强度
        /// </summary>
        public int CipherStrength { get; }

        /// <summary>
        /// 获取用于生成消息身份验证代码 (MAC) 的算法
        /// </summary>
        public HashAlgorithmType HashAlgorithm { get; }

        /// <summary>
        /// 获取此实例使用的哈希算法的强度
        /// </summary>
        public int HashStrength { get; }

        /// <summary>
        /// 密钥交换算法
        /// </summary>
        public ExchangeAlgorithmType KeyExchangeAlgorithm { get; }

        /// <summary>
        /// 获此实例使用的密钥交换算法的强度
        /// </summary>
        public int KeyExchangeStrength { get; }

        /// <summary>
        /// 获取此连接进行身份验证的安全协议
        /// </summary>
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
