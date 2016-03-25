using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace Victornet.Caching
{
    /// <summary>
    /// 实时性缓存助手
    /// </summary>
    [Serializable]
    public class RealTimeCacheHelper
    {
        private ConcurrentDictionary<string, ConcurrentDictionary<int, int>> areaVersionDictionary = new ConcurrentDictionary<string, ConcurrentDictionary<int, int>>();
        private ConcurrentDictionary<object, int> entityVersionDictionary = new ConcurrentDictionary<object, int>();
        private int globalVersion;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="enableCache"> 是否启用缓存</param>
        /// <param name="typeHashID">类型名称哈希值</param>
        public RealTimeCacheHelper(bool enableCache, string typeHashID)
        {
            this.EnableCache = enableCache;
            this.TypeHashID = typeHashID;
        }

        /// <summary>
        /// 获取列表缓存区域version
        /// </summary>
        /// <param name="propertyName">分区属性名称</param>
        /// <param name="propertyValue">分区属性值</param>
        /// <returns>分区属性的缓存版本（从0开始）</returns>
        public int GetAreaVersion(string propertyName, object propertyValue)
        {
            int num = 0;
            if (!string.IsNullOrEmpty(propertyName))
            {
                ConcurrentDictionary<int, int> dictionary;
                propertyName = propertyName.ToLower();
                if (this.areaVersionDictionary.TryGetValue(propertyName, out dictionary))
                {
                    dictionary.TryGetValue(propertyValue.GetHashCode(), out num);
                }
            }
            return num;
        }

        /// <summary>
        /// 获取实体缓存的cacheKey
        /// </summary>
        /// <param name="primaryKey">主键</param>
        /// <returns>实体的CacheKey</returns>
        public string GetCacheKeyOfEntity(object primaryKey)
        {
            if (DIContainer.Resolve<ICacheService>().EnableDistributedCache)
            {
                return string.Concat(new object[] { this.TypeHashID, ":", primaryKey, ":", this.GetEntityVersion(primaryKey) });
            }
            return (this.TypeHashID + ":" + primaryKey);
        }

        /// <summary>
        /// 获取实体正文缓存的cacheKey
        /// </summary>
        /// <param name="primaryKey">主键</param>
        /// <returns>实体正文缓存的cacheKey</returns>
        public string GetCacheKeyOfEntityBody(object primaryKey)
        {
            if (DIContainer.Resolve<ICacheService>().EnableDistributedCache)
            {
                return string.Concat(new object[] { this.TypeHashID, ":B-", primaryKey, ":", this.GetEntityVersion(primaryKey) });
            }
            return (this.TypeHashID + ":B-" + primaryKey);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="typeHashID"></param>
        /// <returns></returns>
        internal static string GetCacheKeyOfTimelinessHelper(string typeHashID)
        {
            return ("CacheTimelinessHelper:" + typeHashID);
        }

        /// <summary>
        /// 获取Entity的缓存版本
        /// </summary>
        /// <param name="primaryKey">主键</param>
        /// <returns>实体的缓存版本（从0开始）</returns>
        public int GetEntityVersion(object primaryKey)
        {
            int num = 0;
            this.entityVersionDictionary.TryGetValue(primaryKey, out num);
            return num;
        }

        /// <summary>
        /// 列表缓存全局version
        /// </summary>
        /// <returns></returns>
        public int GetGlobalVersion()
        {
            return this.globalVersion;
        }

        /// <summary>
        /// 获取列表缓存CacheKey的前缀（例如：abe3ds2sa90:8:）
        /// </summary>
        /// <param name="cacheVersionType"></param>
        /// <returns></returns>
        public string GetListCacheKeyPrefix(CacheVersionType cacheVersionType)
        {
            return this.GetListCacheKeyPrefix(cacheVersionType, null, null);
        }

        /// <summary>
        /// 获取列表缓存CacheKey的前缀（例如：abe3ds2sa90:8:）
        /// </summary>
        /// <param name="cacheVersionSetting"></param>
        /// <returns></returns>
        public string GetListCacheKeyPrefix(IListCacheSetting cacheVersionSetting)
        {
            StringBuilder builder = new StringBuilder(this.TypeHashID);
            builder.Append("-L:");
            switch (cacheVersionSetting.CacheVersionType)
            {
                case CacheVersionType.GlobalVersion:
                    builder.AppendFormat("{0}:", this.GetGlobalVersion());
                    break;

                case CacheVersionType.AreaVersion:
                    builder.AppendFormat("{0}-{1}-{2}:", cacheVersionSetting.AreaCachePropertyName, cacheVersionSetting.AreaCachePropertyValue.ToString(), this.GetAreaVersion(cacheVersionSetting.AreaCachePropertyName, cacheVersionSetting.AreaCachePropertyValue));
                    break;
            }
            return builder.ToString();
        }

        /// <summary>
        /// 获取列表缓存CacheKey的前缀（例如：abe3ds2sa90:8:）
        /// </summary>
        /// <param name="cacheVersionType"></param>
        /// <param name="areaCachePropertyName">缓存分区名称</param>
        /// <param name="areaCachePropertyValue">缓存分区值</param>
        /// <returns></returns>
        public string GetListCacheKeyPrefix(CacheVersionType cacheVersionType, string areaCachePropertyName, object areaCachePropertyValue)
        {
            StringBuilder builder = new StringBuilder(this.TypeHashID);
            builder.Append("-L:");
            switch (cacheVersionType)
            {
                case CacheVersionType.GlobalVersion:
                    builder.AppendFormat("{0}:", this.GetGlobalVersion());
                    break;

                case CacheVersionType.AreaVersion:
                    builder.AppendFormat("{0}-{1}-{2}:", areaCachePropertyName, areaCachePropertyValue, this.GetAreaVersion(areaCachePropertyName, areaCachePropertyValue));
                    break;
            }
            return builder.ToString();
        }

        /// <summary>
        /// 递增列表缓存区域version
        /// </summary>
        /// <param name="propertyName">分区属性名称</param>
        /// <param name="propertyValues">多个分区属性值</param>
        public void IncreaseAreaVersion(string propertyName, IEnumerable<object> propertyValues)
        {
            this.IncreaseAreaVersion(propertyName, propertyValues, true);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="propertyName">分区属性名称</param>
        /// <param name="propertyValue"></param>
        public void IncreaseAreaVersion(string propertyName, object propertyValue)
        {
            if (propertyValue != null)
            {
                this.IncreaseAreaVersion(propertyName, new object[] { propertyValue }, true);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="propertyName">分区属性名称</param>
        /// <param name="propertyValues">多个分区属性值</param>
        /// <param name="raiseChangeEvent"></param>
        private void IncreaseAreaVersion(string propertyName, IEnumerable<object> propertyValues, bool raiseChangeEvent)
        {
            if (!string.IsNullOrEmpty(propertyName))
            {
                ConcurrentDictionary<int, int> dictionary;
                propertyName = propertyName.ToLower();
                int num = 0;
                if (!this.areaVersionDictionary.TryGetValue(propertyName, out dictionary))
                {
                    this.areaVersionDictionary[propertyName] = new ConcurrentDictionary<int, int>();
                    dictionary = this.areaVersionDictionary[propertyName];
                }
                foreach (object obj2 in propertyValues)
                {
                    int hashCode = obj2.GetHashCode();
                    if (dictionary.TryGetValue(hashCode, out num))
                    {
                        num++;
                    }
                    else
                    {
                        num = 1;
                    }
                    dictionary[hashCode] = num;
                }
                if (raiseChangeEvent)
                {
                    this.OnChanged();
                }
            }
        }

        /// <summary>
        /// 递增实体缓存（仅更新实体时需要递增）
        /// </summary>
        /// <param name="entityId">实体Id</param>
        public void IncreaseEntityCacheVersion(object entityId)
        {
            if (DIContainer.Resolve<ICacheService>().EnableDistributedCache)
            {
                int num;
                if (this.entityVersionDictionary.TryGetValue(entityId, out num))
                {
                    num++;
                }
                else
                {
                    num = 1;
                }
                this.entityVersionDictionary[entityId] = num;
                this.OnChanged();
            }
        }

        /// <summary>
        /// 递增列表缓存全局版本
        /// </summary>
        public void IncreaseGlobalVersion()
        {
            this.globalVersion++;
        }

        /// <summary>
        /// 递增列表缓存version（增加、更改、删除实体时需要递增）
        /// </summary>
        /// <param name="entity">实体</param>
        public void IncreaseListCacheVersion(IEntity entity)
        {
            if (this.PropertiesOfArea != null)
            {
                foreach (PropertyInfo info in this.PropertiesOfArea)
                {
                    object obj2 = info.GetValue(entity, null);
                    if (obj2 != null)
                    {
                        this.IncreaseAreaVersion(info.Name.ToLower(), new object[] { obj2 }, false);
                    }
                }
            }
            this.IncreaseGlobalVersion();
            this.OnChanged();
        }

        /// <summary>
        /// 标识为已删除
        /// </summary>
        /// <param name="entity"></param>
        public void MarkDeletion(IEntity entity)
        {
            ICacheService service = DIContainer.Resolve<ICacheService>();
            if (this.EnableCache)
            {
                service.MarkDeletion(this.GetCacheKeyOfEntity(entity.EntityId), entity, Victornet.Caching.CachingExpirationType.SingleObject);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private void OnChanged()
        {
            ICacheService service = DIContainer.Resolve<ICacheService>();
            if (service.EnableDistributedCache)
            {
                service.Set(GetCacheKeyOfTimelinessHelper(this.TypeHashID), this, Victornet.Caching.CachingExpirationType.Invariable);
            }
        }

        /// <summary>
        /// 缓存过期类型
        /// </summary>
        public Victornet.Caching.CachingExpirationType CachingExpirationType { get; set; }

        /// <summary>
        /// 是否使用缓存
        /// </summary>
        public bool EnableCache { get; private set; }

        /// <summary>
        /// 缓存分区的属性
        /// </summary>
        public IEnumerable<PropertyInfo> PropertiesOfArea { get; set; }

        /// <summary>
        /// 实体正文缓存对应的属性名称（如果不需单独存储实体正文缓存，则不要设置该属性）
        /// </summary>
        public PropertyInfo PropertyNameOfBody { get; set; }

        /// <summary>
        /// 完整名称md5-16
        /// </summary>
        public string TypeHashID { get; private set; }
    }
}
