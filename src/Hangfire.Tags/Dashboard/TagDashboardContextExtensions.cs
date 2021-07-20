using Hangfire.Dashboard;
using Hangfire.Tags.Storage;

namespace Hangfire.Tags.Dashboard
{
    public static class TagDashboardContextExtensions
    {
        public static TagDashboardContext ToTagDashboardContext(this DashboardContext context)
        {
            return new TagDashboardContext(context);
        }

        public static void Register(this JobStorage jobStorage, TagsOptions options, ITagsServiceStorage serviceStorage)
        {
            StorageRegistration.Register(jobStorage, options, serviceStorage);
        }
    }
}