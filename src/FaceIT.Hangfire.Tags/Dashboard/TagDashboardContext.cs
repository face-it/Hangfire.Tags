using Hangfire.Dashboard;
using Hangfire.Tags.Storage;

namespace Hangfire.Tags.Dashboard
{
    public class TagDashboardContext
    {
        private readonly DashboardContext _context;

        internal TagDashboardContext(DashboardContext context)
        {
            _context = context;
        }

        public TagsOptions Options => _context.Storage.FindRegistration().Item1;

        public ITagsServiceStorage TagsServiceStorage => _context.Storage.FindRegistration().Item2;
    }
}
