using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Hangfire.Tags.PostgreSql.Tests
{
    public class IntegrationTestBase
    {
        protected IConfiguration Configuration { get; }
        protected IServiceCollection Services { get; }

        protected IntegrationTestBase()
        {
            Configuration = new ConfigurationBuilder()
                .AddJsonFile("testsettings.json", optional: true)
                .Build();

            Services = new ServiceCollection();
        }
    }
}