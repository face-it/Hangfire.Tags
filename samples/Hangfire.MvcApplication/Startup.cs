using System;
using System.Configuration;
using Hangfire.Common;
using Hangfire.Mongo;
using Hangfire.Mongo.Migration.Strategies;
using Hangfire.Mongo.Migration.Strategies.Backup;
using Hangfire.MySql;
using Hangfire.PostgreSql;
using Hangfire.States;
using Hangfire.Storage;
using Hangfire.Storage.SQLite;
using Hangfire.Tags;
using Hangfire.Tags.Mongo;
using Hangfire.Tags.MySql;
using Hangfire.Tags.PostgreSql;
using Hangfire.Tags.SQLite;
using Hangfire.Tags.SqlServer;
using Microsoft.Owin;
using Owin;
using StackExchange.Redis;

[assembly: OwinStartup(typeof(Hangfire.MvcApplication.Startup))]

namespace Hangfire.MvcApplication
{
    internal enum Storage
    {
        SqlServer,
        MySql,
        PostgreSql,
        RedisStack,
        RedisPro,
        Mongo,
        Sqlite
    }

    public class ProlongExpirationTimeAttribute : JobFilterAttribute, IApplyStateFilter
    {
        public void OnStateApplied(ApplyStateContext context, IWriteOnlyTransaction transaction)
        {
            context.JobExpirationTimeout = TimeSpan.FromMinutes(1);
        }

        public void OnStateUnapplied(ApplyStateContext context, IWriteOnlyTransaction transaction)
        {
        }
    }

    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            var storage = (Storage) Enum.Parse(typeof(Storage), ConfigurationManager.AppSettings["Storage"]);

            var tagOptions = new TagsOptions
            {
                TagsListStyle = TagsListStyle.Dropdown,
                Clean = Clean.None,
                MaxTagLength = 100
            };

            switch (storage)
            {
                case Storage.SqlServer:
                    GlobalConfiguration.Configuration.UseSqlServerStorage("DefaultConnection")
                        .UseTagsWithSql(tagOptions);
                    break;

                case Storage.MySql:
                    var mysqlConnectionString =
                        ConfigurationManager.ConnectionStrings["DefaultMySqlConnection"].ConnectionString;

                    GlobalConfiguration.Configuration
                        .UseStorage(new MySqlStorage(mysqlConnectionString, new MySqlStorageOptions()))
                        .UseTagsWithMySql(tagOptions);
                    break;
                case Storage.PostgreSql:
                    GlobalConfiguration.Configuration.UsePostgreSqlStorage(
                        ConfigurationManager.ConnectionStrings["PostgreSqlConnection"].ConnectionString,
                        new PostgreSqlStorageOptions
                        {
                            JobExpirationCheckInterval = TimeSpan.FromSeconds(15),
                            QueuePollInterval = TimeSpan.FromTicks(1) // To reduce processing delays to minimum
                        });
                    GlobalConfiguration.Configuration.UseTagsWithPostgreSql(tagOptions);
                    break;
                case Storage.RedisStack:
                {
                    var redis = ConnectionMultiplexer.Connect(ConfigurationManager
                        .ConnectionStrings["DefaultRedisConnection"]
                        .ConnectionString);

                    Tags.Redis.StackExchange.GlobalConfigurationExtensions.UseTagsWithRedis(
                        Redis.StackExchange.RedisStorageExtensions.UseRedisStorage(GlobalConfiguration.Configuration,
                            redis), tagOptions);
                    break;
                }
                case Storage.RedisPro:
                {
                    var redis = ConfigurationManager
                        .ConnectionStrings["DefaultRedisConnection"]
                        .ConnectionString;

                    Tags.Pro.Redis.GlobalConfigurationExtensions.UseTagsWithRedis(
                        RedisStorageGlobalConfigurationExtensions.UseRedisStorage(GlobalConfiguration.Configuration,
                            redis), tagOptions);
                    break;
                }
                case Storage.Sqlite:
                    var sqliteConnectionString =
                        ConfigurationManager.ConnectionStrings["DefaultSqliteConnection"].ConnectionString;
                    GlobalConfiguration.Configuration.UseSQLiteStorage(sqliteConnectionString)
                        .UseTagsWithSQLite(tagOptions);
                    break;

                case Storage.Mongo:
                    var options = new MongoStorageOptions
                    {
                        MigrationOptions = new MongoMigrationOptions
                        {
                            MigrationStrategy = new DropMongoMigrationStrategy(),
                            BackupStrategy = new NoneMongoBackupStrategy()
                        }
                    };
                    GlobalConfiguration.Configuration
                        .UseMongoStorage(
                            ConfigurationManager.ConnectionStrings["DefaultMongoConnection"].ConnectionString,
                            options)
                        .UseTagsWithMongo(tagOptions, options);
                    break;
            }

            app.UseHangfireDashboard("/hangfire", new DashboardOptions { DarkModeEnabled = false });
            app.UseHangfireServer();

            GlobalConfiguration.Configuration.UseFilter(new ProlongExpirationTimeAttribute());

            RecurringJob.AddOrUpdate<Tasks>("Success Task", x => x.SuccessTask(null, null), Cron.Minutely);

            var recurringJobs = new RecurringJobManager();
            recurringJobs.AddOrUpdate("Failed Task", Job.FromExpression<Tasks>(x => x.FailedTask(null, null)),
                "*/2 * * * *", new RecurringJobOptions { TimeZone = TimeZoneInfo.Local });
        }

        private static void ThrowException()
        {
            throw new Exception("This job failes");
        }
    }
}
