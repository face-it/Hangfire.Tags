using System.Collections.Generic;
using Hangfire.Storage.Monitoring;
using Hangfire.Tags.Dashboard.Monitoring;

namespace Hangfire.Tags.Storage
{
    internal interface ITagsMonitoringApi
    {
        long GetTagsCount();

        long GetJobCount(string[] tags, string stateName = null);

        IDictionary<string, int> GetJobStateCount(string[] tags, int maxTags = 50);

        IEnumerable<TagDto> SearchWeightedTags(string tag = null);

        JobList<MatchingJobDto> GetMatchingJobs(string[] tags, int from, int count, string stateName = null);
    }
}