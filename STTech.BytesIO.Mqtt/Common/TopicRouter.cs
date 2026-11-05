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
        private readonly ConcurrentDictionary<string, List<Func<MqttApplicationMessage, Task>>> _routes =
            new ConcurrentDictionary<string, List<Func<MqttApplicationMessage, Task>>>(StringComparer.Ordinal);

        /// <summary>
        /// 添加或更新主题路由处理器。
        /// </summary>
        public void AddRoute(string topicFilter, Func<MqttApplicationMessage, Task> handler)
        {
            if (string.IsNullOrEmpty(topicFilter)) throw new ArgumentNullException(nameof(topicFilter));
            if (handler == null) return;

            _routes.AddOrUpdate(topicFilter,
                filter => new List<Func<MqttApplicationMessage, Task>> { handler },
                (filter, list) =>
                {
                    lock (list)
                    {
                        list.Add(handler);
                    }
                    return list;
                });
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
                    lock (kvp.Value)
                    {
                        matches.AddRange(kvp.Value);
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
