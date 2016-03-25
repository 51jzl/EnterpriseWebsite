using System;
using System.Collections.Generic;
using WZ;
using WZ.Repositories;
using System.Linq;
using WZ.Common.Model;

namespace WZ.WebSite.Areas.Manage
{
    public class RoleMenusRepository : Repository<RoleMenus>, IRoleMenusRepository
    {
        public MenuRepository menuRepository = new MenuRepository();
        public virtual IEnumerable<Menu> GetRoleMenus(string roleId)
        {
            IEnumerable<int> menuIds = this.GetAll().ToList().Where(n => roleId.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Contains(n.RoleId)).Select(n => n.MenuId).Distinct().ToArray();
            return menuRepository.GetAll().Where(n => menuIds.Contains(n.Id)).ToArray();
        }
    }
}
