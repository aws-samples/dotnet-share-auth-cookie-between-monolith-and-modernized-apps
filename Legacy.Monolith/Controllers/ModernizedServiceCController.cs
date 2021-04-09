using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Mvc;

using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Collections.Generic;

namespace Legacy.Monolith.Controllers
{
    [Authorize]
    public class ModernizedServiceCController : Controller
    { 
        public string GetJWTForCurrentUser()
        {
            // Based on https://www.c-sharpcorner.com/article/asp-net-web-api-2-creating-and-validating-jwt-json-web-token/ 
            var user = this.HttpContext.User;
            string key = "my_secret_key_1234";    
            var issuer = "http://example.com";   

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            //Create a List of Claims, Keep claims name short    
            var permClaims = new List<Claim>();
            permClaims.Add(new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()));
            permClaims.Add(new Claim("valid", "1"));
            permClaims.Add(new Claim("userid", user.Identity.Name));
            permClaims.Add(new Claim("name", user.Identity.Name));

            //Create Security Token object by giving required parameters    
            var token = new JwtSecurityToken(issuer, //Issure    
                            issuer,  //Audience    
                            permClaims,
                            expires: DateTime.Now.AddDays(1),
                            signingCredentials: credentials);
            System.Diagnostics.Debug.WriteLine(token);
            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public async Task<ActionResult> Index()
        {
            var response = new HttpResponseMessage();

            try
            {

                using (var client = new HttpClient(new HttpClientHandler { UseCookies = false }))
                {
                    var jwt = GetJWTForCurrentUser();
                    System.Diagnostics.Debug.WriteLine(jwt);
                    client.DefaultRequestHeaders.Add("Authorization", $"Bearer {jwt}");
                    response = await client.GetAsync("https://localhost:44343/api/service-c");
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