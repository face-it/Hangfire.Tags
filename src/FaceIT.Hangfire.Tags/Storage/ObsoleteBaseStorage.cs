using System.Collections.Generic;
using System.Linq;
using Hangfire.Storage;
using Hangfire.Storage.Monitoring;
using Hangfire.Tags.Dashboard.Monitoring;

namespace Hangfire.Tags.Storage
{
    public abstract class ObsoleteBaseStorage : ITagsServiceStorage
    {
        public abstract ITagsTransaction GetTransaction(IWriteOnlyTransaction transaction);

        public IEnumerable<TagDto> SearchWeightedTags(string tag = null, string setKey = IdExtensions.SetKey)
        {
            return SearchWeightedTags(JobStorage.Current, tag, setKey);
        }

        public abstract IEnumerable<TagDto> SearchWeightedTags(JobStorage jobStorage, string tag = null,
            string setKey = IdExtensions.SetKey);

        public IEnumerable<string> SearchRelatedTags(string tag, string setKey)
        {
            return SearchRelatedTags(JobStorage.Current, tag, setKey);
        }

        public abstract IEnumerable<string> SearchRelatedTags(JobStorage jobStorage, string tag,
            string setKey = IdExtensions.SetKey);

        public int GetJobCount(string[] tags, string stateName = null)
        {
            return GetJobCount(JobStorage.Current, tags, stateName);
        }

        public abstract int GetJobCount(JobStorage jobStorage, string[] tags, string stateName = null);

        public IDictionary<string, int> GetJobStateCount(string[] tags, int maxTags = 50)
        {
            return GetJobStateCount(JobStorage.Current, tags, maxTags);
        }

        public abstract IDictionary<string, int> GetJobStateCount(JobStorage jobStorage, string[] tags,
            int maxTags = 50);

        public JobList<MatchingJobDto> GetMatchingJobs(string[] tags, int @from, int count, string stateName = null)
        {
            return GetMatchingJobs(JobStorage.Current, tags, from, count, stateName);
        }

        public abstract JobList<MatchingJobDto> GetMatchingJobs(JobStorage jobStorage, string[] tags, int from,
            int count, string stateName = null);

        public virtual string[] GetTags(JobStorage jobStorage, string jobId)
        {
            return ((JobStorageConnection) jobStorage.GetConnection()).GetAllItemsFromSet(jobId.GetSetKey()).ToArray();
        }

        public virtual long GetTagCount(JobStorage jobStorage, string setKey = "tags")
        {
            return ((JobStorageConnection) jobStorage.GetConnection()).GetSetCount(setKey);
        }
    }
}
