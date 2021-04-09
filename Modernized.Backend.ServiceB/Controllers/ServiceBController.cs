using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Modernized.Backend.ServiceB.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/service-b")]
    public class ServiceBController : ControllerBase
    {
        private readonly ILogger<ServiceBController> _logger;

        public ServiceBController(ILogger<ServiceBController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public async Task<string> Get()
        {
            var user = this.HttpContext.User;
            return await Task.FromResult<string>($"Hello { User.Identity.Name}, from the externally hosted Service B.");
        }
    }
}
