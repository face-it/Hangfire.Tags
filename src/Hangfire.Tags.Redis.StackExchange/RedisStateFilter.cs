using System.Linq;
using Hangfire.States;
using Hangfire.Storage;

namespace Hangfire.Tags.Redis.StackExchange
{
    internal class RedisStateFilter : IApplyStateFilter
    {
        private readonly RedisTagsServiceStorage _storage;

        public RedisStateFilter(RedisTagsServiceStorage storage)
        {
            _storage = storage;
        }

        public void OnStateApplied(ApplyStateContext context, IWriteOnlyTransaction transaction)
        {
            UpdateTagState(context);
        }

        public void OnStateUnapplied(ApplyStateContext context, IWriteOnlyTransaction transaction)
        {
            UpdateTagState(context);
        }

        private void UpdateTagState(ApplyStateContext context)
        {
            if (context.BackgroundJob == null)
                return;

            var oldState = context.OldStateName?.ToLower();
            var state = context.NewState?.Name.ToLower();

            _storage.GetMonitoringApi(context.Storage).UseConnection(redis =>
            {
                var tags = redis.SortedSetScan(_storage.GetRedisKey($"tags:{context.BackgroundJob.Id}"))
                    .Select(e => e.Element.ToString());

                foreach (var tag in tags)
                {
                    if (!string.IsNullOrEmpty(oldState))
                        redis.SetRemove(_storage.GetRedisKey($"tags:{tag}:{oldState}"), context.BackgroundJob.Id);
                    if (!string.IsNullOrEmpty(state))
                        redis.SetAdd(_storage.GetRedisKey($"tags:{tag}:{state}"), context.BackgroundJob.Id);
                }

                return 0;
            });
        }
    }
}