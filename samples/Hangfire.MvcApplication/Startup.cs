using System;
using Hangfire.Tags.SqlServer;
using Microsoft.Owin;
using Owin;

[assembly: OwinStartup(typeof(Hangfire.MvcApplication.Startup))]

namespace Hangfire.MvcApplication
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            GlobalConfiguration.Configuration.UseSqlServerStorage("DefaultConnection").UseTagsWithSql();

            app.UseHangfireDashboard();
            app.UseHangfireServer();
            
            RecurringJob.AddOrUpdate<Tasks>(x => x.SuccessTask(null, null),  Cron.Minutely);
            RecurringJob.AddOrUpdate<Tasks>(x => x.FailedTask(null, null), Cron.MinuteInterval(2));
        }

        private static void ThrowException()
        {
            throw new Exception("This job failes");
        }
    }
}
