using System;
using System.Threading;
using System.Web.Mvc;
using Datadog.Trace;

namespace MemoryCacheSyntheticTest.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            return View(MvcApplication.CacheManager.GetStats());
        }

        public ActionResult WickedSlow()
        {
            using var scope = Tracer.Instance.StartActive("super-slow-code");
            Thread.Sleep(TimeSpan.FromSeconds(10));
            return View("Index", MvcApplication.CacheManager.GetStats());
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";
            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";
            return View();
        }
    }
}
