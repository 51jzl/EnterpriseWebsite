using System;

namespace Victornet.Caching
{
    public class CacheSettingAttribute : Attribute
    {
        private EntityCacheExpirationPolicies expirationPolicy = EntityCacheExpirationPolicies.Normal;
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="enableCache">是否使用缓存</param>
        public CacheSettingAttribute(bool enableCache)
        {
            this.EnableCache = enableCache;
        }

        /// <summary>
        /// 是否使用缓存
        /// </summary>
        public bool EnableCache { get; private set; }

        /// <summary>
        /// 缓存过期策略
        /// </summary>
        public EntityCacheExpirationPolicies ExpirationPolicy
        {
            get
            {
                return this.expirationPolicy;
            }
            set
            {
                this.expirationPolicy = value;
            }
        }

        /// <summary>
        /// 实体正文缓存对应的属性名称（如果不需单独存储实体正文缓存，则不要设置该属性）
        /// </summary>
        public string PropertyNameOfBody { get; set; }

        /// <summary>
        /// 缓存分区的属性名称（可以设置多个，用逗号分隔）
        /// </summary>
        /// <remarks>
        /// 必须是实体包含的属性，自动维护维护这些分区属性的版本号
        /// </remarks>
        public string PropertyNamesOfArea { get; set; }
    }
}
