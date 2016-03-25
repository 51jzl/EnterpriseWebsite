using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(WZ.WebSite.Startup))]
namespace WZ.WebSite
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
