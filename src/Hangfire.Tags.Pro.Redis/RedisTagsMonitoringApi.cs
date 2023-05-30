using System;
using System.Collections.Generic;
using System.Reflection;
using Hangfire.Common;
using Hangfire.Pro.Redis;
using Hangfire.Storage;
using Hangfire.Storage.Monitoring;
using Hangfire.Tags.Dashboard.Monitoring;

namespace Hangfire.Tags.Pro.Redis
{
    internal class RedisTagsMonitoringApi
    {
        private readonly IMonitoringApi _monitoringApi;
        private readonly DatabaseWrapper _wrapper;
        private readonly MethodInfo _getJobsWithPropertiesMethod;

        public RedisTagsMonitoringApi(IMonitoringApi monitoringApi, RedisStorageOptions options)
        {
            if (monitoringApi.GetType().Name != "RedisMonitoringApi")
                throw new ArgumentException("The monitor API is not implemented using Redis", nameof(monitoringApi));
            _monitoringApi = monitoringApi;

            // Dirty, but lazy...we would like to execute these commands in the same transaction, so we're resorting to reflection for now

            var databaseFactory = monitoringApi.GetType().GetTypeInfo().GetField("_databaseFactory",
                BindingFlags.NonPublic | BindingFlags.Instance);

            if (databaseFactory == null)
                throw new ArgumentException("The field _databaseFactory cannot be found.");

            var getJobsWithPropertiesMethod = monitoringApi.GetType()
                .GetMethod("GetJobsWithProperties", BindingFlags.Instance | BindingFlags.NonPublic);
            if (getJobsWithPropertiesMethod == null)
                throw new ArgumentException("The method GetJobsWithPropertiesMethod cannot be found.");

            _getJobsWithPropertiesMethod = getJobsWithPropertiesMethod.MakeGenericMethod(typeof(MatchingJobDto));

            var factory = (Func<object>) databaseFactory.GetValue(monitoringApi);
            var objFactory = factory.Invoke();
            _wrapper = new DatabaseWrapper(objFactory, options);
        }

        public T UseConnection<T>(Func<DatabaseWrapper, T> action)
        {
            return action(_wrapper);
        }

        public JobList<MatchingJobDto> GetJobsWithProperties(
            IList<string> jobIds, string[] properties, string[] stateProperties,
            Func<Job, InvocationData, JobLoadException, string[], string[], MatchingJobDto> selector)
        {
            return (JobList<MatchingJobDto>) _getJobsWithPropertiesMethod.Invoke(_monitoringApi,
                new object[] {jobIds, properties, stateProperties, selector});
        }
    }
}
