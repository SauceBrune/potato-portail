using Microsoft.Owin;
using Owin;
using SysInternshipManagement;

[assembly: OwinStartup(typeof(Startup))]

namespace SysInternshipManagement
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}