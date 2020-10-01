using System;
using Hangfire.Tags.Storage;

namespace Hangfire.Tags.Redis.StackExchange
{
    internal class RedisTagsTransaction : ITagsTransaction
    {
        public void ExpireSetValue(string key, string value, TimeSpan expireIn)
        {
            // The Hangfire.Redis.StackExchange library doesn't support the expire field in a set.
            // Therefore, this is not possible.

            if (key == null) throw new ArgumentNullException(nameof(key));

            // This function should set the expire value
        }

        public void PersistSetValue(string key, string value)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));

            // The Hangfire.Redis.StackExchange library doesn't support the expire field in a set.
            // Therefore, this is not possible.

            // This function should reset the expire value
        }
    }
}
