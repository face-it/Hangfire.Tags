using System;
using Hangfire.Annotations;

namespace Hangfire.Tags.Storage
{
    public interface ITransactionRetryPolicy
    {
        void RetryOnTransactionError([InstantHandle] Action action);
    }
}