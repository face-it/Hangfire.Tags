using Hangfire.Storage.Monitoring;
using Hangfire.Tags.Dashboard.Monitoring;

namespace Hangfire.Tags.Storage
{
    internal interface ITagsMonitoringApi
    {
        long GetTagsCount();

        long GetJobCount(string tag);

        JobList<MatchingJobDto> GetMatchingJobs(string tag, int from, int count);
    }
}