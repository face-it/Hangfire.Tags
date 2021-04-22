using System;
using Hangfire.Dashboard;
using Hangfire.Tags.Dashboard;
using Hangfire.Tags.Dashboard.Pages;
using Hangfire.Tags.States;
using Hangfire.Tags.Storage;
using Hangfire.Tags.Support;

namespace Hangfire.Tags
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
        /// <param name="jobStorage">Job storage</param>
        /// <returns></returns>
        public static IGlobalConfiguration UseTags(this IGlobalConfiguration configuration, TagsOptions options = null, JobStorage jobStorage = null)
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            TagsOptions.Options = options ?? new TagsOptions { Storage = JobStorage.Current as ITagsServiceStorage };

            if (options != null && jobStorage != null)
            {
                TagsOptions.OptionsDictionary.Add(jobStorage.ToString(), options);
            }

            if (TagsOptions.Options.Storage == null)
            {
                throw new ApplicationException("The specified storage is not suitable for use with tags");
            }

            if (DashboardRoutes.Routes.FindDispatcher("/tags/(.*)") != null)
                throw new InvalidOperationException("Tags are already initialized");

            // Register server filter for jobs, to clean up after ourselves
            GlobalJobFilters.Filters.Add(new TagsCleanupStateFilter(), int.MaxValue);
            GlobalJobFilters.Filters.Add(new CreateJobFilter(), int.MaxValue);

            DashboardRoutes.Routes.AddRazorPage("/tags/search", x => new TagsSearchPage());
            DashboardRoutes.Routes.AddRazorPage("/tags/search/.+", x => new TagsJobsPage());
            DashboardRoutes.Routes.Add("/tags/all", new TagsDispatcher(TagsOptions.Options));
            DashboardRoutes.Routes.Add("/tags/([0-9a-z\\-]+)", new JobTagsDispatcher(TagsOptions.Options));

            DashboardMetrics.AddMetric(TagDashboardMetrics.TagsCount);

            if (!JobsSidebarMenu.Items.Contains(TagsMenuItemInitializer))
            {
                JobsSidebarMenu.Items.Add(TagsMenuItemInitializer);
            }

            var assembly = typeof(GlobalConfigurationExtensions).Assembly;

            var jsPath = DashboardRoutes.Routes.Contains("/js[0-9]+") ? "/js[0-9]+" : "/js[0-9]{3}";
            DashboardRoutes.Routes.Append(jsPath, new EmbeddedResourceDispatcher(assembly, "Hangfire.Tags.Resources.jquery.tagcloud.js"));
            DashboardRoutes.Routes.Append(jsPath, new EmbeddedResourceDispatcher(assembly, "Hangfire.Tags.Resources.script.js"));

            var cssPath = DashboardRoutes.Routes.Contains("/css[0-9]+") ? "/css[0-9]+" : "/css[0-9]{3}";
            DashboardRoutes.Routes.Append(cssPath, new EmbeddedResourceDispatcher(assembly, "Hangfire.Tags.Resources.style.css"));
            DashboardRoutes.Routes.Append(cssPath, new DynamicCssDispatcher(TagsOptions.Options));
            return configuration;
        }

        private static MenuItem TagsMenuItemInitializer(RazorPage page)
        {
            return new MenuItem("Tags", page.Url.To($"/tags/search"))
            {
                Active = page.RequestPath.StartsWith("/tags/search"),
                Metric = TagDashboardMetrics.TagsCount
            };
        }
    }
}