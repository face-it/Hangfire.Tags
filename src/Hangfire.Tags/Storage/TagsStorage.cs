using System;
using System.Collections.Generic;
using System.Linq;
using Hangfire.Storage;
using Hangfire.Storage.Monitoring;
using Hangfire.Tags.Dashboard;
using Hangfire.Tags.Dashboard.Monitoring;

namespace Hangfire.Tags.Storage
{
    internal class TagsStorage : ITagsStorage, ITagsMonitoringApi
    {
        private readonly JobStorage _jobStorage;

        public TagsStorage(JobStorage jobStorage)
        {
            _jobStorage = jobStorage;
            var connection = jobStorage.GetConnection();
            connection = connection ?? throw new ArgumentNullException(nameof(connection));

            if (!(connection is JobStorageConnection jobStorageConnection))
                throw new NotSupportedException("Storage connection must implement JobStorageConnection");

            ServiceStorage = jobStorage.FindRegistration().Item2;
            Connection = jobStorageConnection;
        }

        private ITagsServiceStorage ServiceStorage { get; }

        internal JobStorageConnection Connection { get; }

        public void Dispose()
        {
            Connection.Dispose();
        }

        public long GetTagsCount()
        {
            return Connection.GetSetCount("tags");
        }

        public long GetJobCount(string[] tags, string stateName = null)
        {
            return ServiceStorage?.GetJobCount(_jobStorage, tags.Select(t => t.GetSetKey()).ToArray(), stateName) ?? 0;
        }

        public IDictionary<string, int> GetJobStateCount(string[] tags, int maxTags = 50)
        {
            return ServiceStorage?.GetJobStateCount(_jobStorage, tags.Select(t => t.GetSetKey()).ToArray(), maxTags) ?? new Dictionary<string, int>();
        }

        public IEnumerable<TagDto> SearchWeightedTags(string tag = null)
        {
            return ServiceStorage?.SearchWeightedTags(_jobStorage, tag) ?? Enumerable.Empty<TagDto>();
        }

        public IEnumerable<string> SearchRelatedTags(string tag)
        {
            return ServiceStorage?.SearchRelatedTags(_jobStorage, tag);
        }

        public JobList<MatchingJobDto> GetMatchingJobs(string[] tags, int from, int count, string stateName = null)
        {
            return ServiceStorage?.GetMatchingJobs(_jobStorage, tags.Select(t => t.GetSetKey()).ToArray(), from, count, stateName) ??
                   new JobList<MatchingJobDto>(Enumerable.Empty<KeyValuePair<string, MatchingJobDto>>());
        }

        public ITagsMonitoringApi GetMonitoringApi()
        {
            return this;
        }

        public string[] GetTags(string jobid)
        {
            return Connection.GetAllItemsFromSet(jobid.GetSetKey()).ToArray();
        }

        public string[] GetAllTags()
        {
            return Connection.GetAllItemsFromSet("tags").ToArray();
        }

        public void InitTags(string jobid)
        {
            // No need for initialization
        }

        public void AddTag(string jobid, string tag)
        {
            using (var tran = Connection.CreateWriteTransaction())
            {
                if (!(tran is JobStorageTransaction))
                    throw new NotSupportedException(" Storage transactions must implement JobStorageTransaction");

                var cleanTag = tag.Clean();
                var score = DateTime.Now.Ticks;

                tran.AddToSet("tags", cleanTag, score);
                tran.AddToSet(jobid.GetSetKey(), cleanTag, score);
                tran.AddToSet(cleanTag.GetSetKey(), jobid, score);
                tran.Commit();
            }
        }

        public void AddTags(string jobid, IEnumerable<string> tags)
        {
            using (var tran = Connection.CreateWriteTransaction())
            {
                if (!(tran is JobStorageTransaction))
                    throw new NotSupportedException(" Storage transactions must implement JobStorageTransaction");

                foreach (var tag in tags)
                {
                    var cleanTag = tag.Clean();
                    var score = DateTime.Now.Ticks;

                    tran.AddToSet("tags", cleanTag, score); // Use a set, because it merges by default, where a list only adds
                    tran.AddToSet(jobid.GetSetKey(), cleanTag, score);
                    tran.AddToSet(cleanTag.GetSetKey(), jobid, score);
                }
                tran.Commit();
            }
        }

        public void Removetag(string jobid, string tag)
        {
            using (var tran = Connection.CreateWriteTransaction())
            {
                if (!(tran is JobStorageTransaction))
                    throw new NotSupportedException(" Storage transactions must implement JobStorageTransaction");

                var cleanTag = tag.Clean();

                tran.RemoveFromSet(jobid.GetSetKey(), cleanTag);
                tran.RemoveFromSet(cleanTag.GetSetKey(), jobid);

                if (Connection.GetSetCount(cleanTag.GetSetKey()) == 0)
                {
                    // Remove the tag, it's no longer in use
                    tran.RemoveFromSet("tags", cleanTag);
                }

                tran.Commit();
            }
        }

        public void Expire(string jobid, TimeSpan expireIn)
        {
            using (var tran = (JobStorageTransaction)Connection.CreateWriteTransaction())
            {
                using (var expiration = new TagExpirationTransaction(ServiceStorage, this, tran))
                {
                    expiration.Expire(jobid, expireIn);
                }
            }
        }
    }
}
