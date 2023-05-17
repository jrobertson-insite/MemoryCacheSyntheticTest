using System.Web.Mvc;

namespace MemoryCacheSyntheticTest.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            return View(MvcApplication.CacheManager.GetStats());
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
