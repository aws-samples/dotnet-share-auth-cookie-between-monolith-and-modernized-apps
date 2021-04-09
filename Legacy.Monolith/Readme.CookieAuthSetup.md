# Security - Steps to Add Authentication

1. Add Following Nugests
	- Microsoft.Owin.Host.SystemWeb // This package enables the OWIN middleware to hook into the IIS request pipeline.
	- Microsoft.Owin.Security.Cookies // This package enables cookie based authentication.
2. Initiazlie OWIN identity components
	- Add new partial class called, Startup.cs, to the project // In 'App_Start' folder --> right click --> Add--> New Item --> Class
	```
	// Complete code
	using Microsoft.Owin;
	using Microsoft.Owin.Security.Cookies;
	using Owin;

	//[assembly: OwinStartupAttribute(typeof(Legacy.Startup))]
	namespace Legacy
	{
		public partial class Startup
		{
			public void Configuration(IAppBuilder app)
			{
			}
		}
	}
	```

	- Add new partial class called, Startup.Auth.cs, to the 'App_Start' folder.
	- Add the cookie-based auth middleware.
	```
	// Complete code
	using Microsoft.Owin;
	using Microsoft.Owin.Security.Cookies;
	using Owin;

	namespace Legacy
	{
		public partial class Startup
		{
			public void ConfigureAuth(IAppBuilder app)
			{

				/* FYI: this extension tells ASP.Net Identity framework to use cookie based authentication; 
				* use a cookie to store information for the signed in user.
				*/
				app.UseCookieAuthentication(new CookieAuthenticationOptions
				{
					// This string value identifies the cookie.
					AuthenticationType = "ApplicationCookie", // FYI: could use strongly typed version as well DefaultAuthenticationTypes.ApplicationCookie.

					// When the application returns an unauthorized response (HTTP 401), redirect the user to this path.
					LoginPath = new PathString("/login")
				});

			}
		}
	}
	```

	- In the Statup.cs, wire up the Startup.Auth
	```
	// Complete code
	using Microsoft.Owin;
	using Microsoft.Owin.Security.Cookies;
	using Owin;

	//[assembly: OwinStartupAttribute(typeof(Legacy.Startup))]
	namespace Legacy
	{
		public partial class Startup
		{
			public void Configuration(IAppBuilder app)
			{
				ConfigureAuth(app); // Wiring up the Startup.Auth.cs
			}
		}
	}
	```

 - Ensure the 'Index.cshtml' has only the  bare minimum.
 ```
	@{
		ViewBag.Title = "Home Page (Index)";
	}

	<h2>Welcome to Auth modernization walkthrough!</h2>
	<h4>You need to be logged in to view this page.</h4>
 ```

 - Run the project and validate that the user can see the home page.

 - Add a new controller called, AuthController.cs.
 ```
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Web;
	using System.Web.Mvc;

	namespace Legacy.Controllers
	{
			public class AuthController : Controller
			{
					// GET: Auth
					public ActionResult Login()
					{
							return View();
					}
			}
	}
 ```

 - Add a new view called, Login, under the Views/Auth folder.
 ```
	@{
			ViewBag.Title = "Login Page";
	}

	<h2>Login Page</h2>
 ```

 - Decorate the HomeController class with the attribute, [Authorzie].
 - Run the project and validate that the user is sent to the Login page.
 - Under the 'Models' folder, add LoginModel.cs
 ```
  using System.ComponentModel.DataAnnotations;
	using System.Web.Mvc;

	namespace Legacy
	{

		/* FYI:
		 * 1) Metadata annotation attributes will help some of MVC’s HTML helpers to build the login form.
		 * 2) ReturnUrl property is decorated with the HiddenInput and ScaffoldColumn(false) attributes.
		 * HiddenInput attribute indicates that this property would be rendered as a hidden input element.	
		 * Also, the ScaffoldColumn(false) will tell the razor view not to build the form elements: this property should not be displayed as an input element.
		 */
		public class LoginModel
		{
			[Required]
			[DataType(DataType.EmailAddress)]
			public string Email { get; set; }

			[Required]
			[DataType(DataType.Password)]
			public string Password { get; set; }

			[HiddenInput]
			[ScaffoldColumn(false)]
			public string ReturnUrl { get; set; }
		}
	}
 ```

 - In the AuthController.cs, add the Login and Logout functionality 

 ```
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
				"ApplicationCookie");

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

		authManager.SignOut("ApplicationCookie");
		return RedirectToAction("Index","Home");
	}

	private string GetRedirectUrl(string returnUrl)
	{
		if (string.IsNullOrEmpty(returnUrl) || !Url.IsLocalUrl(returnUrl))
		{
			return Url.Action("Index", "Home");
		}

		return returnUrl;
	}
 ```

 - Now update the Views/Auth/Login.cshtml
 ```
  @model Legacy.Models.LoginModel

	@{
			ViewBag.Title = "Log In";
	}

	<h2>Log In</h2>

	@Html.ValidationSummary(true)

	@using (Html.BeginForm())
	{
			@Html.EditorForModel()
    
			<p>
					<button type="submit">Log In</button>
			</p>
	}
 ```