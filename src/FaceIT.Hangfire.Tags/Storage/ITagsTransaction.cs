using System;

namespace Hangfire.Tags.Storage
{
    public interface ITagsTransaction
    {
        void ExpireSetValue(string key, string value, TimeSpan expireIn);

        void PersistSetValue(string key, string value);
    }
}