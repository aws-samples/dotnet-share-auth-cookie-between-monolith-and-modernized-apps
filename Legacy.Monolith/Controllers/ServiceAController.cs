using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Legacy.Monolith.Controllers
{
  [Authorize]
  public class ServiceAController : Controller
  {
    public ActionResult Index()
    {
      return View();
    }
  }
}