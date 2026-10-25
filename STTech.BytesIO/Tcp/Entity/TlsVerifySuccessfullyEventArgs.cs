using System;
using System.Collections.Generic;
using System.Net.Security;
using System.Security.Authentication;
using System.Text;

namespace STTech.BytesIO.Tcp.Entity
{
    /// <summary>
    /// TLS 通信验证成功事件参数
    /// </summary>
    public class TlsVerifySuccessfullyEventArgs : EventArgs
    {
        /// <summary>
        /// 批量加密算法
        /// </summary>
#if NET10_0_OR_GREATER
        [Obsolete("KeyExchangeAlgorithm, KeyExchangeStrength, CipherAlgorithm, CipherStrength, HashAlgorithm and HashStrength properties of SslStream are obsolete. Use NegotiatedCipherSuite instead.")]
#endif
        public CipherAlgorithmType CipherAlgorithm { get; }

        /// <summary>
        /// 密码算法的强度
        /// </summary>
#if NET10_0_OR_GREATER
        [Obsolete("KeyExchangeAlgorithm, KeyExchangeStrength, CipherAlgorithm, CipherStrength, HashAlgorithm and HashStrength properties of SslStream are obsolete. Use NegotiatedCipherSuite instead.")]
#endif
        public int CipherStrength { get; }

        /// <summary>
        /// 获取用于生成消息身份验证代码 (MAC) 的算法
        /// </summary>
#if NET10_0_OR_GREATER
        [Obsolete("KeyExchangeAlgorithm, KeyExchangeStrength, CipherAlgorithm, CipherStrength, HashAlgorithm and HashStrength properties of SslStream are obsolete. Use NegotiatedCipherSuite instead.")]
#endif
        public HashAlgorithmType HashAlgorithm { get; }

        /// <summary>
        /// 获取此实例使用的哈希算法的强度
        /// </summary>
#if NET10_0_OR_GREATER
        [Obsolete("KeyExchangeAlgorithm, KeyExchangeStrength, CipherAlgorithm, CipherStrength, HashAlgorithm and HashStrength properties of SslStream are obsolete. Use NegotiatedCipherSuite instead.")]
#endif
        public int HashStrength { get; }

        /// <summary>
        /// 密钥交换算法
        /// </summary>
#if NET10_0_OR_GREATER
        [Obsolete("KeyExchangeAlgorithm, KeyExchangeStrength, CipherAlgorithm, CipherStrength, HashAlgorithm and HashStrength properties of SslStream are obsolete. Use NegotiatedCipherSuite instead.")]
#endif
        public ExchangeAlgorithmType KeyExchangeAlgorithm { get; }

        /// <summary>
        /// 获此实例使用的密钥交换算法的强度
        /// </summary>
#if NET10_0_OR_GREATER
        [Obsolete("KeyExchangeAlgorithm, KeyExchangeStrength, CipherAlgorithm, CipherStrength, HashAlgorithm and HashStrength properties of SslStream are obsolete. Use NegotiatedCipherSuite instead.")]
#endif
        public int KeyExchangeStrength { get; }

        /// <summary>
        /// 获取此连接进行身份验证的安全协议
        /// </summary>
        public SslProtocols SslProtocol { get; }

#if NETCOREAPP3_0_OR_GREATER || NET5_0_OR_GREATER
        /// <summary>
        /// 获取此连接协商的密码套件
        /// </summary>
        public TlsCipherSuite NegotiatedCipherSuite { get; }
#endif

        /// <summary>
        /// 构造 TLS 通信验证成功事件参数
        /// </summary>
        /// <param name="stream">SSL 通信流</param>
        public TlsVerifySuccessfullyEventArgs(SslStream stream)
        {
#pragma warning disable CS0618, SYSLIB0058
            CipherAlgorithm = stream.CipherAlgorithm;
            CipherStrength = stream.CipherStrength;
            HashAlgorithm = stream.HashAlgorithm;
            HashStrength = stream.HashStrength;
            KeyExchangeAlgorithm = stream.KeyExchangeAlgorithm;
            KeyExchangeStrength = stream.KeyExchangeStrength;
#pragma warning restore CS0618, SYSLIB0058
            SslProtocol = stream.SslProtocol;
#if NETCOREAPP3_0_OR_GREATER || NET5_0_OR_GREATER
            NegotiatedCipherSuite = stream.NegotiatedCipherSuite;
#endif
        }
    }
}
