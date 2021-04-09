using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Modernized.Backend.ServiceB.Services;

namespace Modernized.Backend.ServiceB
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {

            services.AddControllers();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "Modernized.Backend.ServiceB", Version = "v1" });
            });

            #region [Custom]: Health Check
            services.AddHealthChecks();
            #endregion

            #region [Custom]: Add the auth cookie support & configuring for Shared cookie.

            var _sharedCookieName = Configuration["ServiceB:SharedCookieName"];

            services.AddDataProtection()
                //.PersistKeysToFileSystem(new System.IO.DirectoryInfo(@"C:\SharedCookieAppKey")) // FYI: the same key must be shared by all the Apps sharing this cookie.
                .SetApplicationName("SharedCookieApp") // FYI: the App name value (e.g. "SharedCookieApp") must be same across all the Apps sharing this cookie.
                #region 
                // FYI: comment this region and uncomment the 'PersistKeysToFileSystem' to make shared cookie auth work without any central repository (e.g. AWS Parameter store)
                .Services.AddSingleton<IConfigureOptions<KeyManagementOptions>>(services =>
                {
                    return new ConfigureOptions<KeyManagementOptions>(options =>
                    {
                        options.XmlRepository = new CustomPersistKeysToAWSParameterStore();
                    });
                });
                #endregion

            // FYI:Schemes are a mechanism for referring to the authentication, challenge, and forbid behaviors of the associated handler.
            services.AddAuthentication("Identity.Application") // FYI: this is App's default auth scheme; used when no specific scheme (e.g. AddCookie) is registered.             
                .AddCookie("Identity.Application", options => // FYI: Helps register the specified auth scheme; this auth scheme name (e.g. "Identity.Application") must be same across all the Apps sharing this cookie.
                {
                    options.Cookie.Name = _sharedCookieName; // FYI: this auth cookie name(e.g. ".AspNet.SharedCookie") must be same across all the Apps sharing this cookie.                   
                    options.Events = new CookieAuthenticationEvents
                    {
                        /* FYI: Without this, the middleware will redirect the un-authorized requests to the default '/Account/Login' which does not exit (HTTP 404).
                         * To avoid confusing the caller with the 404, we are manually give the user a proper HTTP response.*/
                        OnRedirectToLogin = redirectContext =>
                        {
                            redirectContext.HttpContext.Response.StatusCode = 401;
                            return Task.CompletedTask;
                        }
                    };
                });
            #endregion
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Modernized.Backend.ServiceB v1"));
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            #region [Custom]: Add cookie-based auth middleware
            app.UseAuthentication();
            #endregion

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                #region [Custom]: register health check URL, read from the appsettings.json file
                endpoints.MapHealthChecks(Configuration["ServiceB:HealthCheckUrl"]);
                #endregion

                endpoints.MapControllers();
            });
        }
    }
}
