using System;

namespace Victornet.Caching
{
    /// <summary>
    /// 用于列表缓存设置接口
    /// </summary>
    public interface IListCacheSetting
    {
        /// <summary>
        /// 用于列表缓存设置接口
        /// </summary>
        /// <remarks>
        /// 用于在查询对象中设置缓存策略
        /// </remarks>
        string AreaCachePropertyName { get; set; }

        /// <summary>
        /// 缓存分区字段值
        /// </summary>
        object AreaCachePropertyValue { get; set; }

        /// <summary>
        /// 列表缓存版本设置
        /// </summary>
        Victornet.Caching.CacheVersionType CacheVersionType { get; }
    }
}
