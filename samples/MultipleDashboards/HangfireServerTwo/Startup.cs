using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Hangfire;
using Hangfire.SqlServer;
using Hangfire.Tags;
using Hangfire.Tags.SqlServer;
using JobsInterfaces;

namespace HangfireServerTwo
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddHangfire(config => config
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings());

            string storageTwoConnectionString = "Server=.;Initial Catalog=HF2;Integrated Security=true;";
            GlobalConfiguration.Configuration
                .UseSqlServerStorage(
                    storageTwoConnectionString,
                    new SqlServerStorageOptions
                    {
                        CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
                        SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
                        QueuePollInterval = TimeSpan.Zero,
                        UseRecommendedIsolationLevel = true,
                        DisableGlobalLocks = true
                    })
                .UseTagsWithSql(new TagsOptions { TagsListStyle = TagsListStyle.Dropdown });

            services.AddHangfireServer();

            services.AddSingleton<IOtherDummyJob, OtherDummyJob>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting()
                .UseEndpoints(endpoints => { })
                .UseStaticFiles()
                .UseHangfireDashboard(
                    "/server-two",
                    new DashboardOptions { DashboardTitle = "Dashboard Two" });

            RecurringJob.AddOrUpdate<IOtherDummyJob>("job-server-two", dummyJob => dummyJob.DoJob(null), Cron.Minutely);
        }
    }
}
