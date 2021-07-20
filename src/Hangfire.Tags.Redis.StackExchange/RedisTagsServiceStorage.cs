using System;
using System.Collections.Generic;
using System.Linq;
using Hangfire.Annotations;
using Hangfire.Common;
using Hangfire.Redis;
using Hangfire.Storage;
using Hangfire.Storage.Monitoring;
using Hangfire.Tags.Dashboard.Monitoring;
using Hangfire.Tags.Storage;
using StackExchange.Redis;

namespace Hangfire.Tags.Redis.StackExchange
{
    public class RedisTagsServiceStorage : ObsoleteBaseStorage, ITagsServiceStorage
    {
        private readonly RedisStorageOptions _options;

        internal RedisTagsMonitoringApi GetMonitoringApi(JobStorage jobStorage)
        {
            return new RedisTagsMonitoringApi(jobStorage.GetMonitoringApi());
        }

        public RedisTagsServiceStorage()
            : this(new RedisStorageOptions())
        {
        }

        public RedisTagsServiceStorage(RedisStorageOptions options)
        {
            _options = options;
        }

        internal string GetRedisKey([NotNull] string key)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            return _options.Prefix + key;
        }

        public override ITagsTransaction GetTransaction(IWriteOnlyTransaction transaction)
        {
            return new RedisTagsTransaction();
        }

        public override IEnumerable<TagDto> SearchWeightedTags(JobStorage jobStorage, string tag, string setKey)
        {
            var monitoringApi = GetMonitoringApi(jobStorage);
            return monitoringApi.UseConnection(redis =>
            {
                var key = GetRedisKey(setKey);

                // We cannot do weighted tags here, since counting the amount of matching keys in Redis can only be done with the KEYS command
                // and you don't want that.
                var matchingTags = redis.SortedSetScan(key,  string.IsNullOrEmpty(tag) ? "" : $"*{tag}*").ToList();

                return matchingTags.Select(m => new TagDto
                    {Amount = 1, Percentage = 1, Tag = m.Element.ToString()});
            });
        }

        public override IEnumerable<string> SearchRelatedTags(JobStorage jobStorage, string tag, string setKey)
        {
            var monitoringApi = GetMonitoringApi(jobStorage);
            return monitoringApi.UseConnection(redis =>
            {
                // Get all jobs with the specified tag
                // Then, find all tags for those jobs
                var entries = redis.SortedSetScan(GetRedisKey($"{setKey}:{tag}")).ToList();

                var jobIds = entries.Select(e => e.Element.ToString()).ToList();

                return jobIds.SelectMany(j =>
                {
                    var key = GetRedisKey($"{setKey}:{j}");
                    var tags = redis.SortedSetScan(key);

                    return tags.Select(t => t.Element.ToString());
                }).Where(t => t != tag).Distinct();
            });
        }

        public override int GetJobCount(JobStorage jobStorage, string[] tags, string stateName = null)
        {
            var monitoringApi = GetMonitoringApi(jobStorage);
            return monitoringApi.UseConnection(redis => GetJobCount(redis, tags, stateName));
        }

        public override IDictionary<string, int> GetJobStateCount(JobStorage jobStorage, string[] tags, int maxTags = 50)
        {
            var monitoringApi = GetMonitoringApi(jobStorage);
            return monitoringApi.UseConnection(redis =>
            {
                var retval = new Dictionary<string, int>();
                var allStates = Hangfire.GlobalStateHandlers.Handlers.Select(h => h.StateName)
                    .Union(new[] {"Failed", "Processing"});

                foreach (var state in allStates)
                {
                    var redisKeys = tags.Select(t => (RedisKey) GetRedisKey($"{t}:{state.ToLower()}"));
                    var tempKey = (RedisKey) GetRedisKey($"tags:jobstatecount-{state.ToLower()}");
                    var amount = redis.SortedSetCombineAndStore(SetOperation.Intersect, tempKey, redisKeys.ToArray());
                    redis.KeyDelete(tempKey);

                    if (amount != 0)
                        retval.Add(state, (int) amount);
                }

                return retval;
            });
        }

        public override JobList<MatchingJobDto> GetMatchingJobs(JobStorage jobStorage, string[] tags, int from, int count, string stateName = null)
        {
            var monitoringApi = GetMonitoringApi(jobStorage);
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

        private int GetJobCount(IDatabase redis, string[] tags, string stateName)
        {
            var redisKeys = tags.Select(t =>
            {
                var key = string.IsNullOrEmpty(stateName) ? $"{t}" : $"{t}:{stateName.ToLower()}";
                return (RedisKey) GetRedisKey(key);
            }).ToArray();

            if (redisKeys.Length <= 1)
            {
                return (int) (string.IsNullOrEmpty(stateName)
                    ? redis.SortedSetLength(redisKeys.First())
                    : redis.SetLength(redisKeys.First()));
            }

            var tempKey = (RedisKey) GetRedisKey($"tags:jobcount-{stateName.ToLower()}");
            var retval = string.IsNullOrEmpty(stateName)
                ? redis.SortedSetCombineAndStore(SetOperation.Intersect, tempKey, redisKeys) // Without state are stored in Sorted Set
                : redis.SetCombineAndStore(SetOperation.Intersect, tempKey, redisKeys); // With state are stored in a regular Set

            redis.KeyDelete(tempKey);
            return (int) retval;
        }

        private JobList<TDto> GetJobs<TDto>(
            IDatabase redis, int from, int count, string[] tags, string stateName,
            Func<RedisJob, Job, SafeDictionary<string, string>, TDto> selector)
        {
            var redisKeys = tags.Select(t =>
            {
                var key = string.IsNullOrEmpty(stateName) ? $"{t}" : $"{t}:{stateName.ToLower()}";
                return (RedisKey) GetRedisKey(key);
            }).ToArray();

            List<string> jobIds;

            if (redisKeys.Length <= 1)
            {
                jobIds = string.IsNullOrEmpty(stateName)
                    ? redis.SortedSetScan(redisKeys.First())
                        .Select(e => e.Element.ToString()).Skip(from).Take(count).ToList()
                    : redis.SetScan(redisKeys.First()).Select(e => e.ToString()).Skip(from).Take(count).ToList();
            }
            else
            {
                var tempKey = (RedisKey) GetRedisKey($"tags:job-{stateName.ToLower()}");
                if (string.IsNullOrEmpty(stateName))
                {
                    // Without state are stored in Sorted Set
                    redis.SortedSetCombineAndStore(SetOperation.Intersect, tempKey, redisKeys);
                    jobIds = redis.SortedSetScan(tempKey)
                        .Select(v => v.Element.ToString()).Skip(from).Take(count).ToList();
                }
                else
                {
                    // With state are stored in a regular Set
                    redis.SetCombineAndStore(SetOperation.Intersect, tempKey, redisKeys);
                    jobIds = redis.SetScan(tempKey).Select(v => v.ToString()).Skip(from).Take(count).ToList();
                }

                redis.KeyDelete(tempKey);
            }

            var redisJobs = jobIds.Select(i =>
            {
                var values = new SafeDictionary<string, string>(
                    redis.HashGetAll(GetRedisKey($"job:{i}")).ToStringDictionary(), StringComparer.OrdinalIgnoreCase);

                var stateValues = new SafeDictionary<string, string>(
                    redis.HashGetAll(GetRedisKey($"job:{i}:state")).ToStringDictionary(),
                    StringComparer.OrdinalIgnoreCase);

                return new RedisJob
                {
                    Id = Guid.Parse(i),
                    CreatedAt = JobHelper.DeserializeDateTime(values["CreatedAt"]),
                    StateData = stateValues,
                    StateName = values["State"],
                    Arguments = values["Arguments"],
                    InvocationData =
                        new InvocationData(values["Type"], values["Method"], values["ParameterTypes"],
                            values["Arguments"]).SerializePayload(true)
                };
            }).ToList();

            return DeserializeJobs(redisJobs, selector);
        }

        private static Job DeserializeJob(string invocationData, string arguments)
        {
            var data = InvocationData.DeserializePayload(invocationData);
            if (!string.IsNullOrEmpty(arguments))
                data.Arguments = arguments;

            try
            {
                return data.DeserializeJob();
            }
            catch (JobLoadException)
            {
                return null;
            }
        }

        private static JobList<TDto> DeserializeJobs<TDto>(
            ICollection<RedisJob> jobs,
            Func<RedisJob, Job, SafeDictionary<string, string>, TDto> selector)
        {
            var result = new List<KeyValuePair<string, TDto>>(jobs.Count);

            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach (var job in jobs)
            {
                var dto = default(TDto);

                if (job.InvocationData != null)
                {
                    var deserializedData = job.StateData;
                    var stateData = deserializedData != null
                        ? new SafeDictionary<string, string>(deserializedData, StringComparer.OrdinalIgnoreCase)
                        : null;

                    dto = selector(job, DeserializeJob(job.InvocationData, job.Arguments), stateData);
                }

                result.Add(new KeyValuePair<string, TDto>(job.Id.ToString(), dto));
            }

            return new JobList<TDto>(result);
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

            public new TValue this[TKey i]
            {
                get => ContainsKey(i) ? base[i] : default;
                set => base[i] = value;
            }
        }
    }
}
