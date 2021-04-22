using System;
using System.Collections.Generic;
using System.Linq;
using Hangfire.Storage;
using Hangfire.Storage.Monitoring;
using Hangfire.Tags.Dashboard.Monitoring;

namespace Hangfire.Tags.Storage
{
    internal class TagsStorage : ITagsStorage, ITagsMonitoringApi
    {
        private readonly JobStorage _jobStorage;

        public TagsStorage(JobStorage jobStorage)
        {
            var connection = jobStorage.GetConnection();
            connection = connection ?? throw new ArgumentNullException(nameof(connection));

            if (!(connection is JobStorageConnection jobStorageConnection))
                throw new NotSupportedException("Storage connection must implement JobStorageConnection");

            Connection = jobStorageConnection;

            _jobStorage = jobStorage;
        }

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
            if (TagsOptions.OptionsDictionary.TryGetValue(this._jobStorage.ToString(), out var options))
                return options.Storage?.GetJobCount(tags.Select(t => t.GetSetKey()).ToArray(), stateName) ?? 0;

            return TagsOptions.Options.Storage?.GetJobCount(tags.Select(t => t.GetSetKey()).ToArray(), stateName) ?? 0;
        }

        public IDictionary<string, int> GetJobStateCount(string[] tags, int maxTags = 50)
        {
            if (TagsOptions.OptionsDictionary.TryGetValue(this._jobStorage.ToString(), out var options))
                return options.Storage?.GetJobStateCount(tags.Select(t => t.GetSetKey()).ToArray(), maxTags) ?? new Dictionary<string, int>();

            return TagsOptions.Options.Storage?.GetJobStateCount(tags.Select(t => t.GetSetKey()).ToArray(), maxTags) ?? new Dictionary<string, int>();
        }

        public IEnumerable<TagDto> SearchWeightedTags(string tag = null)
        {
            if (TagsOptions.OptionsDictionary.TryGetValue(this._jobStorage.ToString(), out var options))
                return options.Storage?.SearchWeightedTags(tag) ?? Enumerable.Empty<TagDto>();

            return TagsOptions.Options.Storage?.SearchWeightedTags(tag) ?? Enumerable.Empty<TagDto>();
        }

        public IEnumerable<string> SearchRelatedTags(string tag)
        {
            if (TagsOptions.OptionsDictionary.TryGetValue(this._jobStorage.ToString(), out var options))
                return options.Storage?.SearchRelatedTags(tag);

            return TagsOptions.Options.Storage?.SearchRelatedTags(tag);
        }

        public JobList<MatchingJobDto> GetMatchingJobs(string[] tags, int from, int count, string stateName = null)
        {
            if (TagsOptions.OptionsDictionary.TryGetValue(this._jobStorage.ToString(), out var options))
                return options.Storage?.GetMatchingJobs(tags.Select(t => t.GetSetKey()).ToArray(), from, count, stateName) ??
                       new JobList<MatchingJobDto>(Enumerable.Empty<KeyValuePair<string, MatchingJobDto>>());

            return TagsOptions.Options.Storage?.GetMatchingJobs(tags.Select(t => t.GetSetKey()).ToArray(), from, count, stateName) ??
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
                using (var expiration = new TagExpirationTransaction(this, tran))
                {
                    expiration.Expire(jobid, expireIn);
                }
            }
        }
    }
}
