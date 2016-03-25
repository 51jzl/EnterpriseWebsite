using Enyim.Caching;
using Enyim.Caching.Memcached;
using System;
using System.Collections.Generic;
using System.Web;

namespace Victornet.Caching
{
    /// <summary>
    /// 用于连接Memcached的分布式缓存
    /// </summary>
    public class MemcachedCache : ICache
    {
        private MemcachedClient cache = new MemcachedClient();

        /// <summary>
        /// 加入缓存项
        /// </summary>
        /// <param name="key">缓存项标识</param>
        /// <param name="value">缓存项</param>
        /// <param name="timeSpan">缓存失效时间</param>
        public void Add(string key, object value, TimeSpan timeSpan)
        {
            key = key.ToLower();
            this.cache.Store(StoreMode.Set, key, value, DateTime.Now.Add(timeSpan));
        }

        /// <summary>
        /// 加入依赖物理文件的缓存项
        /// </summary>
        /// <param name="key">缓存项标识</param>
        /// <param name="value">缓存项</param>
        /// <param name="fullFileNameOfFileDependency">主要应用于配置文件或配置项</param>
        public void AddWithFileDependency(string key, object value, string fullFileNameOfFileDependency)
        {
            this.Add(key, value, new TimeSpan(30, 0, 0, 0));
        }

        /// <summary>
        /// 清空缓存
        /// </summary>
        public void Clear()
        {
            this.cache.FlushAll();
        }

        /// <summary>
        /// 获取缓存项
        /// </summary>
        /// <param name="cacheKey">缓存项标识</param>
        /// <returns>返回cacheKey对应的缓存项，如果不存在则返回null</returns>
        public object Get(string cacheKey)
        {
            cacheKey = cacheKey.ToLower();
            HttpContext current = HttpContext.Current;
            if ((current != null) && current.Items.Contains(cacheKey))
            {
                return current.Items[cacheKey];
            }
            object obj2 = this.cache.Get(cacheKey);
            if ((current != null) && (obj2 != null))
            {
                current.Items[cacheKey] = obj2;
            }
            return obj2;
        }
        /// <summary>
        /// 获取缓存服务器统计信息
        /// </summary>
        /// <returns></returns>
        public Dictionary<string, Dictionary<string, string>> GetStatistics()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 标识删除
        /// </summary>
        /// <param name="key">缓存项标识</param>
        /// <param name="value">缓存项</param>
        /// <param name="timeSpan">缓存失效时间间隔</param>
        /// <remarks>
        /// 由于DB读写分离导致只读DB会有延迟，为保证缓存中的数据时时更新，需要在缓存中设置ID缓存为已删除状态
        /// </remarks>
        public void MarkDeletion(string key, object value, TimeSpan timeSpan)
        {
            this.Set(key, value, timeSpan);
        }

        /// <summary>
        /// 移除缓存项
        /// </summary>
        /// <param name="cacheKey">缓存项标识</param>
        public void Remove(string cacheKey)
        {
            cacheKey = cacheKey.ToLower();
            this.cache.Remove(cacheKey);
        }

        /// <summary>
        /// 如果不存在缓存项则添加，否则更新
        /// </summary>
        /// <param name="key">缓存项标识</param>
        /// <param name="value">缓存项</param>
        /// <param name="timeSpan">缓存失效时间</param>
        public void Set(string key, object value, TimeSpan timeSpan)
        {
            this.Add(key, value, timeSpan);
        }

    }
}
