using System;
using Hangfire.Annotations;

namespace Hangfire.Tags.Storage
{
    public interface ITagsStorageTransactionRetryPolicy
    {
        void RetryOnTransactionError([InstantHandle] Action action);
    }

    public class NoOpTagsStorageTransactionRetryPolicy : ITagsStorageTransactionRetryPolicy
    {
        public void RetryOnTransactionError(Action action)
        {
            action();
        }
    }
}