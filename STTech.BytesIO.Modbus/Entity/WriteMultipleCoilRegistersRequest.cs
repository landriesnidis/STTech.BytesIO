using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace STTech.BytesIO.Modbus
{
    /// <summary>
    /// 写多个线圈寄存器请求
    /// </summary>
    public class WriteMultipleCoilRegistersRequest : ModbusRequest
    {
        /// <summary>
        /// 写入起始地址
        /// </summary>
        [Description("写入地址")]
        public ushort WriteAddress { get; set; }

        /// <summary>
        /// 写入数据列表
        /// </summary>
        [Browsable(false)]
        [Description("写入数据")]
        public bool[] Data { get; set; } = [false];

        /// <summary>
        /// 构造写多个线圈寄存器请求
        /// </summary>
        public WriteMultipleCoilRegistersRequest() : base(FunctionCode.WriteMultipleCoilRegisters)
        {

        }

        /// <summary>
        /// 构造写多个线圈寄存器请求 (基于字节数组)
        /// </summary>
        /// <param name="data">原始报文数据</param>
        public WriteMultipleCoilRegistersRequest(byte[] data) : base(data)
        {
            DeserializePayloadHandle();
        }

        /// <summary>
        /// 构造写多个线圈寄存器请求 (基于解包上下文)
        /// </summary>
        /// <param name="context">解包上下文</param>
        public WriteMultipleCoilRegistersRequest(STTech.BytesIO.Core.Component.UnpackContext context) : base(context)
        {
            DeserializePayloadHandle();
        }

        /// <inheritdoc/>
        protected internal override void SerializePayloadHandle()
        {
            List<byte> bytes = new List<byte>();
            bytes.AddRange(Enumerable.Reverse(BitConverter.GetBytes(WriteAddress)));
            bytes.AddRange(Enumerable.Reverse(BitConverter.GetBytes((ushort)Data.Length)));

            var bytesArray = Data.Slice(8);
            var bits = bytesArray.Select(arr =>
            {
                int b = 0;
                foreach (var c in Enumerable.Reverse(arr))
                {
                    b = b << 1;
                    if (c) b++;
                }
                return (byte)b;
            });

            bytes.Add((byte)bits.Count());
            bytes.AddRange(bits);
            Payload = bytes.ToArray();
        }

        /// <inheritdoc/>
        protected internal override void DeserializePayloadHandle()
        {
            if (Payload == null || Payload.Length < 5) return;
            WriteAddress = BitConverter.ToUInt16(Payload.Take(2).Reverse().ToArray(), 0);
            var quantity = BitConverter.ToUInt16(Payload.Skip(2).Take(2).Reverse().ToArray(), 0);
            var byteCount = Payload[4];

            if (Payload.Length >= 5 + byteCount)
            {
                var bytesArray = Payload.Skip(5).Take(byteCount).ToArray();
                var boolList = new List<bool>();
                
                foreach (var b in bytesArray)
                {
                    for (int i = 0; i < 8; i++)
                    {
                        if (boolList.Count >= quantity) break;
                        boolList.Add((b & (1 << i)) != 0);
                    }
                }
                Data = boolList.ToArray();
            }
        }
    }
}
