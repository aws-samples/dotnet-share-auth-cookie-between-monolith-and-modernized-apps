using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace Legacy.Monolith.Controllers
{
    [Authorize]
    public class ModernizedServiceBController : Controller
    {
        public async Task<ActionResult> Index()
        {   
            var _sharedCookieName = System.Configuration.ConfigurationManager.AppSettings["SharedCookieName"];
            var _service_B_Url = System.Configuration.ConfigurationManager.AppSettings["ServiceB:Url"];

            var response = new HttpResponseMessage();

            try
            {
                // Grab the auth cookie
                var authCookie = Request.Cookies.Get(_sharedCookieName);                

                using (var client = new HttpClient(new HttpClientHandler { UseCookies = false }))
                {
                    // Pass it auth cookie along
                    client.DefaultRequestHeaders.Add("Cookie", $"{authCookie.Name}={authCookie.Value}");

                    response = await client.GetAsync(_service_B_Url);
                }

                response.EnsureSuccessStatusCode();
                ViewBag.ApiResponse = response.Content.ReadAsStringAsync().Result;

            }
            catch
            {
                ViewBag.ApiResponse = $"Error: external api call responded with: {response.ReasonPhrase} ";
            }            

            return View();
        }
    }
}