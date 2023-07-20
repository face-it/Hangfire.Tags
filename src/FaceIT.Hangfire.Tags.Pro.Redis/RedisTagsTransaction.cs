using System;
using Hangfire.Storage;
using Hangfire.Tags.Storage;

namespace Hangfire.Tags.Pro.Redis
{
    internal class RedisTagsTransaction : ITagsTransaction
    {
        private readonly IWriteOnlyTransaction _transaction;

        public RedisTagsTransaction(IWriteOnlyTransaction transaction)
        {
            _transaction = transaction;
        }

        public void ExpireSetValue(string key, string value, TimeSpan expireIn)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));

            // Store the timespan as the score
            _transaction.AddToSet(ExpiredTagsWatcher.ExpiredTagsKey, $"{key}|{value}",
                DateTimeOffset.Now.ToUnixTimeSeconds() + expireIn.TotalSeconds);
        }

        public void PersistSetValue(string key, string value)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));

            _transaction.RemoveFromSet(ExpiredTagsWatcher.ExpiredTagsKey, $"{key}|{value}");
        }
    }
}
