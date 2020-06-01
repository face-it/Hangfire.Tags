using Hangfire.MySql;
using Hangfire.Tags.Storage;

namespace Hangfire.Tags.MySql
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
        public static IGlobalConfiguration UseTagsWithMySql(this IGlobalConfiguration configuration, TagsOptions options = null, MySqlStorageOptions sqlOptions = null)
        {
            options = options ?? new TagsOptions();
            sqlOptions = sqlOptions ?? new MySqlStorageOptions();

            options.Storage = new MySqlTagsServiceStorage(sqlOptions);

            TagsServiceStorage.Current = options.Storage;

            var config = configuration.UseTags(options);
            return config;
        }
    }
}
