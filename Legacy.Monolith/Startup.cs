using Microsoft.Owin;
using Microsoft.Owin.Security.Cookies;
using Owin;

//[assembly: OwinStartupAttribute(typeof(Monolith.Startup))]
namespace Legacy.Monolith
{
	public partial class Startup
	{
		public void Configuration(IAppBuilder app)
		{
			ConfigureAuth(app); // Wiring up the App_Start/Startup.Auth.cs
		}
	}
}