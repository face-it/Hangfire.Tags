using System;

namespace Hangfire.Tags.Storage
{
    public class TransactionRetryPolicy : ITransactionRetryPolicy
    {
        public virtual void RetryOnTransactionError(Action action)
        {
            action();
        }
    }
}