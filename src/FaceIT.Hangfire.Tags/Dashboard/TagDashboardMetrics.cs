using Hangfire.Dashboard;
using Hangfire.Tags.Storage;

namespace Hangfire.Tags.Dashboard
{
    internal static class TagDashboardMetrics
    {
        public static readonly DashboardMetric TagsCount = new DashboardMetric("tags:count", razorPage =>
            {
                long count;
                using (var tagStorage = new TagsStorage(razorPage.Storage))
                {
                    count = tagStorage.GetTagsCount();
                }
                return new Metric(count);
            });
    }
}
