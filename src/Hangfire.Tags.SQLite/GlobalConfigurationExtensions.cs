using Hangfire.SQLite;
using Hangfire.Tags.Storage;

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
        /// <returns></returns>
        public static IGlobalConfiguration UseTagsWithSQLite(this IGlobalConfiguration configuration, TagsOptions options = null, SQLiteStorageOptions sqlOptions = null)
        {
            options = options ?? new TagsOptions();
            sqlOptions = sqlOptions ?? new SQLiteStorageOptions();

            options.Storage = new SQLiteTagsServiceStorage(sqlOptions);

            TagsServiceStorage.Current = options.Storage;

            var config = configuration.UseTags(options);
            return config;
        }
    }
}
