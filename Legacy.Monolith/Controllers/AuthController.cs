using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Web;
using System.Web.Mvc;

namespace Legacy.Monolith.Controllers
{
  public class AuthController : Controller
  {
    [HttpGet]
    public ActionResult Login(string returnUrl)
    {

      var model = new LoginModel()
      {
        ReturnUrl = returnUrl
      };

      return View(model);
    }

    [HttpPost]
    public ActionResult Login(LoginModel model)
    {
			if (!ModelState.IsValid)
			{
				return View();
			}

			// The user related information have been hardcoded for the time being.
			// In production, the hard coded values would be fetched from the database using the new ASP.Net Identity UserManager.
			if (model.Email == "admin@admin.com" && model.Password == "admin")
			{
				var identity = new ClaimsIdentity(
					new[] {
						new Claim(ClaimTypes.Name, "Admin"),
						new Claim(ClaimTypes.Email, "admin@admin.com")
					},
					"Identity.Application");

                var ctx = Request.GetOwinContext();
				var authManager = ctx.Authentication;

				authManager.SignIn(identity);

				return Redirect(GetRedirectUrl(model.ReturnUrl));
			}

			// In case user authentication fails.
			ModelState.AddModelError("", "Invalid email or password");
			return View();
		}

		public ActionResult Logout()
		{
			var ctx = Request.GetOwinContext();
			var authManager = ctx.Authentication;

			authManager.SignOut("Identity.Application");
			return RedirectToAction("Index", "Home");
		}

		private string GetRedirectUrl(string returnUrl)
		{
			if (string.IsNullOrEmpty(returnUrl) || !Url.IsLocalUrl(returnUrl))
			{
				return Url.Action("index", "home");
			}

			return returnUrl;
		}
	}
}