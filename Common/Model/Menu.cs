using PetaPoco;
using System;

namespace WZ.Common.Model
{
    /// <summary>
    /// 菜单
    /// </summary>
    [TableName("tn_Menu")]
    [PrimaryKey("Id", autoIncrement = false)]
    [Serializable]
    public class Menu : IEntity
    {
        public int Id { get; set; }
        public int ParentId { get; set; }
        public string ParentIdList { get; set; }
        public int ChildCount { get; set; }
        public int Depth { get; set; }
        public string Title { get; set; }
        public string Url { get; set; }
        public string Ico { get; set; }
        public bool IsHref { get; set; }

        #region IEntity 成员

        object IEntity.EntityId { get { return this.Id; } }

        #endregion
    }
}
