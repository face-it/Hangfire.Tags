using System;
using System.Collections.Generic;
using System.Threading;
using Hangfire.Redis.StackExchange;
using Hangfire.Server;

namespace Hangfire.Tags.Redis.StackExchange
{
    internal class ExpiredTagsWatcher : IBackgroundProcess
    {
        private readonly RedisStorage _storage;
        private readonly RedisTagsServiceStorage _tagsServiceStorage;
        private readonly TimeSpan _checkInterval;

        private readonly string expireAfterDate = "tags:expire_min_ticks";

        private double _expireTagsAfter;

        public ExpiredTagsWatcher(RedisStorage storage, RedisTagsServiceStorage tagsServiceStorage,
            TimeSpan checkInterval)
        {
            if (checkInterval.Ticks <= 0L)
                throw new ArgumentOutOfRangeException(nameof(checkInterval), "Check interval should be positive.");

            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
            _tagsServiceStorage = tagsServiceStorage;
            _checkInterval = checkInterval;
            expireAfterDate = _tagsServiceStorage.GetRedisKey(expireAfterDate);
            Init();
        }

        private void Init()
        {
            var monitoringApi = _tagsServiceStorage.GetMonitoringApi(_storage);
            monitoringApi.UseConnection<int>(redis =>
            {
                if (!redis.KeyExists(expireAfterDate))
                {
                    redis.StringSet(expireAfterDate, DateTimeOffset.Now.ToUnixTimeSeconds());
                }
                _expireTagsAfter = int.Parse(redis.StringGet(expireAfterDate));

                return 0;
            });
        }

        public const string ExpiredTagsKey = "tags:expiry";
        static string[] states = new[] { "deleted", "succeeded" };
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
                    foreach (string state in states)
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
                            if (count.Status == System.Threading.Tasks.TaskStatus.RanToCompletion && count.Result == 0)
                            {
                                var splittedJobName = keyvalue[0].Split(':');
                                if (splittedJobName.Length > 1)
                                    batch.SortedSetRemoveAsync(_tagsServiceStorage.GetRedisKey("tags"), splittedJobName[1]);
                            }
                        });
                    }
                }
                batch.Execute();

                //clean up tags zset
                foreach (string tag in tagsToCheckForCleaning)
                {
                    if (redis.SortedSetLength(_tagsServiceStorage.GetRedisKey($"tags:{tag}")) == 0)
                        redis.SortedSetRemove(_tagsServiceStorage.GetRedisKey("tags"), tag);
                }

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