using System;
using System.Collections.Generic;
using System.Threading;

namespace STTech.BytesIO.Mqtt
{
    /// <summary>
    /// 线程安全的 MQTT 报文标识符 (Packet Identifier) 管理器。
    /// 报文标识符范围为 1 ~ 65535。
    /// </summary>
    internal sealed class PacketIdManager
    {
        private int _currentId = 0;
        private readonly HashSet<ushort> _inUseIds = new HashSet<ushort>();
        private readonly object _lock = new object();

        /// <summary>
        /// 获取下一个可用的报文标识符。
        /// </summary>
        public ushort GetNextPacketId()
        {
            lock (_lock)
            {
                for (int i = 0; i < ushort.MaxValue; i++)
                {
                    int next = Interlocked.Increment(ref _currentId);
                    if (next > ushort.MaxValue || next <= 0)
                    {
                        Interlocked.Exchange(ref _currentId, 1);
                        next = 1;
                    }

                    ushort id = (ushort)next;
                    if (!_inUseIds.Contains(id))
                    {
                        _inUseIds.Add(id);
                        return id;
                    }
                }

                throw new MqttCommunicationException("可用的 MQTT 报文标识符已耗尽 (1~65535 全部在用)。");
            }
        }

        /// <summary>
        /// 释放已完成的报文标识符。
        /// </summary>
        public void ReleasePacketId(ushort packetId)
        {
            if (packetId == 0) return;
            lock (_lock)
            {
                _inUseIds.Remove(packetId);
            }
        }

        /// <summary>
        /// 重置所有正在使用的报文标识符。
        /// </summary>
        public void Reset()
        {
            lock (_lock)
            {
                _inUseIds.Clear();
                Interlocked.Exchange(ref _currentId, 0);
            }
        }
    }
}
