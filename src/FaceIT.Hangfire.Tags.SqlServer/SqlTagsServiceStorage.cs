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
    public class SqlTagsServiceStorage : ObsoleteBaseStorage, ITagsServiceStorage
    {
        private readonly SqlServerStorageOptions _options;

        private static SqlTagsMonitoringApi GetMonitoringApi(JobStorage jobStorage)
        {
            return new SqlTagsMonitoringApi(jobStorage.GetMonitoringApi());
        }

        public SqlTagsServiceStorage(SqlServerStorageOptions options = null)
        {
            _options = options ?? new SqlServerStorageOptions();
        }

        public override ITagsTransaction GetTransaction(IWriteOnlyTransaction transaction)
        {
            return new SqlTagsTransaction(_options, transaction);
        }

        public override IEnumerable<TagDto> SearchWeightedTags(JobStorage jobStorage, string tag, string setKey)
        {
            var monitoringApi = GetMonitoringApi(jobStorage);
            return monitoringApi.UseConnection(connection =>
            {
                if (string.IsNullOrEmpty(tag))
                    tag = "[^0-9]"; // Exclude tags:<id> entries

                var sql =
                    $@"select count(*) as Amount from [{_options.SchemaName}].[Set] s where s.[Key] like @setKey + ':%' + @tag + '%'";
                var total = connection.ExecuteScalar<int>(sql, new { setKey, tag });

                sql =
                    $@"select STUFF([Key], 1, {setKey.Length + 1}, '') AS [Tag], COUNT(*) AS [Amount], CAST(ROUND(count(*) * 1.0 / @total * 100, 0) AS INT) as [Percentage]
from [{_options.SchemaName}].[Set] s where s.[Key] like @setKey + ':%' + @tag + '%' group by s.[Key]";

                return connection.Query<TagDto>(
                    sql,
                    new { setKey, tag, total },
                    commandTimeout: (int?)_options.CommandTimeout?.TotalSeconds);
            });
        }

        public override IEnumerable<string> SearchRelatedTags(JobStorage jobStorage, string tag, string setKey)
        {
            var monitoringApi = GetMonitoringApi(jobStorage);
            return monitoringApi.UseConnection(connection =>
            {
                var sql =
                    $"select distinct sv.[Value] from [{_options.SchemaName}].[Set] sv INNER JOIN [{_options.SchemaName}].[Set] s ON sv.Value <> @tag AND s.[Key] LIKE @setKey + ':%' + @tag + '%' AND sv.[Key] = @setKey + ':' + s.[Value]";

                return connection.Query<string>(
                    sql,
                    new { setKey, tag },
                    commandTimeout: (int?)_options.CommandTimeout?.TotalSeconds);
            });
        }

        public override int GetJobCount(JobStorage jobStorage, string[] tags, string stateName = null)
        {
            var monitoringApi = GetMonitoringApi(jobStorage);
            return monitoringApi.UseConnection(connection => GetJobCount(connection, tags, stateName));
        }

        public override IDictionary<string, int> GetJobStateCount(JobStorage jobStorage, string[] tags, int maxTags = 50)
        {
            var monitoringApi = GetMonitoringApi(jobStorage);
            return monitoringApi.UseConnection(connection =>
            {
                var parameters = new Dictionary<string, object>();

                var jobsSql =
                    $@";with cte as
(
  select j.Id, row_number() over (order by j.Id desc) as row_num
  from [{_options.SchemaName}].Job j with (nolock, forceseek)";

                for (var i = 0; i < tags.Length; i++)
                {
                    parameters["tag" + i] = tags[i];
                    jobsSql +=
                        $"  inner join [{_options.SchemaName}].[Set] s{i} on ISNUMERIC(s{i}.Value)=1 AND j.Id=s{i}.Value and s{i}.[Key]=@tag{i}";
                }

                jobsSql +=
                    $@")
select top {maxTags} j.StateName AS [Key], count(*) AS [Value]
from [{_options.SchemaName}].Job j with (nolock)
inner join cte on cte.Id = j.Id
inner join [{_options.SchemaName}].State s with (nolock) on j.StateId = s.Id and j.Id = s.jobId
group by j.StateName order by count(*) desc";

                return connection.Query<KeyValuePair<string, int>>(
                        jobsSql,
                        parameters,
                        commandTimeout: (int?)_options.CommandTimeout?.TotalSeconds)
                    .ToDictionary(d => d.Key, d => d.Value);
            });
        }

        public override JobList<MatchingJobDto> GetMatchingJobs(JobStorage jobStorage, string[] tags, int from, int count, string stateName = null)
        {
            var monitoringApi = GetMonitoringApi(jobStorage);
            return monitoringApi.UseConnection(connection => GetJobs(connection, from, count, tags, stateName,
                (sqlJob, job, stateData) =>
                    new MatchingJobDto
                    {
                        Job = job,
                        State = sqlJob.StateName,
                        CreatedAt = sqlJob.CreatedAt,
                        ResultAt = GetStateDate(stateData, sqlJob.StateName),
                        EnqueueAt = GetNullableStateDate(stateData, "Enqueue")
                    }));
        }

        private static DateTime? GetNullableStateDate(SafeDictionary<string, string> stateData, string stateName)
        {
            var stateDateName = stateName == "Processing" ? "StartedAt" : $"{stateName}At";
            var dateTime = stateData?[stateDateName];
            return !string.IsNullOrEmpty(dateTime) ? JobHelper.DeserializeNullableDateTime(dateTime) : null;
        }

        private static DateTime GetStateDate(SafeDictionary<string, string> stateData, string stateName)
        {
            return GetNullableStateDate(stateData, stateName) ?? DateTime.MinValue;
        }

        private int GetJobCount(DbConnection connection, string[] tags, string stateName)
        {
            var parameters = new Dictionary<string, object>
            {
                {"stateName", stateName}
            };

            var jobsSql =
                $@";with cte as
(
  select j.Id, row_number() over (order by j.Id desc) as row_num
  from [{_options.SchemaName}].Job j with (nolock, forceseek)";

            for (var i = 0; i < tags.Length; i++)
            {
                parameters["tag" + i] = tags[i];
                jobsSql += $"  inner join [{_options.SchemaName}].[Set] s{i} on ISNUMERIC(s{i}.Value)=1 AND j.Id=s{i}.Value and s{i}.[Key]=@tag{i}";
            }

            jobsSql +=
                $@"
  where (@stateName IS NULL OR LEN(@stateName)=0 OR j.StateName=@stateName)
)
select count(*)
from [{_options.SchemaName}].Job j with (nolock)
inner join cte on cte.Id = j.Id";

            return connection.ExecuteScalar<int>(
                jobsSql,
                parameters,
                commandTimeout: (int?)_options.CommandTimeout?.TotalSeconds);
        }

        private JobList<TDto> GetJobs<TDto>(
            DbConnection connection, int from, int count, string[] tags, string stateName,
            Func<SqlJob, Job, SafeDictionary<string, string>, TDto> selector)
        {
            var parameters = new Dictionary<string, object>
            {
                { "start", from + 1 },
                { "end", from + count },
                { "stateName", stateName }
            };

            var jobsSql =
                $@";with cte as
(
  select j.Id, row_number() over (order by j.Id desc) as row_num
  from [{_options.SchemaName}].Job j with (nolock, forceseek)";

            for (var i = 0; i < tags.Length; i++)
            {
                parameters["tag" + i] = tags[i];
                jobsSql += $"  inner join [{_options.SchemaName}].[Set] s{i} on ISNUMERIC(s{i}.Value)=1 AND j.Id=s{i}.Value and s{i}.[Key]=@tag{i}";
            }

            jobsSql +=
$@"
  where (@stateName IS NULL OR LEN(@stateName) = 0 OR j.StateName=@stateName)
)
select j.*, s.Reason as StateReason, s.Data as StateData
from [{_options.SchemaName}].Job j with (nolock)
inner join cte on cte.Id = j.Id
inner join [{_options.SchemaName}].State s with (nolock) on j.StateId = s.Id and j.Id = s.jobId
where cte.row_num between @start and @end
order by j.Id desc";

            var jobs = connection.Query<SqlJob>(
                    jobsSql,
                    parameters,
                    commandTimeout: (int?)_options.CommandTimeout?.TotalSeconds)
                .ToList();

            return DeserializeJobs(jobs, selector);
        }

        private static Job DeserializeJob(string invocationData, string arguments)
        {
            var data = InvocationData.DeserializePayload(invocationData);
            if (!string.IsNullOrEmpty(arguments))
                data.Arguments = arguments;

            try
            {
                return data.DeserializeJob();
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
                    var deserializedData = SerializationHelper.Deserialize<Dictionary<string, string>>(job.StateData);
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
                get => ContainsKey(i) ? base[i] : default;
                set => base[i] = value;
            }
        }
    }
}
