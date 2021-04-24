using Hangfire;
using Hangfire.SqlServer;
using Hangfire.Tags;
using Hangfire.Tags.SqlServer;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace MultipleDashboards
{

    public class Startup
    {
        const string StorageOneConnectionString =
            "Server=.;Initial Catalog=HF1;Integrated Security=true;";

        const string StorageTwoConnectionString =
            "Server=.;Initial Catalog=HF2;Integrated Security=true;";

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            // NOTE: In this case tags will be displayed from the second storage on both dashboards. However two storages registered.
            services.AddHangfire(config => config
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UseSqlServerStorage(StorageOneConnectionString)
                .UseSqlServerStorage(StorageTwoConnectionString)
                .UseTagsWithSql(new TagsOptions { TagsListStyle = TagsListStyle.Dropdown }));
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            var dashboardOneOptions = new DashboardOptions {DashboardTitle = "Dashboard One"};
            var dashboardTwoOptions = new DashboardOptions {DashboardTitle = "Dashboard Two"};

            var dashboardOneStorage = new SqlServerStorage(StorageOneConnectionString);
            var dashboardTwoStorage = new SqlServerStorage(StorageTwoConnectionString);

            app.UseRouting()
                .UseEndpoints(endpoints => { })
                .UseDefaultFiles()
                .UseStaticFiles()

                .UseHangfireDashboard("/dashboard-one", dashboardOneOptions, dashboardOneStorage)
                .UseHangfireDashboard("/dashboard-two", dashboardTwoOptions, dashboardTwoStorage);
        }
    }
}
