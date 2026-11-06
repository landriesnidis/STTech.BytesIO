using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace STTech.BytesIO.Mqtt
{
    /// <summary>
    /// MQTT 主题通配符匹配与消息路由分发引擎。
    /// 支持单层通配符 '+' 与多层通配符 '#'。
    /// </summary>
    public sealed class TopicRouter
    {
        private sealed class RouteEntry
        {
            public MqttQualityOfServiceLevel QualityOfService { get; set; }
            public List<Func<MqttApplicationMessage, Task>> Handlers { get; } = new List<Func<MqttApplicationMessage, Task>>();
        }

        private readonly ConcurrentDictionary<string, RouteEntry> _routes =
            new ConcurrentDictionary<string, RouteEntry>(StringComparer.Ordinal);

        /// <summary>
        /// 添加或更新主题路由处理器与服务质量等级。
        /// </summary>
        /// <param name="topicFilter">主题过滤器</param>
        /// <param name="handler">消息处理委托，可为 null（仅记录主题与 QoS 订阅关系）</param>
        /// <param name="qos">订阅的服务质量等级</param>
        public void AddRoute(string topicFilter, Func<MqttApplicationMessage, Task> handler, MqttQualityOfServiceLevel qos = MqttQualityOfServiceLevel.AtMostOnce)
        {
            if (string.IsNullOrEmpty(topicFilter)) throw new ArgumentNullException(nameof(topicFilter));

            _routes.AddOrUpdate(topicFilter,
                filter =>
                {
                    var entry = new RouteEntry { QualityOfService = qos };
                    if (handler != null)
                    {
                        entry.Handlers.Add(handler);
                    }
                    return entry;
                },
                (filter, entry) =>
                {
                    lock (entry.Handlers)
                    {
                        if (qos > entry.QualityOfService)
                        {
                            entry.QualityOfService = qos;
                        }
                        if (handler != null && !entry.Handlers.Contains(handler))
                        {
                            entry.Handlers.Add(handler);
                        }
                    }
                    return entry;
                });
        }

        /// <summary>
        /// 添加或更新主题路由处理器（默认 QoS 0）。
        /// </summary>
        public void AddRoute(string topicFilter, Func<MqttApplicationMessage, Task> handler)
        {
            AddRoute(topicFilter, handler, MqttQualityOfServiceLevel.AtMostOnce);
        }

        /// <summary>
        /// 移除指定主题路由。
        /// </summary>
        public bool RemoveRoute(string topicFilter)
        {
            if (string.IsNullOrEmpty(topicFilter)) return false;
            return _routes.TryRemove(topicFilter, out _);
        }

        /// <summary>
        /// 清空所有注册的主题路由。
        /// </summary>
        public void Clear()
        {
            _routes.Clear();
        }

        /// <summary>
        /// 获取所有已注册的主题过滤器列表。
        /// </summary>
        public IReadOnlyCollection<string> GetSubscribedTopicFilters()
        {
            return new List<string>(_routes.Keys);
        }

        /// <summary>
        /// 获取所有已注册的主题过滤器与其对应的 QoS 服务质量等级字典。
        /// </summary>
        public IReadOnlyDictionary<string, MqttQualityOfServiceLevel> GetSubscribedTopicsWithQoS()
        {
            var dict = new Dictionary<string, MqttQualityOfServiceLevel>(_routes.Count, StringComparer.Ordinal);
            foreach (var kvp in _routes)
            {
                dict[kvp.Key] = kvp.Value.QualityOfService;
            }
            return dict;
        }

        /// <summary>
        /// 检索与目标主题匹配的所有处理委托。
        /// </summary>
        public List<Func<MqttApplicationMessage, Task>> MatchHandlers(string topic)
        {
            var matches = new List<Func<MqttApplicationMessage, Task>>();
            if (string.IsNullOrEmpty(topic)) return matches;

            foreach (var kvp in _routes)
            {
                if (IsTopicMatch(topic, kvp.Key))
                {
                    lock (kvp.Value.Handlers)
                    {
                        matches.AddRange(kvp.Value.Handlers);
                    }
                }
            }

            return matches;
        }

        /// <summary>
        /// 判断发布主题是否符合主题过滤器规范。
        /// </summary>
        public static bool IsTopicMatch(string topic, string topicFilter)
        {
            if (string.Equals(topic, topicFilter, StringComparison.Ordinal))
            {
                return true;
            }

            if (topicFilter == "#")
            {
                return !topic.StartsWith("$");
            }

            if (topic.StartsWith("$") != topicFilter.StartsWith("$"))
            {
                return false;
            }

            string[] topicSegments = topic.Split('/');
            string[] filterSegments = topicFilter.Split('/');

            for (int i = 0; i < filterSegments.Length; i++)
            {
                string filterSeg = filterSegments[i];

                if (filterSeg == "#")
                {
                    // '#' 必须是最后一个段，匹配后续所有段
                    return i == filterSegments.Length - 1;
                }

                if (i >= topicSegments.Length)
                {
                    return false;
                }

                string topicSeg = topicSegments[i];
                if (filterSeg != "+" && !string.Equals(filterSeg, topicSeg, StringComparison.Ordinal))
                {
                    return false;
                }
            }

            return filterSegments.Length == topicSegments.Length;
        }
    }
}
