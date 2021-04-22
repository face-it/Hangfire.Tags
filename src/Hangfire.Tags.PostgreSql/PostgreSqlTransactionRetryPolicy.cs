using System;
using System.Collections.Generic;
using System.Threading;
using Hangfire.Logging;
using Hangfire.Tags.Storage;
using Npgsql;

namespace Hangfire.Tags.PostgreSql
{
    /// <summary>
    /// Retry policy to address https://github.com/face-it/Hangfire.Tags/issues/35
    /// </summary>
    public class PostgreSqlTransactionRetryPolicy : ITransactionRetryPolicy
    {
        private readonly int? _timeBetweenRetries;
        private readonly int _retryCount;
        private readonly ILog _logger = LogProvider.For<PostgreSqlTransactionRetryPolicy>(); 

        public PostgreSqlTransactionRetryPolicy(int retryCount, int? timeBetweenRetries = null)
        {
            if (retryCount < 0)
                throw new ArgumentException("Retrycount should be 0 or higher", nameof(retryCount));
            if (timeBetweenRetries < 0)
                throw new ArgumentException("Time between retries should be null, 0 or higher", nameof(timeBetweenRetries));

            _timeBetweenRetries = timeBetweenRetries;
            _retryCount = 1 + retryCount;
        }
        
        private static readonly HashSet<string> ConcurrencyErrorCodes = new HashSet<string>
        {
            "40001", //serialization_failure
            "40P01"  //deadlock_detected 
        };

        public void RetryOnTransactionError(Action action)
        {
            for (var i = 0; i < _retryCount; i++)
            {
                try
                {
                    action();
                }
                catch (PostgresException e) when (ConcurrencyErrorCodes.Contains(e.SqlState))
                {
                    if (i == _retryCount - 1)
                        throw;

                    if (_logger.IsTraceEnabled())
                        _logger.TraceException($"Operation failed with concurrency error. Retrying {i + 1}/{_retryCount + 1}", e);

                    if (_timeBetweenRetries.HasValue)
                        Thread.Sleep(_timeBetweenRetries.Value);
                }
            }
        }
    }
}