using Hangfire.Server;
using Microsoft.Extensions.DependencyInjection;

namespace Hangfire.Tags.Redis.StackExchange
{
    public static class HangfireTagsRedisStackExchangeRegistrationExtensions
    {
        public static IServiceCollection AddHangfireTagsRedisExpiredTagsWatcher(this IServiceCollection serviceCollection)
        {
            return serviceCollection.AddSingleton<IBackgroundProcess, ExpiredTagsWatcher>();
        }
    }
}
