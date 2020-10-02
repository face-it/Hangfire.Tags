using System;
using System.Configuration;
using Hangfire.Common;
using Hangfire.MySql;
using Hangfire.SQLite;
using Hangfire.Tags;
using Hangfire.Tags.MySql;
using Hangfire.Tags.Redis.StackExchange;
using Hangfire.Tags.SQLite;
using Hangfire.Tags.SqlServer;
using Microsoft.Owin;
using Owin;
using StackExchange.Redis;

[assembly: OwinStartup(typeof(Hangfire.MvcApplication.Startup))]

namespace Hangfire.MvcApplication
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            // GlobalConfiguration.Configuration.UseSqlServerStorage("DefaultConnection").UseTagsWithSql(new TagsOptions
            // {
            //     TagsListStyle = TagsListStyle.Dropdown
            // });

            // var mysqlConnectionString =
            //     ConfigurationManager.ConnectionStrings["DefaultMySqlConnection"].ConnectionString;
            //
            // GlobalConfiguration.Configuration.UseStorage(new MySqlStorage(mysqlConnectionString, new MySqlStorageOptions())).UseTagsWithMySql(new TagsOptions
            // {
            //     TagsListStyle = TagsListStyle.Dropdown
            // });

            // var redis = ConnectionMultiplexer.Connect(ConfigurationManager.ConnectionStrings["DefaultRedisConnection"]
            //     .ConnectionString);
            // GlobalConfiguration.Configuration.UseRedisStorage(redis).UseTagsWithRedis(new TagsOptions
            // {
            //     TagsListStyle = TagsListStyle.Dropdown
            // });

            var sqliteConnectionString =
                ConfigurationManager.ConnectionStrings["DefaultSqliteConnection"].ConnectionString;
            GlobalConfiguration.Configuration.UseSQLiteStorage(sqliteConnectionString).UseTagsWithSQLite(new TagsOptions
                {
                    TagsListStyle = TagsListStyle.Dropdown
                });

            app.UseHangfireDashboard();
            app.UseHangfireServer();

            RecurringJob.AddOrUpdate<Tasks>(x => x.SuccessTask(null, null), Cron.Minutely);

            var recurringJobs = new RecurringJobManager();
            recurringJobs.AddOrUpdate("Failed Task", Job.FromExpression<Tasks>(x => x.FailedTask(null, null)), "*/2 * * * *", TimeZoneInfo.Local);
        }

        private static void ThrowException()
        {
            throw new Exception("This job failes");
        }
    }
}
