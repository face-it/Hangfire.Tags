using System;
using System.Threading;
using Hangfire.Pro.Redis;
using Hangfire.Server;

namespace Hangfire.Tags.Pro.Redis
{
    internal class ExpiredTagsWatcher : IServerComponent, IBackgroundProcess
    {
        private readonly RedisStorage _storage;
        private readonly RedisTagsServiceStorage _tagsServiceStorage;
        private readonly TimeSpan _checkInterval;

        private const string ExpireAfterDate = "tags:expire_min_ticks";

        private double _expireTagsAfter;

        public ExpiredTagsWatcher(RedisStorage storage, RedisTagsServiceStorage tagsServiceStorage,
            TimeSpan checkInterval)
        {
            if (checkInterval.Ticks <= 0L)
                throw new ArgumentOutOfRangeException(nameof(checkInterval), "Check interval should be positive.");

            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
            _tagsServiceStorage = tagsServiceStorage;
            _checkInterval = checkInterval;

            Init();
        }

        private void Init()
        {
            var monitoringApi = _tagsServiceStorage.GetMonitoringApi(_storage);
            monitoringApi.UseConnection(redis =>
            {
                if (!redis.KeyExists(ExpireAfterDate))
                {
                    redis.StringSet(ExpireAfterDate, DateTimeOffset.Now.ToUnixTimeSeconds().ToString());
                }
                _expireTagsAfter = int.Parse(redis.StringGet(ExpireAfterDate));

                return 0;
            });
        }

        public const string ExpiredTagsKey = "tags:expiry";

        public override string ToString() => GetType().ToString();

        private void Execute()
        {
            var monitoringApi = _tagsServiceStorage.GetMonitoringApi(_storage);
            monitoringApi.UseConnection(redis =>
            {
                var secondsSinceEpoch = DateTimeOffset.Now.ToUnixTimeSeconds();

                var values = redis.SortedSetRangeByScore(ExpiredTagsKey, _expireTagsAfter, secondsSinceEpoch);

                var batch = redis.CreateBatch();
                foreach (string redisValue in values)
                {
                    var keyvalue = redisValue.Split(new[] {'|'}, 2);
                    batch.SortedSetRemoveAsync(ExpiredTagsKey, redisValue);
                    batch.SetRemoveAsync(keyvalue[0], keyvalue[1]);
                }
                batch.Execute();

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