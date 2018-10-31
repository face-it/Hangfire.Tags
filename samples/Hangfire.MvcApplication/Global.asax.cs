using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using Hangfire.Logging;

namespace Hangfire.MvcApplication
{
    public class MvcApplication : HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            RouteConfig.RegisterRoutes(RouteTable.Routes);

            LogProvider.SetCurrentLogProvider(new TextBufferLogProvider());
            TextBuffer.WriteLine("Application started.");
        }
    }
}
