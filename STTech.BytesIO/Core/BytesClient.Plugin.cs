using System;
using System.Collections.Generic;
using System.Linq;

namespace STTech.BytesIO.Core
{
    // ===============================================================================
    // 
    //                                  插件管理
    // 
    // ===============================================================================

    public abstract partial class BytesClient
    {
        /// <summary>
        /// 已注册的插件列表
        /// </summary>
        private readonly List<BytesClientPlugin> _plugins = new List<BytesClientPlugin>();

        /// <summary>
        /// 插件操作同步锁
        /// </summary>
        private readonly object _pluginLock = new object();

        /// <summary>
        /// 添加插件
        /// </summary>
        /// <param name="plugin">插件实例</param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="InvalidOperationException">同一类型的插件已存在时抛出</exception>
        public void AddPlugin(BytesClientPlugin plugin)
        {
            if (plugin == null) throw new ArgumentNullException(nameof(plugin));

            lock (_pluginLock)
            {
                plugin.Client = this;
                _plugins.Add(plugin);
            }

            plugin.OnAttached();
        }

        /// <summary>
        /// 移除插件
        /// </summary>
        /// <param name="plugin">插件实例</param>
        /// <returns>是否成功移除</returns>
        public bool RemovePlugin(BytesClientPlugin plugin)
        {
            if (plugin == null) return false;

            bool removed;
            lock (_pluginLock)
            {
                removed = _plugins.Remove(plugin);
            }

            if (removed)
            {
                plugin.OnDetached();
                plugin.Client = null;
            }

            return removed;
        }

        /// <summary>
        /// 移除指定类型的插件
        /// </summary>
        /// <typeparam name="T">插件类型</typeparam>
        /// <returns>是否成功移除</returns>
        public bool RemovePlugin<T>() where T : BytesClientPlugin
        {
            var plugin = GetPlugin<T>();
            if (plugin != null)
            {
                return RemovePlugin(plugin);
            }
            return false;
        }

        /// <summary>
        /// 获取指定类型的插件
        /// </summary>
        /// <typeparam name="T">插件类型</typeparam>
        /// <returns>插件实例，不存在则返回 null</returns>
        public T GetPlugin<T>() where T : BytesClientPlugin
        {
            lock (_pluginLock)
            {
                return _plugins.OfType<T>().FirstOrDefault();
            }
        }

        /// <summary>
        /// 获取所有已注册的插件
        /// </summary>
        /// <returns>插件列表的只读快照</returns>
        public IReadOnlyList<BytesClientPlugin> GetPlugins()
        {
            lock (_pluginLock)
            {
                return _plugins.ToList().AsReadOnly();
            }
        }

        /// <summary>
        /// 通知所有已启用的插件
        /// </summary>
        internal void NotifyPlugins<TArgs>(Action<BytesClientPlugin, TArgs> callback, TArgs args)
        {
            BytesClientPlugin[] snapshot;
            lock (_pluginLock)
            {
                snapshot = _plugins.ToArray();
            }

            foreach (var plugin in snapshot)
            {
                if (plugin.IsEnabled)
                {
                    try
                    {
                        callback(plugin, args);
                    }
                    catch (Exception ex)
                    {
                        // 插件回调异常不应影响主流程
                        SafelyInvokeCallback(() => RaiseExceptionOccurs(this, new ExceptionOccursEventArgs(ex)));
                    }
                }
            }
        }
    }
}
