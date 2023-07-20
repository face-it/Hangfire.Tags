using System;
using Hangfire.Dashboard;
using Hangfire.Tags.Dashboard;
using Hangfire.Tags.Dashboard.Pages;
using Hangfire.Tags.States;
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
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        /// <returns></returns>
        public static IGlobalConfiguration UseTags(this IGlobalConfiguration configuration, TagsOptions options = null)
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            if (DashboardRoutes.Routes.FindDispatcher("/tags/(.*)") != null)
                throw new InvalidOperationException("Tags are already initialized");

            // Register server filter for jobs, to clean up after ourselves
            GlobalJobFilters.Filters.Add(new TagsCleanupStateFilter(), int.MaxValue);
            GlobalJobFilters.Filters.Add(new CreateJobFilter(), int.MaxValue);

            DashboardRoutes.Routes.AddRazorPage("/tags/search", x => new TagsSearchPage());
            DashboardRoutes.Routes.AddRazorPage("/tags/search/.+", x => new TagsJobsPage());
            DashboardRoutes.Routes.Add("/tags/all", new TagsDispatcher());
            DashboardRoutes.Routes.Add("/tags/([0-9a-z\\-]+)", new JobTagsDispatcher());

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
            DashboardRoutes.Routes.Append(cssPath, new DynamicCssDispatcher(options));
            return configuration;
        }

        private static MenuItem TagsMenuItemInitializer(RazorPage page)
        {
            return new MenuItem("Tags", page.Url.To("/tags/search"))
            {
                Active = page.RequestPath.StartsWith("/tags/search"),
                Metric = TagDashboardMetrics.TagsCount
            };
        }
    }
}