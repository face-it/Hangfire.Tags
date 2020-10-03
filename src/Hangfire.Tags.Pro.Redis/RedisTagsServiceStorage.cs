using System;
using System.Collections.Generic;
using System.Linq;
using Hangfire.Common;
using Hangfire.Pro.Redis;
using Hangfire.Storage;
using Hangfire.Storage.Monitoring;
using Hangfire.Tags.Dashboard.Monitoring;
using Hangfire.Tags.Storage;

namespace Hangfire.Tags.Pro.Redis
{
    public class RedisTagsServiceStorage : ITagsServiceStorage
    {
        internal RedisTagsMonitoringApi MonitoringApi { get; }

        public RedisTagsServiceStorage()
            : this(new RedisStorageOptions())
        {
        }

        public RedisTagsServiceStorage(RedisStorageOptions options)
        {
            MonitoringApi = new RedisTagsMonitoringApi(JobStorage.Current.GetMonitoringApi(), options);
        }

        public ITagsTransaction GetTransaction(IWriteOnlyTransaction transaction)
        {
            return new RedisTagsTransaction();
        }

        public IEnumerable<TagDto> SearchWeightedTags(string tag, string setKey)
        {
            var monitoringApi = MonitoringApi;
            return monitoringApi.UseConnection(redis =>
            {
                var key = setKey;

                // We cannot do weighted tags here, since counting the amount of matching keys in Redis can only be done with the KEYS command
                // and you don't want that.
                var matchingTags = redis.SortedSetScan(key,  string.IsNullOrEmpty(tag) ? "" : $"*{tag}*").ToList();

                return matchingTags.Select(m => new TagDto
                    {Amount = 1, Percentage = 1, Tag = m});
            });
        }

        public IEnumerable<string> SearchRelatedTags(string tag, string setKey)
        {
            var monitoringApi = MonitoringApi;
            return monitoringApi.UseConnection(redis =>
            {
                // Get all jobs with the specified tag
                // Then, find all tags for those jobs
                var jobIds = redis.SortedSetScan($"{setKey}:{tag}").ToList();
                var keys = jobIds.Select(j => $"{setKey}:{j}");

                var tempKey = $"tags:job-{tag.ToLower()}-{Guid.NewGuid():N}";
                redis.SortedSetCombineAndStore(tempKey, keys);
                var allTags = redis.SortedSetScan(tempKey).Distinct().Where(t => t != tag).ToList();
                redis.KeyDelete(tempKey);

                return allTags;
            });
        }

        public int GetJobCount(string[] tags, string stateName = null)
        {
            var monitoringApi = MonitoringApi;
            return monitoringApi.UseConnection(redis => GetJobCount(redis, tags, stateName));
        }

        public IDictionary<string, int> GetJobStateCount(string[] tags, int maxTags = 50)
        {
            var monitoringApi = MonitoringApi;
            return monitoringApi.UseConnection(redis =>
            {
                var retval = new Dictionary<string, int>();
                var allStates = GlobalStateHandlers.Handlers.Select(h => h.StateName)
                    .Union(new[] {"Failed", "Processing"});

                foreach (var state in allStates)
                {
                    var redisKeys = tags.Select(t => $"{t}:{state.ToLower()}");
                    var tempKey = $"tags:jobstatecount-{state.ToLower()}";
                    var amount = redis.SortedSetCombineAndStore(tempKey, redisKeys.ToArray());
                    redis.KeyDelete(tempKey);

                    if (amount != 0)
                        retval.Add(state, (int) amount);
                }

                return retval;
            });
        }

        public JobList<MatchingJobDto> GetMatchingJobs(string[] tags, int from, int count, string stateName = null)
        {
            var monitoringApi = MonitoringApi;
            return monitoringApi.UseConnection(redis => GetJobs(redis, from, count, tags, stateName,
                (redisJob, job, stateData) =>
                    new MatchingJobDto
                    {
                        Job = job,
                        State = redisJob.StateName,
                        CreatedAt = redisJob.CreatedAt,
                        ResultAt = GetNullableStateDate(stateData, redisJob.StateName),
                        EnqueueAt = GetNullableStateDate(stateData, "Enqueue")
                    }));
        }

        private static DateTime? GetNullableStateDate(SafeDictionary<string, string> stateData, string stateName)
        {
            var stateDateName = stateName == "Processing" ? "StartedAt" : $"{stateName}At";
            var dateTime = stateData?[stateDateName];
            return !string.IsNullOrEmpty(dateTime) ? JobHelper.DeserializeNullableDateTime(dateTime) : null;
        }

        private static int GetJobCount(DatabaseWrapper redis, string[] tags, string stateName)
        {
            var redisKeys = tags.Select(t => string.IsNullOrEmpty(stateName) ? $"{t}" : $"{t}:{stateName.ToLower()}")
                .ToArray();

            if (redisKeys.Length <= 1)
                return (int) redis.SortedSetLength(redisKeys.First());

            var tempKey = $"tags:jobcount-{stateName.ToLower()}-{Guid.NewGuid():N}";
            var retval = redis.SortedSetCombineAndStore(tempKey, redisKeys);
            redis.KeyDelete(tempKey);

            return (int) retval;
        }

        private JobList<MatchingJobDto> GetJobs(
            DatabaseWrapper redis, int from, int count, string[] tags, string stateName,
            Func<RedisJob, Job, SafeDictionary<string, string>, MatchingJobDto> selector)
        {
            var redisKeys = tags.Select(t => string.IsNullOrEmpty(stateName) ? $"{t}" : $"{t}:{stateName.ToLower()}")
                .ToArray();

            List<string> jobIds;

            if (redisKeys.Length <= 1)
            {
                jobIds = redis.SortedSetRangeByScore(redisKeys.First(), from, count).ToList();
            }
            else
            {
                var tempKey = $"tags:job-{stateName.ToLower()}-{Guid.NewGuid():N}";
                redis.SortedSetCombineAndStore(tempKey, redisKeys);
                jobIds = redis.SortedSetRangeByScore(tempKey, from, count).ToList();
                redis.KeyDelete(tempKey);
            }

            return MonitoringApi.GetJobsWithProperties(jobIds, new[] {"State", "CreatedAt"},
                new[] {"EnqueuedAt", "FailedAt", "ScheduledAt", "SucceededAt", "DeletedAt"},
                (method, job, state) =>
                {
                    var redisJob = new RedisJob
                    {
                        StateName = job[0],
                        CreatedAt = JobHelper.DeserializeDateTime(job[1]),
                        StateData = new Dictionary<string, string>
                        {
                            {"EnqueuedAt", state[0]}, {"FailedAt", state[1]}, {"ScheduledAt", state[2]},
                            {"SucceededAt", state[3]}, {"DeletedAt", state[4]}
                        }
                    };
                    return selector(redisJob, method,
                        new SafeDictionary<string, string>(redisJob.StateData,
                            StringComparer.InvariantCultureIgnoreCase));
                });
        }

        /// <summary>
        /// Overloaded dictionary that doesn't throw if given an invalid key
        /// Fixes issues such as https://github.com/HangfireIO/Hangfire/issues/871
        /// </summary>
        private class SafeDictionary<TKey, TValue> : Dictionary<TKey, TValue>
        {
            public SafeDictionary(IDictionary<TKey, TValue> dictionary, IEqualityComparer<TKey> comparer)
                : base(dictionary, comparer)
            {
            }

            public SafeDictionary(IEnumerable<KeyValuePair<TKey, TValue>> dictionary, IEqualityComparer<TKey> comparer)
                : base(comparer)
            {
                foreach (var elm in dictionary)
                {
                    Add(elm.Key, elm.Value);
                }
            }

            public new TValue this[TKey i]
            {
                get => ContainsKey(i) ? base[i] : default;
                set => base[i] = value;
            }
        }
    }
}
