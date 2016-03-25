using System;
using System.Globalization;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin.Security;
using WZ.WebSite.Models;
using System.Collections.Generic;
using Microsoft.AspNet.Identity.EntityFramework;
using WZ.Common.Model;

namespace WZ.WebSite.Areas.Manage.Controllers
{
    [Authorize]
    public class AdminController : Controller
    {
        #region 变量 初始化
        private ApplicationSignInManager _signInManager;
        private ApplicationUserManager _userManager;
        private RoleMenusRepository _roleMenusRepository;
        public AdminController()
        {
        }

        public AdminController(ApplicationUserManager userManager, ApplicationSignInManager signInManager)
        {
            UserManager = userManager;
            SignInManager = signInManager;
        }

        public ApplicationSignInManager SignInManager
        {
            get
            {
                return _signInManager ?? HttpContext.GetOwinContext().Get<ApplicationSignInManager>();
            }
            private set
            {
                _signInManager = value;
            }
        }

        public ApplicationUserManager UserManager
        {
            get
            {
                return _userManager ?? HttpContext.GetOwinContext().GetUserManager<ApplicationUserManager>();
            }
            private set
            {
                _userManager = value;
            }
        }

        public RoleMenusRepository RoleMenusRepository
        {
            get
            {
                return _roleMenusRepository ?? new RoleMenusRepository();
            }
            set
            {
                _roleMenusRepository = value;
            }
        }
        #endregion
        // GET: Manage/Admin
        public ActionResult Index()
        {
            ApplicationUser thisUser = UserManager.FindById(User.Identity.GetUserId());
            IEnumerator<IdentityUserRole> userRoles = thisUser.Roles.GetEnumerator();
            IdentityUserRole userRole = null;
            string roleids = string.Empty;
            while (userRoles.MoveNext())
            {
                userRole = userRoles.Current;
                if (userRole != null)
                    roleids += userRole.RoleId + ",";
            }
            IEnumerable<Menu> model = RoleMenusRepository.GetRoleMenus(roleids);
            return View(model);
        }
        #region 登出
        //
        // POST: /Account/LogOff
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult LogOff()
        {
            AuthenticationManager.SignOut(DefaultAuthenticationTypes.ApplicationCookie);
            return RedirectToAction("Login", "Default");
        }
        #endregion



        #region 帮助程序
        // 用于在添加外部登录名时提供 XSRF 保护
        private const string XsrfKey = "XsrfId";

        private IAuthenticationManager AuthenticationManager
        {
            get
            {
                return HttpContext.GetOwinContext().Authentication;
            }
        }

        private void AddErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error);
            }
        }

        private ActionResult RedirectToLocal(string returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction("Login", "Default");
        }

        internal class ChallengeResult : HttpUnauthorizedResult
        {
            public ChallengeResult(string provider, string redirectUri)
                : this(provider, redirectUri, null)
            {
            }

            public ChallengeResult(string provider, string redirectUri, string userId)
            {
                LoginProvider = provider;
                RedirectUri = redirectUri;
                UserId = userId;
            }

            public string LoginProvider { get; set; }
            public string RedirectUri { get; set; }
            public string UserId { get; set; }

            public override void ExecuteResult(ControllerContext context)
            {
                var properties = new AuthenticationProperties { RedirectUri = RedirectUri };
                if (UserId != null)
                {
                    properties.Dictionary[XsrfKey] = UserId;
                }
                context.HttpContext.GetOwinContext().Authentication.Challenge(properties, LoginProvider);
            }
        }
        #endregion
    }
}