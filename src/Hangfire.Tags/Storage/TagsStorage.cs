using System;
using System.Collections.Generic;
using System.Linq;
using Hangfire.Storage;
using Hangfire.Storage.Monitoring;
using Hangfire.Tags.Dashboard.Monitoring;

namespace Hangfire.Tags.Storage
{
    internal class TagsStorage : ITagsStorage, ITagsMonitoringApi, IDisposable
    {
        private readonly JobStorageConnection _connection;

        public TagsStorage(JobStorage jobStorage)
        {
            var connection = jobStorage.GetConnection();
            connection = connection ?? throw new ArgumentNullException(nameof(connection));

            if (!(connection is JobStorageConnection jobStorageConnection))
                throw new NotSupportedException("Storage connection must implement JobStorageConnection");

            _connection = jobStorageConnection;
        }

        public void Dispose()
        {
            _connection.Dispose();
        }

        public long GetTagsCount()
        {
            return _connection.GetSetCount("tags");
        }

        public long GetJobCount(string[] tags, string stateName = null)
        {
            return TagsOptions.Options.Storage?.GetJobCount(tags.Select(t => t.GetSetKey()).ToArray(), stateName) ?? 0;
        }

        public IDictionary<string, int> GetJobStateCount(string[] tags, int maxTags = 50)
        {
            return TagsOptions.Options.Storage?.GetJobStateCount(tags.Select(t => t.GetSetKey()).ToArray(), maxTags) ??
                   new Dictionary<string, int>();
        }

        public IEnumerable<TagDto> SearchWeightedTags(string tag = null)
        {
            return TagsOptions.Options.Storage?.SearchWeightedTags(tag) ?? Enumerable.Empty<TagDto>();
        }

        public JobList<MatchingJobDto> GetMatchingJobs(string[] tags, int from, int count, string stateName = null)
        {
            return TagsOptions.Options.Storage?.GetMatchingJobs(tags.Select(t => t.GetSetKey()).ToArray(), from, count,
                       stateName) ??
                   new JobList<MatchingJobDto>(Enumerable.Empty<KeyValuePair<string, MatchingJobDto>>());
        }

        public ITagsMonitoringApi GetMonitoringApi()
        {
            return this;
        }

        public string[] GetTags(string jobid)
        {
            return _connection.GetAllItemsFromSet(jobid.GetSetKey()).ToArray();
        }

        public string[] GetAllTags()
        {
            return _connection.GetAllItemsFromSet("tags").ToArray();
        }

        public void InitTags(string jobid)
        {
            // No need for initialization
        }

        public void AddTag(string jobid, string tag)
        {
            using (var tran = _connection.CreateWriteTransaction())
            {
                if (!(tran is JobStorageTransaction))
                    throw new NotSupportedException(" Storage transactions must impelement JobStorageTransaction");

                var cleanTag = tag.Clean();

                tran.AddToSet("tags", cleanTag);
                tran.AddToSet(jobid.GetSetKey(), cleanTag);
                tran.AddToSet(cleanTag.GetSetKey(), jobid);
                tran.Commit();
            }
        }

        public void AddTags(string jobid, IEnumerable<string> tags)
        {
            using (var tran = _connection.CreateWriteTransaction())
            {
                if (!(tran is JobStorageTransaction))
                    throw new NotSupportedException(" Storage transactions must impelement JobStorageTransaction");

                foreach (var tag in tags)
                {
                    var cleanTag = tag.Clean();

                    tran.AddToSet("tags", cleanTag); // Use a set, because it merges by default, where a list only adds
                    tran.AddToSet(jobid.GetSetKey(), cleanTag);
                    tran.AddToSet(cleanTag.GetSetKey(), jobid);
                }
                tran.Commit();
            }
        }

        public void Removetag(string jobid, string tag)
        {
            using (var tran = _connection.CreateWriteTransaction())
            {
                if (!(tran is JobStorageTransaction))
                    throw new NotSupportedException(" Storage transactions must impelement JobStorageTransaction");

                var cleanTag = tag.Clean();

                tran.RemoveFromSet(jobid.GetSetKey(), cleanTag);
                tran.RemoveFromSet(cleanTag.GetSetKey(), jobid);

                if (_connection.GetSetCount(cleanTag.GetSetKey()) == 0)
                {
                    // Remove the tag, it's no longer in use
                    tran.RemoveFromSet("tags", cleanTag);
                }

                tran.Commit();
            }
        }

        public void Expire(string jobid, TimeSpan expireIn)
        {
            using (var tran = (JobStorageTransaction) _connection.CreateWriteTransaction())
            {
                using (var expiration = new TagExpirationTransaction(this, tran))
                {
                    expiration.Expire(jobid, expireIn);
                }
            }
        }
    }
}
