using Hangfire.Mongo;
using Hangfire.Tags.Dashboard;

namespace Hangfire.Tags.Mongo
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
        /// <param name="mongoOptions">Options for sql storage</param>
        /// <param name="jobStorage">The jobStorage for which this configuration is used.</param>
        /// <returns></returns>
        public static IGlobalConfiguration UseTagsWithMongo(this IGlobalConfiguration configuration,
            TagsOptions options = null, MongoStorageOptions mongoOptions = null, JobStorage jobStorage = null)
        {
            options = options ?? new TagsOptions();
            if (options.MaxTagLength == null)
                options.MaxTagLength = 100; // The maximum length in the Hangfire.Set table of the [Key] column

            mongoOptions = mongoOptions ?? new MongoStorageOptions();

            var storage = new MongoTagsServiceStorage(mongoOptions);
            (jobStorage ?? JobStorage.Current).Register(options, storage);

            var config = configuration.UseTags(options);
            return config;
        }
    }
}
