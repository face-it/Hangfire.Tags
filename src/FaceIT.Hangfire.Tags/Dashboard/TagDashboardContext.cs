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

        public TagsOptions Options => StorageRegistration.FindRegistration(_context.Storage).Item1;

        public ITagsServiceStorage TagsServiceStorage => StorageRegistration.FindRegistration(_context.Storage).Item2;
    }
}
