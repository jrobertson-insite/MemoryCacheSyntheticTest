using System.Threading.Tasks;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;

namespace MemoryCacheSyntheticTest
{
    public class MvcApplication : System.Web.HttpApplication
    {
        public static MemoryCacheManager CacheManager { get; set; } = new();

        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);

            Task.Factory.StartNew(() => { CacheManager.BlowItUp(); });
        }
    }
}
