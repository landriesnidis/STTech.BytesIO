using System;
using System.Buffers.Binary;

namespace STTech.BytesIO.Ipc.Entity
{
    /// <summary>
    /// 共享内存 IPC 控制帧 (定长, 通过命名管道传输)
    /// 数据面走共享内存零拷贝, 管道仅传递消息元数据与确认信号
    /// </summary>
    internal struct ControlFrame
    {
        /// <summary>
        /// 控制帧固定大小 (字节)
        /// </summary>
        public const int FrameSize = 24;

        /// <summary>
        /// 帧魔术字 "ST"
        /// </summary>
        public const ushort Magic = 0x5354;

        /// <summary>
        /// 控制帧类型
        /// </summary>
        public enum FrameType : byte
        {
            /// <summary>
            /// 数据帧: 共享内存中 [DataOffset, DataOffset+Length) 为新数据
            /// </summary>
            Data = 1,

            /// <summary>
            /// 确认帧: 对端已完成消费, 槽位可复用
            /// </summary>
            Ack = 2,
        }

        /// <summary>
        /// 魔术字, 用于校验帧合法性
        /// </summary>
        public ushort MagicValue;

        /// <summary>
        /// 帧类型
        /// </summary>
        public byte Type;

        /// <summary>
        /// 保留字节
        /// </summary>
        public byte Reserved;

        /// <summary>
        /// 帧序号 (发送端递增)
        /// </summary>
        public int Sequence;

        /// <summary>
        /// 有效数据长度
        /// </summary>
        public long Length;

        /// <summary>
        /// 数据在共享内存中的起始偏移
        /// </summary>
        public long DataOffset;

        /// <summary>
        /// 将帧序列化到缓冲区
        /// </summary>
        public void WriteTo(byte[] buffer)
        {
            if (buffer == null) throw new ArgumentNullException(nameof(buffer));
            if (buffer.Length < FrameSize) throw new ArgumentOutOfRangeException(nameof(buffer), $"缓冲区大小必须不小于 {FrameSize} 字节");

            BinaryPrimitives.WriteUInt16LittleEndian(buffer.AsSpan(0, 2), MagicValue);
            buffer[2] = Type;
            buffer[3] = Reserved;
            BinaryPrimitives.WriteInt32LittleEndian(buffer.AsSpan(4, 4), Sequence);
            BinaryPrimitives.WriteInt64LittleEndian(buffer.AsSpan(8, 8), Length);
            BinaryPrimitives.WriteInt64LittleEndian(buffer.AsSpan(16, 8), DataOffset);
        }

        /// <summary>
        /// 从缓冲区解析帧
        /// </summary>
        public static ControlFrame ReadFrom(byte[] buffer)
        {
            if (buffer == null) throw new ArgumentNullException(nameof(buffer));
            if (buffer.Length < FrameSize) throw new ArgumentOutOfRangeException(nameof(buffer), $"缓冲区大小必须不小于 {FrameSize} 字节");

            return new ControlFrame
            {
                MagicValue = BinaryPrimitives.ReadUInt16LittleEndian(buffer.AsSpan(0, 2)),
                Type = buffer[2],
                Reserved = buffer[3],
                Sequence = BinaryPrimitives.ReadInt32LittleEndian(buffer.AsSpan(4, 4)),
                Length = BinaryPrimitives.ReadInt64LittleEndian(buffer.AsSpan(8, 8)),
                DataOffset = BinaryPrimitives.ReadInt64LittleEndian(buffer.AsSpan(16, 8)),
            };
        }
    }
}
