using Hangfire.SQLite;
using Hangfire.Tags.Dashboard;

namespace Hangfire.Tags.SQLite
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
        /// <param name="sqlOptions">Options for sql storage</param>
        /// <param name="jobStorage">The jobStorage for which this configuration is used.</param>
        /// <returns></returns>
        public static IGlobalConfiguration UseTagsWithSQLite(this IGlobalConfiguration configuration, TagsOptions options = null, SQLiteStorageOptions sqlOptions = null, JobStorage jobStorage = null)
        {
            options = options ?? new TagsOptions();
            sqlOptions = sqlOptions ?? new SQLiteStorageOptions();

            var storage = new SQLiteTagsServiceStorage(sqlOptions);
            (jobStorage ?? JobStorage.Current).Register(options, storage);

            var config = configuration.UseTags(options);
            return config;
        }
    }
}
