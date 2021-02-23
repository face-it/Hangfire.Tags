using System;
using System.IO;
using Hangfire.Common;
using Hangfire.Dashboard;
using Hangfire.Heartbeat;
using Hangfire.MySql; //used with MySql Sample
using Hangfire.PostgreSql; //used with postgreSql Sample
using Hangfire.SqlServer; //used with SqlServer Sample
using Hangfire.Tags;
using Hangfire.Tags.SqlServer;//used with SqlServer Sample
using Hangfire.Tags.MySql; //used with MySql Sample
using Hangfire.Tags.PostgreSql; //used with postgreSql Sample
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Transactions;
using Hangfire.Core.MvcApplication.Jobs;
using Hangfire.SQLite;
using Hangfire.Tags.Pro.Redis;
// using Hangfire.Tags.Pro.Redis;
// using Hangfire.Tags.Redis.StackExchange;
using Hangfire.Tags.SQLite;
using Microsoft.Extensions.Hosting;
using StackExchange.Redis;

namespace Hangfire.Core.MvcApplication
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<CookiePolicyOptions>(options =>
            {
                // This lambda determines whether user consent for non-essential cookies is needed for a given request.
                options.CheckConsentNeeded = context => true;
                options.MinimumSameSitePolicy = SameSiteMode.None;
            });

            services.AddHangfire(config =>
            {
                //SqlServer Sample
                // config.UseSqlServerStorage(Configuration.GetConnectionString("DefaultConnection"), new SqlServerStorageOptions
                // {
                //     SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5), // To enable Sliding invisibility fetching
                //     CommandBatchMaxTimeout = TimeSpan.FromMinutes(5), // To enable command pipelining
                //     QueuePollInterval = TimeSpan.FromTicks(1) // To reduce processing delays to minimum
                // });
                // var options = new TagsOptions
                // {
                //     TagsListStyle = TagsListStyle.Dropdown
                // };
                // config.UseTagsWithSql(options);
                //end SqlServer Sample

                //MySql Sample
                //var mySqlOptions = new MySqlStorageOptions
                //{
                //    TransactionIsolationLevel = IsolationLevel.ReadCommitted,
                //    QueuePollInterval = TimeSpan.FromSeconds(15),
                //    JobExpirationCheckInterval = TimeSpan.FromHours(1),
                //    CountersAggregateInterval = TimeSpan.FromMinutes(5),
                //    PrepareSchemaIfNecessary = true,
                //    DashboardJobListLimit = 50000,
                //    TransactionTimeout = TimeSpan.FromMinutes(1),
                //    TablesPrefix = "hangfire"
                //};
                //config.UseStorage(new MySqlStorage(Configuration.GetConnectionString("MySqlConnection"), mySqlOptions));
                //var options = new TagsOptions()
                //{
                //    TagsListStyle = TagsListStyle.Dropdown
                //};
                //config.UseTagsWithMySql(options,mySqlOptions);
                //end MySql Sample

                //postgreSql Sample
                // config.UsePostgreSqlStorage(Configuration.GetConnectionString("PostgreSqlConnection"), new PostgreSqlStorageOptions
                // {
                //     QueuePollInterval = TimeSpan.FromTicks(1) // To reduce processing delays to minimum
                // });
                // var options = new TagsOptions
                // {
                //     TagsListStyle = TagsListStyle.Dropdown
                // };
                // config.UseTagsWithPostgreSql(options);
                //end postgreSql Sample

                //redis sample
                // var redis = ConnectionMultiplexer.Connect(Configuration.GetConnectionString("RedisConnection"));
                // config.UseRedisStorage(redis)
                //     .UseTagsWithRedis(new TagsOptions {TagsListStyle = TagsListStyle.Dropdown});

                // redis pro sample
                config.UseRedisStorage(Configuration.GetConnectionString("RedisConnection"))
                    .UseTagsWithRedis(new TagsOptions {TagsListStyle = TagsListStyle.Dropdown});

                // config.UseSQLiteStorage(Configuration.GetConnectionString("SQLiteConnection")).UseTagsWithSQLite(
                //     new TagsOptions
                //     {
                //         TagsListStyle = TagsListStyle.Dropdown
                //     });

                //config.UseNLogLogProvider();
                config.UseHeartbeatPage(checkInterval: TimeSpan.FromSeconds(5));
            });

            services.AddMvc();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            app.UseHangfireServer();
            app.UseHangfireDashboard();

            app.UseStaticFiles();
            app.UseCookiePolicy();

            app.UseRouting();
            app.UseEndpoints(routes => routes.MapDefaultControllerRoute());

            var recurringJobs = new RecurringJobManager();

            RecurringJob.AddOrUpdate<Tasks>(x => x.SuccessTask(null, null), Cron.Minutely);
            //            RecurringJob.AddOrUpdate<Tasks>(x => x.FailedTask(null, null), "*/2 * * * *");
            recurringJobs.AddOrUpdate("Failed Task", Job.FromExpression<Tasks>(x => x.FailedTask(null)), "*/2 * * * *", TimeZoneInfo.Local);

            BackgroundJob.Enqueue<BaseJob>(x => x.Run());
            BackgroundJob.Enqueue<DerivedJob>(x => x.Run());
        }
    }
}
