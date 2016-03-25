using System;

namespace Victornet.Caching
{
    public enum CacheVersionType
    {
        /// <summary>
        /// 不使用缓存版本
        /// </summary>
        None,
        /// <summary>
        /// 使用全局缓存版本
        /// </summary>
        GlobalVersion,
        /// <summary>
        /// 使用分区缓存版本
        /// </summary>
        AreaVersion
    }
}
