using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace Legacy.Monolith.Controllers
{
    [Authorize]
    public class ModernizedServiceAController : Controller
    {
        public async Task<ActionResult> Index()
        {            
            var _sharedCookieName = System.Configuration.ConfigurationManager.AppSettings["SharedCookieName"];
            var _service_A_Url = System.Configuration.ConfigurationManager.AppSettings["ServiceA:Url"];

            var response = new HttpResponseMessage();

            try
            {
                // Grab the incoming auth cookie
                var authCookie = Request.Cookies.Get(_sharedCookieName);                

                using (var client = new HttpClient(new HttpClientHandler { UseCookies = false })) // FYI: For Prod, use a centralized re-usable instance of the HttpClient
                {
                    // Pass the incoming auth cookie along
                    client.DefaultRequestHeaders.Add("Cookie", $"{authCookie.Name}={authCookie.Value}");

                    response = await client.GetAsync(_service_A_Url);
                }

                response.EnsureSuccessStatusCode();
                ViewBag.ApiResponse = response.Content.ReadAsStringAsync().Result;

            }
            catch (Exception ex)
            {
                ViewBag.ApiResponse = $"Error: external api call responded with: serviceAURL: {_service_A_Url}, {response.StatusCode}, {response.ReasonPhrase}. Exception:Stacktracke= {ex.StackTrace} AND  Exception:Msg= {ex.Message} ";
            }            

            return View();
        }
    }
}