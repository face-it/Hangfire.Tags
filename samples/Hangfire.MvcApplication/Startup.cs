using Microsoft.Owin;
using Owin;

[assembly: OwinStartup(typeof(Hangfire.MvcApplication.Startup))]

namespace Hangfire.MvcApplication
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            GlobalConfiguration.Configuration.UseSqlServerStorage("DefaultConnection");

            app.UseHangfireDashboard();
            app.UseHangfireServer();
            
            RecurringJob.AddOrUpdate(
                () => TextBuffer.WriteLine("Recurring Job completed successfully!"), 
                Cron.Minutely);
        }
    }
}
