using System;
using System.Collections.Generic;
using Hangfire.Logging;
using Hangfire.Tags.Storage;
using Npgsql;

namespace Hangfire.Tags.PostgreSql
{
    /// <summary>
    /// Retry policy to address https://github.com/face-it/Hangfire.Tags/issues/35
    /// </summary>
    public class TagsStorageTransactionRetryPolicy : ITagsStorageTransactionRetryPolicy
    {
        private readonly int _retryCount;
        private readonly ILog _logger = LogProvider.For<TagsStorageTransactionRetryPolicy>(); 

        public TagsStorageTransactionRetryPolicy(int retryCount)
        {
            _retryCount = retryCount;
        }
        
        private static readonly HashSet<string> ConcurrencyErrorCodes = new HashSet<string>
        {
            "40001", //serialization_failure
            "40P01"  //deadlock_detected 
        };

        public void RetryOnTransactionError(Action action)
        {
            for (int i = 1; i < _retryCount; i++)
            {
                try
                {
                    action();
                }
                catch (PostgresException e) when (ConcurrencyErrorCodes.Contains(e.SqlState))
                {
                    _logger.TraceException($"Operation failed with concurrency error. Retrying {i}/{_retryCount}", e);
                }
            }
        }
    }
}