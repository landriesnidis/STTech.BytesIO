using System;
using System.IO;
using System.Text;

namespace STTech.BytesIO.Mqtt
{
    /// <summary>
    /// MQTT 二进制协议序列化写入器。
    /// </summary>
    public sealed class MqttBinaryWriter : IDisposable
    {
        private readonly MemoryStream _stream;

        public long Length => _stream.Length;
        public long Position { get => _stream.Position; set => _stream.Position = value; }

        public MqttBinaryWriter()
        {
            _stream = new MemoryStream();
        }

        public MqttBinaryWriter(int capacity)
        {
            _stream = new MemoryStream(capacity);
        }

        public void WriteByte(byte value)
        {
            _stream.WriteByte(value);
        }

        public void WriteTwoByteInteger(ushort value)
        {
            _stream.WriteByte((byte)(value >> 8));
            _stream.WriteByte((byte)(value & 0xFF));
        }

        public void WriteFourByteInteger(uint value)
        {
            _stream.WriteByte((byte)((value >> 24) & 0xFF));
            _stream.WriteByte((byte)((value >> 16) & 0xFF));
            _stream.WriteByte((byte)((value >> 8) & 0xFF));
            _stream.WriteByte((byte)(value & 0xFF));
        }

        public void WriteVariableByteInteger(int value)
        {
            if (value < 0 || value > 0x0FFFFFFF)
            {
                throw new ArgumentOutOfRangeException(nameof(value), "Variable byte integer must be between 0 and 268,435,455.");
            }

            do
            {
                byte encodedByte = (byte)(value % 128);
                value /= 128;
                if (value > 0)
                {
                    encodedByte |= 0x80;
                }
                _stream.WriteByte(encodedByte);
            } while (value > 0);
        }

        public void WriteString(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                WriteTwoByteInteger(0);
                return;
            }

            byte[] bytes = Encoding.UTF8.GetBytes(value);
            if (bytes.Length > ushort.MaxValue)
            {
                throw new ArgumentException("String length exceeds maximum 65535 bytes.", nameof(value));
            }

            WriteTwoByteInteger((ushort)bytes.Length);
            _stream.Write(bytes, 0, bytes.Length);
        }

        public void WriteBinaryData(byte[] data)
        {
            if (data == null || data.Length == 0)
            {
                WriteTwoByteInteger(0);
                return;
            }

            if (data.Length > ushort.MaxValue)
            {
                throw new ArgumentException("Binary data length exceeds maximum 65535 bytes.", nameof(data));
            }

            WriteTwoByteInteger((ushort)data.Length);
            _stream.Write(data, 0, data.Length);
        }

        public void WriteRawBytes(byte[] data)
        {
            if (data != null && data.Length > 0)
            {
                _stream.Write(data, 0, data.Length);
            }
        }

        public void WriteRawBytes(byte[] data, int offset, int count)
        {
            if (data != null && count > 0)
            {
                _stream.Write(data, offset, count);
            }
        }

        public byte[] ToArray()
        {
            return _stream.ToArray();
        }

        public void Dispose()
        {
            _stream.Dispose();
        }
    }
}
