using Hangfire.Dashboard;

namespace Hangfire.Tags.Dashboard
{
    public abstract class TagsRazorPage : RazorPage
    {
        private TagDashboardContext _tagContext;

        private TagDashboardContext TagContext => _tagContext ?? (_tagContext = Context.ToTagDashboardContext());

        protected TagsOptions Options => TagContext.Options;
    }
}
