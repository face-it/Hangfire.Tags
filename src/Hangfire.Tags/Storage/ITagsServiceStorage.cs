using Hangfire.Storage;
using Hangfire.Storage.Monitoring;
using Hangfire.Tags.Dashboard.Monitoring;

namespace Hangfire.Tags.Storage
{
    public interface ITagsServiceStorage
    {
        ITagsTransaction GetTransaction(IWriteOnlyTransaction transaction);

        JobList<MatchingJobDto> GetMatchingJobs(string tag, int from, int count);
    }
}
