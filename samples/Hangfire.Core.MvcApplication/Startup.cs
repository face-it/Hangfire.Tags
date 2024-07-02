using System;
using Hangfire.Common;
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
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Transactions;
using Hangfire.Core.MvcApplication.Jobs;
using Hangfire.Mongo;
using Hangfire.Mongo.Migration.Strategies;
using Hangfire.Mongo.Migration.Strategies.Backup;
using Microsoft.Extensions.Hosting;
using StackExchange.Redis;
using Hangfire.States;
using Hangfire.Storage;
using Hangfire.Storage.SQLite;
using Hangfire.Tags.Mongo;
using Hangfire.Tags.SQLite;
using Microsoft.Data.Sqlite;
using MongoDB.Driver;
using SQLite;

namespace Hangfire.Core.MvcApplication
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
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        private IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            var storage = Configuration.GetValue<Storage>("Storage");
            
            services.Configure<CookiePolicyOptions>(options =>
            {
                // This lambda determines whether user consent for non-essential cookies is needed for a given request.
                options.CheckConsentNeeded = context => true;
                options.MinimumSameSitePolicy = SameSiteMode.None;
            });

            services.AddHangfireServer();
            services.AddHangfire(config =>
            {
                var tagOptions = new TagsOptions
                {
                    TagsListStyle = TagsListStyle.Dropdown,
                    Clean = Clean.None
                };
                
                switch (storage)
                {
                    case Storage.SqlServer:
                    {
                        //SqlServer Sample
                        config.UseSqlServerStorage(Configuration.GetConnectionString("DefaultConnection"),
                            new SqlServerStorageOptions
                            {
                                JobExpirationCheckInterval = TimeSpan.FromSeconds(15),
                                SlidingInvisibilityTimeout =
                                    TimeSpan.FromMinutes(5), // To enable Sliding invisibility fetching
                                CommandBatchMaxTimeout = TimeSpan.FromMinutes(5), // To enable command pipelining
                                QueuePollInterval = TimeSpan.FromTicks(1) // To reduce processing delays to minimum
                            });
                        config.UseTagsWithSql(tagOptions);
                        //end SqlServer Sample
                        break;
                    }
                    case Storage.MySql:
                    {
                        //MySql Sample
                        var mySqlOptions = new MySqlStorageOptions
                        {
                            TransactionIsolationLevel = IsolationLevel.ReadCommitted,
                            QueuePollInterval = TimeSpan.FromSeconds(15),
                            JobExpirationCheckInterval = TimeSpan.FromSeconds(15),
                            CountersAggregateInterval = TimeSpan.FromMinutes(5),
                            PrepareSchemaIfNecessary = true,
                            DashboardJobListLimit = 50000,
                            TransactionTimeout = TimeSpan.FromMinutes(1),
                            TablesPrefix = "hangfire"
                        };
                        config.UseStorage(new MySqlStorage(Configuration.GetConnectionString("MySqlConnection"),
                            mySqlOptions));
                        config.UseTagsWithMySql(tagOptions, mySqlOptions);
                        //end MySql Sample
                        break;
                    }
                    case Storage.PostgreSql:
                    {
                        //postgreSql Sample
                        config.UsePostgreSqlStorage(Configuration.GetConnectionString("PostgreSqlConnection"), new PostgreSqlStorageOptions
                        {
                            JobExpirationCheckInterval = TimeSpan.FromSeconds(15),
                            QueuePollInterval = TimeSpan.FromTicks(1) // To reduce processing delays to minimum
                        });
                        config.UseTagsWithPostgreSql(tagOptions);
                        //end postgreSql Sample
                        break;
                    }
                    case Storage.RedisStack:
                    {
                        //redis sample
                        var redis = ConnectionMultiplexer.Connect(Configuration.GetConnectionString("RedisConnection"));
                        Tags.Redis.StackExchange.GlobalConfigurationExtensions.UseTagsWithRedis(
                            Redis.StackExchange.RedisStorageExtensions.UseRedisStorage(config, redis),
                            tagOptions
                        );
                        break;
                    }
                    case Storage.RedisPro:
                        // redis pro sample
                        Tags.Pro.Redis.GlobalConfigurationExtensions.UseTagsWithRedis(
                            config.UseRedisStorage(Configuration.GetConnectionString("RedisConnection")), 
                            tagOptions
                        );
                        break;
                    case Storage.Sqlite:
                        config.UseSQLiteStorage(Configuration.GetConnectionString("SQLiteConnection"))
                            .UseTagsWithSQLite(tagOptions);
                        break;
                    case Storage.Mongo:
                    {
                        // mongo sample
                        var mongoUrlBuilder = new MongoUrlBuilder(Configuration.GetConnectionString("MongoConnection"));
                        var mongoClient = new MongoClient(mongoUrlBuilder.ToMongoUrl());

                        var mongoStorageOptions = new MongoStorageOptions
                        {
                            MigrationOptions = new MongoMigrationOptions
                            {
                                MigrationStrategy = new MigrateMongoMigrationStrategy(),
                                BackupStrategy = new CollectionMongoBackupStrategy()
                            },
                            Prefix = "hangfire.mongo",
                            CheckConnection = true
                        };
                        config.UseMongoStorage(mongoClient, mongoUrlBuilder.DatabaseName, mongoStorageOptions)
                            .UseTagsWithMongo(tagOptions, mongoStorageOptions);
                        break;
                    }
                }

                config.UseHeartbeatPage(checkInterval: TimeSpan.FromSeconds(5));
            });
            services.AddHangfireServer();

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

            app.UseHangfireDashboard();

            app.UseStaticFiles();
            app.UseCookiePolicy();

            app.UseRouting();
            app.UseEndpoints(routes => routes.MapDefaultControllerRoute());

            var recurringJobs = new RecurringJobManager();

            RecurringJob.AddOrUpdate<Tasks>("Success Task", x => x.SuccessTask(null, null), Cron.Minutely);
            //            RecurringJob.AddOrUpdate<Tasks>(x => x.FailedTask(null, null), "*/2 * * * *");
            recurringJobs.AddOrUpdate("Failed Task", Job.FromExpression<Tasks>(x => x.FailedTask(null)), "*/2 * * * *",
                new RecurringJobOptions { TimeZone = TimeZoneInfo.Local });

            BackgroundJob.Enqueue<BaseJob>(x => x.Run());
            BackgroundJob.Enqueue<DerivedJob>(x => x.Run());
        }
    }
}
