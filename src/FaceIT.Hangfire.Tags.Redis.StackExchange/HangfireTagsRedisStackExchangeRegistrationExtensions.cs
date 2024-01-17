using Hangfire.Redis.StackExchange;
using Hangfire.Server;
using Hangfire.Tags.Dashboard;
using Microsoft.Extensions.DependencyInjection;

namespace Hangfire.Tags.Redis.StackExchange
{
    public static class HangfireTagsRedisStackExchangeRegistrationExtensions
    {
        public static IServiceCollection AddHangfireTagsRedisExpiredTagsWatcher(this IServiceCollection serviceCollection)
        {
            return serviceCollection.AddSingleton<IBackgroundProcess>(x =>
                new ExpiredTagsWatcher(
                    x.GetService<RedisStorage>(),
                    x.GetService<RedisTagsServiceStorage>(),
                    x.GetService<RedisStorageOptions>().ExpiryCheckInterval)
                );
        }
        public static IServiceCollection AddHangfireTagsRedisStackExchange(this IServiceCollection serviceCollection, TagsOptions options = null, RedisStorageOptions redisOptions = null, RedisStorage jobStorage = null)
        {
            options ??= new TagsOptions();
            serviceCollection.AddSingleton(options);

            redisOptions ??= new RedisStorageOptions();
            serviceCollection.AddSingleton(redisOptions);
            jobStorage ??= new RedisStorage();
            serviceCollection.AddSingleton(jobStorage);

            var storage = new RedisTagsServiceStorage(redisOptions);
            serviceCollection.AddSingleton(storage);

            jobStorage.Register(options, storage);

            serviceCollection.AddSingleton<IBackgroundProcess>(x =>
                new ExpiredTagsWatcher(
                    x.GetService<RedisStorage>(),
                    x.GetService<RedisTagsServiceStorage>(),
                    x.GetService<RedisStorageOptions>().ExpiryCheckInterval)
                );

            return serviceCollection;

        }
    }
}
