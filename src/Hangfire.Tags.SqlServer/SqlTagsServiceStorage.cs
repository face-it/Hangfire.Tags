using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using Dapper;
using Hangfire.Common;
using Hangfire.SqlServer;
using Hangfire.Storage;
using Hangfire.Storage.Monitoring;
using Hangfire.Tags.Dashboard.Monitoring;
using Hangfire.Tags.Storage;

namespace Hangfire.Tags.SqlServer
{
    public class SqlTagsServiceStorage : ITagsServiceStorage
    {
        private readonly SqlServerStorageOptions _options;

        private SqlTagsMonitoringApi MonitoringApi => new SqlTagsMonitoringApi(JobStorage.Current.GetMonitoringApi());

        public SqlTagsServiceStorage()
            : this(new SqlServerStorageOptions())
        {
        }

        public SqlTagsServiceStorage(SqlServerStorageOptions options)
        {
            _options = options;
        }

        public ITagsTransaction GetTransaction(IWriteOnlyTransaction transaction)
        {
            return new SqlTagsTransaction(_options, transaction);
        }

        public JobList<MatchingJobDto> GetMatchingJobs(string tag, int from, int count)
        {
            var monitoringApi = MonitoringApi;
            return monitoringApi.UseConnection(connection => GetJobs(connection, from, count, tag,
                (sqlJob, job, stateData) =>
                    new MatchingJobDto
                    {
                        Job = job,
                        State = sqlJob.StateName,
                    }));
        }

        private JobList<TDto> GetJobs<TDto>(
            DbConnection connection,
            int from,
            int count,
            string tag,
            Func<SqlJob, Job, SafeDictionary<string, string>, TDto> selector)
        {
            var jobsSql =
                $@";with cte as 
(
  select j.Id, row_number() over (order by j.Id desc) as row_num
  from [{_options.SchemaName}].Job j with (nolock, forceseek)
  inner join [{_options.SchemaName}].[Set] s on j.Id=s.Value
  where s.[Key]=@tag
)
select j.*, s.Reason as StateReason, s.Data as StateData
from [{_options.SchemaName}].Job j with (nolock)
inner join cte on cte.Id = j.Id 
left join [{_options.SchemaName}].State s with (nolock) on j.StateId = s.Id
where cte.row_num between @start and @end
order by j.Id desc";

            var jobs = connection.Query<SqlJob>(
                    jobsSql,
                    new {tag, start = from + 1, end = from + count},
                    commandTimeout: (int?) _options.CommandTimeout?.TotalSeconds)
                .ToList();

            return DeserializeJobs(jobs, selector);
        }

        private static Job DeserializeJob(string invocationData, string arguments)
        {
            var data = JobHelper.FromJson<InvocationData>(invocationData);
            data.Arguments = arguments;

            try
            {
                return data.Deserialize();
            }
            catch (JobLoadException)
            {
                return null;
            }
        }

        private static JobList<TDto> DeserializeJobs<TDto>(
            ICollection<SqlJob> jobs,
            Func<SqlJob, Job, SafeDictionary<string, string>, TDto> selector)
        {
            var result = new List<KeyValuePair<string, TDto>>(jobs.Count);

            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach (var job in jobs)
            {
                var dto = default(TDto);

                if (job.InvocationData != null)
                {
                    var deserializedData = JobHelper.FromJson<Dictionary<string, string>>(job.StateData);
                    var stateData = deserializedData != null
                        ? new SafeDictionary<string, string>(deserializedData, StringComparer.OrdinalIgnoreCase)
                        : null;

                    dto = selector(job, DeserializeJob(job.InvocationData, job.Arguments), stateData);
                }

                result.Add(new KeyValuePair<string, TDto>(job.Id.ToString(), dto));
            }

            return new JobList<TDto>(result);
        }

        /// <summary>
        /// Overloaded dictionary that doesn't throw if given an invalid key
        /// Fixes issues such as https://github.com/HangfireIO/Hangfire/issues/871
        /// </summary>
        private class SafeDictionary<TKey, TValue> : Dictionary<TKey, TValue>
        {
            public SafeDictionary(IDictionary<TKey, TValue> dictionary, IEqualityComparer<TKey> comparer)
                : base(dictionary, comparer)
            {
            }

            public new TValue this[TKey i]
            {
                get => ContainsKey(i) ? base[i] : default(TValue);
                set => base[i] = value;
            }
        }
    }
}
