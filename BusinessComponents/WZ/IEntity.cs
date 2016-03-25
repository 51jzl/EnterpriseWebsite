using System;

namespace WZ
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
    }
}
