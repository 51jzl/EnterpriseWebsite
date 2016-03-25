using PetaPoco;
using System;

namespace WZ.Common.Model
{
    /// <summary>
    /// 角色&菜单
    /// </summary>
    [TableName("tn_RoleMenus")]
    [PrimaryKey("Id", autoIncrement = true)]
    [Serializable]
    public class RoleMenus : IEntity
    {
        public int Id { get; set; }
        public string RoleId { get; set; }
        public int MenuId { get; set; }

        #region IEntity 成员

        object IEntity.EntityId { get { return this.Id; } }

        #endregion
    }
}
