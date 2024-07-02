using Hangfire.Dashboard;

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
    }
}
