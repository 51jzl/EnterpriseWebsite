using System;

namespace Victornet.Caching
{
    public enum EntityCacheExpirationPolicies
    {
        /// <summary>
        /// 稳定数据
        /// </summary>
        /// <remarks>
        /// 例如： Area/School
        /// </remarks>
        Stable = 1,
        /// <summary>
        /// 常用的单个实体
        /// </summary>
        /// <remarks>
        /// 例如： 用户、圈子
        /// </remarks>
        Usual = 3,
        /// <summary>
        /// 单个实体
        /// </summary>
        /// <remarks>
        /// 例如： 博文、帖子
        /// </remarks>
        Normal = 5,
    }
}
