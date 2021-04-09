using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Legacy.Monolith.Controllers
{
  [Authorize]
  public class ServiceBController : Controller
  {
    public ActionResult Index()
    {
      return View();
    }
  }
}