using System.Collections.Generic;
using WZ;
using WZ.Common.Model;
using WZ.Repositories;

namespace WZ.WebSite.Areas.Manage
{
    public interface IRoleMenusRepository : IRepository<RoleMenus>
    {
        IEnumerable<Menu> GetRoleMenus(string roleId);
    }
}
