using System;
using System.Text;

namespace STTech.BytesIO.Mqtt
{
    /// <summary>
    /// MQTT 二进制协议反序列化读取器。
    /// </summary>
    public sealed class MqttBinaryReader
    {
        private readonly ReadOnlyMemory<byte> _buffer;
        private int _position;

        public int Position
        {
            get => _position;
            set
            {
                if (value < 0 || value > _buffer.Length)
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }
                _position = value;
            }
        }

        public int BytesRemaining => _buffer.Length - _position;
        public int Length => _buffer.Length;

        public MqttBinaryReader(byte[] buffer) : this(buffer != null ? new ReadOnlyMemory<byte>(buffer) : ReadOnlyMemory<byte>.Empty)
        {
        }

        public MqttBinaryReader(byte[] buffer, int offset, int count) : this(new ReadOnlyMemory<byte>(buffer, offset, count))
        {
        }

        public MqttBinaryReader(ReadOnlyMemory<byte> buffer)
        {
            _buffer = buffer;
            _position = 0;
        }

        public byte ReadByte()
        {
            if (BytesRemaining < 1)
            {
                throw new MqttProtocolViolationException("读取数据不足：期望读取 1 字节。");
            }
            return _buffer.Span[_position++];
        }

        public ushort ReadTwoByteInteger()
        {
            if (BytesRemaining < 2)
            {
                throw new MqttProtocolViolationException("读取数据不足：期望读取 2 字节无符号整数。");
            }

            var span = _buffer.Span;
            byte b1 = span[_position++];
            byte b2 = span[_position++];
            return (ushort)((b1 << 8) | b2);
        }

        public uint ReadFourByteInteger()
        {
            if (BytesRemaining < 4)
            {
                throw new MqttProtocolViolationException("读取数据不足：期望读取 4 字节无符号整数。");
            }

            var span = _buffer.Span;
            byte b1 = span[_position++];
            byte b2 = span[_position++];
            byte b3 = span[_position++];
            byte b4 = span[_position++];
            return (uint)((b1 << 24) | (b2 << 16) | (b3 << 8) | b4);
        }

        public int ReadVariableByteInteger()
        {
            int multiplier = 1;
            int value = 0;
            int count = 0;

            while (count < 4)
            {
                if (BytesRemaining < 1)
                {
                    throw new MqttProtocolViolationException("读取数据不足：可变字节整数不完整。");
                }

                byte encodedByte = _buffer.Span[_position++];
                count++;
                value += (encodedByte & 0x7F) * multiplier;
                multiplier *= 128;

                if ((encodedByte & 0x80) == 0)
                {
                    return value;
                }
            }

            throw new MqttProtocolViolationException("MQTT 可变字节整数超出 4 字节限制。");
        }

        public string ReadString()
        {
            ushort length = ReadTwoByteInteger();
            if (length == 0)
            {
                return string.Empty;
            }

            if (BytesRemaining < length)
            {
                throw new MqttProtocolViolationException($"读取数据不足：期望读取 {length} 字节 UTF-8 字符串。");
            }

            byte[] bytes = _buffer.Slice(_position, length).ToArray();
            _position += length;
            return Encoding.UTF8.GetString(bytes);
        }

        public byte[] ReadBinaryData()
        {
            ushort length = ReadTwoByteInteger();
            if (length == 0)
            {
                return new byte[0];
            }

            if (BytesRemaining < length)
            {
                throw new MqttProtocolViolationException($"读取数据不足：期望读取 {length} 字节二进制数据。");
            }

            byte[] bytes = _buffer.Slice(_position, length).ToArray();
            _position += length;
            return bytes;
        }

        public byte[] ReadRemainingBytes()
        {
            if (BytesRemaining <= 0)
            {
                return new byte[0];
            }

            byte[] bytes = _buffer.Slice(_position, BytesRemaining).ToArray();
            _position = _buffer.Length;
            return bytes;
        }

        public MqttBinaryReader Slice(int length)
        {
            if (length < 0 || length > BytesRemaining)
            {
                throw new ArgumentOutOfRangeException(nameof(length));
            }

            var subReader = new MqttBinaryReader(_buffer.Slice(_position, length));
            _position += length;
            return subReader;
        }
    }
}
