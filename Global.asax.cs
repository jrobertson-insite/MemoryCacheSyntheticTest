using System;
using System.ComponentModel.DataAnnotations;
using System.Configuration;
using System.Threading.Tasks;
using System.Web.Hosting;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using Serilog;
using Serilog.Formatting.Compact;

namespace MemoryCacheSyntheticTest
{
    public class MvcApplication : System.Web.HttpApplication
    {
        public static MemoryCacheManager CacheManager { get; set; }

        protected void Application_Start()
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console()
                .WriteTo.File(new CompactJsonFormatter (), HostingEnvironment.MapPath("~/App_Log/log.log"), flushToDiskInterval: TimeSpan.FromSeconds(5))
                .CreateLogger();

            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);

            CacheManager = new MemoryCacheManager();
            Task.Factory.StartNew(() => { CacheManager.BlowItUp(); });
        }
    }
}
