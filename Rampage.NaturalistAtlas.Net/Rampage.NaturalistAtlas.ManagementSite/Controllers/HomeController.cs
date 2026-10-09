using Microsoft.AspNetCore.Mvc;
using Rampage.Management.Mvc;

namespace Rampage.NaturalistAtlas.ManagementSite.Controllers
{
    /// <summary>
    /// 后台首页（运营看板）：对所有登录管理员开放——登录固定落 /，无看板权限时前端优雅降级（见 admin-dashboard.js）
    /// Admin home (dashboard): open to every logged-in admin — login always lands at /, and the
    /// page degrades gracefully without dashboard permission (see admin-dashboard.js)
    /// </summary>
    [PermissionLoggedInFilter]
    public class HomeController : NaturalistAtlasManagementController
    {
        /// <summary>
        /// 后台首页概览 / Admin home overview
        /// </summary>
        public IActionResult Index()
        {
            return VW();
        }
    }
}
