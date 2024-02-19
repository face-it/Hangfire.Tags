using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Hangfire.Redis.StackExchange;
using Hangfire.Server;

namespace Hangfire.Tags.Redis.StackExchange
{
    internal class ExpiredTagsWatcher : IBackgroundProcess
    {
        private readonly RedisStorage _storage;
        private readonly RedisTagsServiceStorage _tagsServiceStorage;
        private readonly TimeSpan _checkInterval;

        private readonly string _expireAfterDate = "tags:expire_min_ticks";
        private double _expireTagsAfter;

        private static readonly string[] States = { "deleted", "succeeded" };

        public const string ExpiredTagsKey = "tags:expiry";

        public ExpiredTagsWatcher(RedisStorage storage, RedisTagsServiceStorage tagsServiceStorage,
            TimeSpan checkInterval)
        {
            if (checkInterval.Ticks <= 0L)
                throw new ArgumentOutOfRangeException(nameof(checkInterval), "Check interval should be positive.");

            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
            _tagsServiceStorage = tagsServiceStorage;
            _checkInterval = checkInterval;
            _expireAfterDate = _tagsServiceStorage.GetRedisKey(_expireAfterDate);
            Init();
        }

        private void Init()
        {
            var monitoringApi = _tagsServiceStorage.GetMonitoringApi(_storage);
            monitoringApi.UseConnection(redis =>
            {
                if (!redis.KeyExists(_expireAfterDate))
                {
                    redis.StringSet(_expireAfterDate, DateTimeOffset.Now.ToUnixTimeSeconds());
                }
                _expireTagsAfter = int.Parse(redis.StringGet(_expireAfterDate));

                return 0;
            });
        }

        public override string ToString() => GetType().ToString();

        private void Execute()
        {
            var monitoringApi = _tagsServiceStorage.GetMonitoringApi(_storage);
            monitoringApi.UseConnection(redis =>
            {
                var secondsSinceEpoch = DateTimeOffset.Now.ToUnixTimeSeconds();

                var expiredTagKey = _tagsServiceStorage.GetRedisKey(ExpiredTagsKey);
                var values = redis.SortedSetRangeByScore(expiredTagKey, _expireTagsAfter, secondsSinceEpoch);
                var tagsToCheckForCleaning = new HashSet<string>();
                var batch = redis.CreateBatch();
                foreach (string redisValue in values)
                {
                    var keyvalue = redisValue.Split(new[] { '|' }, 2);
                    batch.SortedSetRemoveAsync(expiredTagKey, redisValue);
                    batch.SetRemoveAsync(_tagsServiceStorage.GetRedisKey(keyvalue[0]), keyvalue[1]);
                    foreach (var state in States)
                    {
                        var stateKey = _tagsServiceStorage.GetRedisKey($"{keyvalue[0]}:{state}");
                        batch.SetRemoveAsync(stateKey, keyvalue[1]);
                        var jobTrackKey = _tagsServiceStorage.GetRedisKey($"{keyvalue[0]}");
                        batch.SortedSetRemoveAsync(jobTrackKey, keyvalue[1]);

                        var splittedJobName = keyvalue[0].Split(':');
                        if (splittedJobName.Length > 1)
                            tagsToCheckForCleaning.Add(splittedJobName[1]);

                        batch.SortedSetLengthAsync(jobTrackKey).ContinueWith(count =>
                        {
                            if (count.Status != TaskStatus.RanToCompletion || count.Result != 0) return;

                            var jobNameParts = keyvalue[0].Split(':');
                            if (jobNameParts.Length > 1)
                                batch.SortedSetRemoveAsync(_tagsServiceStorage.GetRedisKey("tags"), jobNameParts[1]);
                        });
                    }
                }
                batch.Execute();

                //clean up tags zset
                foreach (var tag in tagsToCheckForCleaning)
                    if (redis.SortedSetLength(_tagsServiceStorage.GetRedisKey($"tags:{tag}")) == 0)
                        redis.SortedSetRemove(_tagsServiceStorage.GetRedisKey("tags"), tag);

                return 0;
            });
        }

        public void Execute(CancellationToken cancellationToken)
        {
            Execute();
            cancellationToken.WaitHandle.WaitOne(_checkInterval);
        }

        public void Execute(BackgroundProcessContext context)
        {
            Execute();
            context.Wait(_checkInterval);
        }
    }
}