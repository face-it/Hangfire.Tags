using System.Collections.Generic;
using Hangfire.Logging;
using Hangfire.Pro.Redis;
using Hangfire.Server;
using Hangfire.States;
using Hangfire.Storage;

namespace Hangfire.Tags.Pro.Redis
{
    internal class RedisTagsStorage : JobStorage
    {
        private readonly RedisStorage _redisStorage;
        private readonly RedisTagsServiceStorage _tagsServiceStorage;
        private readonly RedisStorageOptions _options;

        public RedisTagsStorage(RedisStorage redisStorage, RedisTagsServiceStorage tagsServiceStorage,
            RedisStorageOptions options)
        {
            _redisStorage = redisStorage;
            _tagsServiceStorage = tagsServiceStorage;
            _options = options;
        }

        public override IMonitoringApi GetMonitoringApi()
        {
            return _redisStorage.GetMonitoringApi();
        }

        public override IStorageConnection GetConnection()
        {
            return _redisStorage.GetConnection();
        }
        
        public override IEnumerable<IServerComponent> GetComponents()
        {
            // Reset JobStorage to the original storage, the ExpiredTagsWatcher has been initialized after this method finishes.
            JobStorage.Current = _redisStorage;

            var components = _redisStorage.GetComponents();
            if (components != null)
            {
                foreach (var c in components)
                {
                    yield return c;
                }
            }

            yield return new ExpiredTagsWatcher(_redisStorage, _tagsServiceStorage, _redisStorage.JobExpirationTimeout);
        }

        public override IEnumerable<IStateHandler> GetStateHandlers()
        {
            return _redisStorage.GetStateHandlers();
        }

        public override bool LinearizableReads => _redisStorage.LinearizableReads;

        public override void WriteOptionsToLog(ILog logger)
        {
            _redisStorage.WriteOptionsToLog(logger);
        }

        public static implicit operator RedisStorage(RedisTagsStorage s) => s._redisStorage;
    }
}