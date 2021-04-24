using Hangfire.Pro.Redis;
using Hangfire.Tags.Dashboard;
namespace Hangfire.Tags.Pro.Redis
{
    /// <summary>
    /// Provides extension methods to setup Hangfire.Tags
    /// </summary>
    public static class GlobalConfigurationExtensions
    {
        /// <summary>
        /// Configures Hangfire to use Tags.
        /// </summary>
        /// <param name="configuration">Global configuration</param>
        /// <param name="options">Options for tags</param>
        /// <param name="redisOptions">Options for Redis storage</param>
        /// <param name="jobStorage">The jobStorage for which this configuration is used.</param>
        /// <returns></returns>
        public static IGlobalConfiguration UseTagsWithRedis(this IGlobalConfiguration configuration, TagsOptions options = null, RedisStorageOptions redisOptions = null, JobStorage jobStorage = null)
        {
            options = options ?? new TagsOptions();
            redisOptions = redisOptions ?? new RedisStorageOptions();

            var storage = new RedisTagsServiceStorage(redisOptions);
            (jobStorage ?? JobStorage.Current).Register(options, storage);

            var config = configuration.UseTags(options).UseFilter(new RedisStateFilter(storage));
            return config;
        }
    }
}
