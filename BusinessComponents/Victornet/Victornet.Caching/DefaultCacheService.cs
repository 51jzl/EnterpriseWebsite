using System;
using System.Collections.Generic;

namespace Victornet.Caching
{
    [Serializable]
    public class DefaultCacheService:ICacheService
    {

        private ICache cache;
        private readonly Dictionary<CachingExpirationType, TimeSpan> cachingExpirationDictionary;
        private bool enableDistributedCache;
        private ICache localCache;

        /// <summary>
        /// 构造函数(仅本机缓存)
        /// </summary>
        /// <param name="cache">本机缓存</param>
        /// <param name="cacheExpirationFactor">缓存过期时间因子</param>
        public DefaultCacheService(ICache cache, float cacheExpirationFactor)
            : this(cache, cache, cacheExpirationFactor, false)
        {
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="cache">缓存</param>
        /// <param name="localCache">本机缓存</param>
        /// <param name="cacheExpirationFactor">缓存过期时间因子</param>
        /// <param name="enableDistributedCache">是否启用分布式缓存</param>
        public DefaultCacheService(ICache cache, ICache localCache, float cacheExpirationFactor, bool enableDistributedCache)
        {
            this.cache = cache;
            this.localCache = localCache;
            this.enableDistributedCache = enableDistributedCache;
            this.cachingExpirationDictionary = new Dictionary<CachingExpirationType, TimeSpan>();
            this.cachingExpirationDictionary.Add(CachingExpirationType.Invariable, new TimeSpan(0, 0, (int)(86400f * cacheExpirationFactor)));
            this.cachingExpirationDictionary.Add(CachingExpirationType.Stable, new TimeSpan(0, 0, (int)(28800f * cacheExpirationFactor)));
            this.cachingExpirationDictionary.Add(CachingExpirationType.RelativelyStable, new TimeSpan(0, 0, (int)(7200f * cacheExpirationFactor)));
            this.cachingExpirationDictionary.Add(CachingExpirationType.UsualSingleObject, new TimeSpan(0, 0, (int)(600f * cacheExpirationFactor)));
            this.cachingExpirationDictionary.Add(CachingExpirationType.UsualObjectCollection, new TimeSpan(0, 0, (int)(300f * cacheExpirationFactor)));
            this.cachingExpirationDictionary.Add(CachingExpirationType.SingleObject, new TimeSpan(0, 0, (int)(180f * cacheExpirationFactor)));
            this.cachingExpirationDictionary.Add(CachingExpirationType.ObjectCollection, new TimeSpan(0, 0, (int)(180f * cacheExpirationFactor)));
        }

        /// <summary>
        /// 添加到缓存
        /// </summary>
        /// <param name="cacheKey">缓存项标识</param>
        /// <param name="value">缓存项</param>
        /// <param name="timeSpan">缓存失效时间间隔</param>
        public void Add(string cacheKey, object value, TimeSpan timeSpan)
        {
            this.cache.Add(cacheKey, value, timeSpan);
        }

        /// <summary>
        /// 添加到缓存
        /// </summary>
        /// <param name="cacheKey">缓存项标识</param>
        /// <param name="value">缓存项</param>
        /// <param name="cachingExpirationType">缓存期限类型</param>
        public void Add(string cacheKey, object value, CachingExpirationType cachingExpirationType)
        {
            this.Add(cacheKey, value, this.cachingExpirationDictionary[cachingExpirationType]);
        }

        /// <summary>
        /// 清空缓存
        /// </summary>
        public void Clear()
        {
            this.cache.Clear();
        }

        /// <summary>
        /// 从缓存获取
        /// </summary>
        /// <param name="cacheKey">缓存项标识</param>
        /// <returns></returns>
        public object Get(string cacheKey)
        {
            object obj2 = null;
            if (this.enableDistributedCache)
            {
                obj2 = this.localCache.Get(cacheKey);
            }
            if (obj2 == null)
            {
                obj2 = this.cache.Get(cacheKey);
                if (this.enableDistributedCache)
                {
                    this.localCache.Add(cacheKey, obj2, this.cachingExpirationDictionary[CachingExpirationType.SingleObject]);
                }
            }
            return obj2;
        }

        /// <summary>
        /// 从缓存获取
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="cacheKey">缓存项标识</param>
        /// <returns></returns>
        public T Get<T>(string cacheKey) where T : class
        {
            object obj2 = this.Get(cacheKey);
            if (obj2 != null)
            {
                return (obj2 as T);
            }
            return default(T);
        }

        /// <summary>
        /// 从一层缓存获取缓存项
        /// </summary>
        /// <param name="cacheKey">缓存项标识</param>
        /// <returns></returns>
        public object GetFromFirstLevel(string cacheKey)
        {
            return this.cache.Get(cacheKey);
        }

        /// <summary>
        /// 从一层缓存获取缓存项
        /// </summary>
        /// <typeparam name="T">缓存项类型</typeparam>
        /// <param name="cacheKey">缓存项标识</param>
        /// <returns>返回cacheKey对应的缓存项，如果不存在则返回null</returns>
        /// <remarks>
        /// 在启用分布式缓存的情况下，指穿透二级缓存从一层缓存（分布式缓存）读取
        /// </remarks>
        public T GetFromFirstLevel<T>(string cacheKey) where T : class
        {
            object fromFirstLevel = this.GetFromFirstLevel(cacheKey);
            if (fromFirstLevel != null)
            {
                return (fromFirstLevel as T);
            }
            return default(T);
        }

        /// <summary>
        /// 标识为删除
        /// </summary>
        /// <param name="cacheKey">缓存项标识</param>
        /// <param name="entity">缓存的实体</param>
        /// <param name="cachingExpirationType">缓存期限类型</param>
        public void MarkDeletion(string cacheKey, IEntity entity, CachingExpirationType cachingExpirationType)
        {
            entity.IsDeletedInDatabase = true;
            this.cache.MarkDeletion(cacheKey, entity, this.cachingExpirationDictionary[cachingExpirationType]);
        }

        /// <summary>
        /// 移除缓存
        /// </summary>
        /// <param name="cacheKey">缓存项标识</param>
        public void Remove(string cacheKey)
        {
            this.cache.Remove(cacheKey);
        }

        /// <summary>
        /// 添加或更新缓存
        /// </summary>
        /// <param name="cacheKey">缓存项标识</param>
        /// <param name="value">缓存项</param>
        /// <param name="timeSpan">缓存失效时间间隔</param>
        public void Set(string cacheKey, object value, TimeSpan timeSpan)
        {
            this.cache.Set(cacheKey, value, timeSpan);
        }

        /// <summary>
        /// 添加或更新缓存
        /// </summary>
        /// <param name="cacheKey">缓存项标识</param>
        /// <param name="value">缓存项</param>
        /// <param name="cachingExpirationType">缓存期限类型</param>
        public void Set(string cacheKey, object value, CachingExpirationType cachingExpirationType)
        {
            this.Set(cacheKey, value, this.cachingExpirationDictionary[cachingExpirationType]);
        }

        /// <summary>
        /// 是否启用分布式缓存
        /// </summary>
        public bool EnableDistributedCache
        {
            get
            {
                return this.enableDistributedCache;
            }
        }
    }
}
