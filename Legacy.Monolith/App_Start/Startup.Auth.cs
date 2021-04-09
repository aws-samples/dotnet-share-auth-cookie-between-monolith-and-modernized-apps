using Legacy.Monolith.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Owin;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.Interop;
using Owin;

namespace Legacy.Monolith
{
  public partial class Startup
  {     
        public void ConfigureAuth(IAppBuilder app)
        {
            #region [Customer]: add the auth middleware & configure it for shared cookie.

            var _sharedCookieName = System.Configuration.ConfigurationManager.AppSettings["SharedCookieName"];

            // FYI: Using the cookie based authentication without the ASP.NET Identity.
            app.UseCookieAuthentication(new CookieAuthenticationOptions
            {
                /* FYI:
                 * The auth handlers and their configuration options are called, "schemes".
                 * The auth scheme name that you choose (e.g. "Identity.Application") must be consistently used within and across the shared cookie apps.
                 * The auth schema is used when encrpyting/decrypting cookies.
                 */
                AuthenticationType = "Identity.Application",
                // FYI: This auth cookie name (e.g. ".AspNet.SharedCookie") must be same across the shared cookie apps.
                CookieName = _sharedCookieName,
                // FYI: The unauthorized access results in HTTP 401; however, this middleware intercepts the call and redirects (HTTP 302) the caller to this path.
                LoginPath = new PathString("/Auth/Login"), 
                /*Provider = new CookieAuthenticationProvider
                {
                    OnValidateIdentity =
                        SecurityStampValidator
                            .OnValidateIdentity<ApplicationUserManager, ApplicationUser>(
                                validateInterval: TimeSpan.FromMinutes(30),
                                regenerateIdentity: (manager, user) =>
                                    user.GenerateUserIdentityAsync(manager))
                },*/
                TicketDataFormat = new AspNetTicketDataFormat(
                    new DataProtectorShim(
                        DataProtectionProvider.Create(
                            /* FYI: this Data Protection key must be shared across the shared cookie apps.  
                             * Note: when the custom IXmlRepository implementation is provided, this path configuration will be ignored.
                             */
                            new System.IO.DirectoryInfo(@"C:\SharedCookieAppKey"),
                            (builder) => 
                                {
                                    // FYI: The common app name that you choose (e.g. SharedCookieApp) is used to enable the data protection system to share the Data Protection keys.
                                    builder.SetApplicationName("SharedCookieApp");

                                    #region 
                                    // FYI: comment this region if you want to make it work without any central repository (e.g. AWS Parameter store)
                                    builder.Services.AddSingleton<IConfigureOptions<KeyManagementOptions>>(services =>
                                    {
                                        return new ConfigureOptions<KeyManagementOptions>(options =>
                                        {
                                            options.XmlRepository = new CustomPersistKeysToAWSParameterStore(); // Register the custom Data Protection key repository implementation
                                        });
                                    });
                                    #endregion
                                }
                        ).CreateProtector(
                            "Microsoft.AspNetCore.Authentication.Cookies." +
                                "CookieAuthenticationMiddleware",
                            "Identity.Application", // FYI: this auth scheme that you choose (e.g. "Identity.Application") must be same across the shared cookie apps.
                            "v2"))),
                CookieManager = new ChunkingCookieManager()
            });

            System.Web.Helpers.AntiForgeryConfig.UniqueClaimTypeIdentifier =
                "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name";

            #endregion

        }
    }
}