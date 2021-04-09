using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Modernized.Backend.ServiceA.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/service-a")]
    public class ServiceAController : ControllerBase
    {
        private readonly ILogger<ServiceAController> _logger;

        public ServiceAController(ILogger<ServiceAController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public async Task<string> Get()
        {
            var user = this.HttpContext.User;
            return await Task.FromResult<string>($"Hello { User.Identity.Name}, from the externally hosted Service A.");
        }
    }
}
