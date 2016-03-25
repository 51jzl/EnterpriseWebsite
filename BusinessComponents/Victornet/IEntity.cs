using System;

namespace Victornet
{
    /// <summary>
    /// Entity接口（所有实体都应该实现该接口）
    /// </summary>
    public interface IEntity
    {
        /// <summary>
        /// 实体ID
        /// </summary>
        object EntityId { get; }

        /// <summary>
        /// 该实体是否已经在数据库中删除(分布式部署时使用)
        /// </summary>
        bool IsDeletedInDatabase { get; set; }
    }
}
