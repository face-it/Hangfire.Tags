using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using Hangfire.Common;
using Hangfire.PostgreSql;
using Hangfire.Storage;
using Hangfire.Storage.Monitoring;
using Hangfire.Tags.Dashboard.Monitoring;
using Hangfire.Tags.Storage;

namespace Hangfire.Tags.PostgreSql
{
    public class PostgreSqlTagsServiceStorage : ObsoleteBaseStorage, ITagsServiceStorage
    {
        private readonly PostgreSqlStorageOptions _options;

        private PostgreSqlTagsMonitoringApi GetMonitoringApi(JobStorage jobStorage)
        {
            return new PostgreSqlTagsMonitoringApi(jobStorage);
        }

        public PostgreSqlTagsServiceStorage(PostgreSqlStorageOptions options)
        {
            _options = options;
        }

        public override ITagsTransaction GetTransaction(IWriteOnlyTransaction transaction)
        {
            return new PostgreSqlTagsTransaction(_options, transaction);
        }

        public override IEnumerable<TagDto> SearchWeightedTags(JobStorage jobStorage, string tag, string setKey)
        {
            var monitoringApi = GetMonitoringApi(jobStorage);
            return monitoringApi.UseConnection(connection =>
            {
                if (string.IsNullOrEmpty(tag))
                    tag = "[^0-9]"; // Exclude tags:<id> entries

                var sql =
                    $@"select count(*) as Amount from {_options.SchemaName}.Set s where s.Key ~ (@setKey || ':' || @tag) ";
                var total = connection.ExecuteScalar<int>(sql, new { setKey, tag });

                sql =
                    $@"select Overlay(Key placing '' from 1 for {setKey.Length + 1}) AS Tag, COUNT(*) AS Amount, CAST(ROUND(count(*) * 1.0 / @total * 100, 0) AS INT) as Percentage
from {_options.SchemaName}.Set s where s.Key ~ (@setKey || ':' || @tag) group by s.Key";

                var weightedTags = connection.Query<TagDto>(
                    sql,
                    new { setKey, tag, total });
                return weightedTags;
            });
        }

        public override IEnumerable<string> SearchRelatedTags(JobStorage jobStorage, string tag, string setKey)
        {
            var monitoringApi = GetMonitoringApi(jobStorage);
            return monitoringApi.UseConnection(connection =>
            {
                var sql =
                    $@"select distinct Overlay(sr.Key placing '' from 1 for {setKey.Length + 1}) AS Tag from {_options.SchemaName}.Set s inner join {_options.SchemaName}.Set sr on s.Value=sr.Value and s.Key <> sr.Key where s.Key ~ (@setKey || ':' || @tag)";

                return connection.Query<string>(
                    sql,
                    new { setKey, tag });
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
  from {_options.SchemaName}.Job j";

                for (var i = 0; i < tags.Length; i++)
                {
                    parameters["tag" + i] = tags[i];
                    jobsSql +=
                        $"  inner join {_options.SchemaName}.Set s{i} on j.Id=s{i}.Value::BIGINT and s{i}.Key=@tag{i}";
                }

                jobsSql +=
                    $@")
select j.StateName AS Key, count(*) AS Value
from {_options.SchemaName}.Job j
inner join cte on cte.Id = j.Id
inner join {_options.SchemaName}.State s on j.StateId = s.Id
group by j.StateName order by count(*) desc
limit {maxTags}";

                return connection.Query<KeyValuePair<string, int>>(
                        jobsSql,
                        parameters)
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

        private int GetJobCount(IDbConnection connection, string[] tags, string stateName)
        {
            var parameters = new Dictionary<string, object>
            {
                {"stateName", stateName}
            };

            var jobsSql =
                $@";with cte as
(
  select j.Id, row_number() over (order by j.Id desc) as row_num
  from {_options.SchemaName}.Job j ";

            for (var i = 0; i < tags.Length; i++)
            {
                parameters["tag" + i] = tags[i];
                jobsSql += $" inner join {_options.SchemaName}.Set s{i} on j.Id= s{i}.Value::bigint and s{i}.Key=@tag{i}";
            }

            jobsSql +=
                $@"
  where (@stateName IS NULL OR LENGTH(@stateName)=0 OR j.StateName=@stateName)
)
select count(*)
from {_options.SchemaName}.Job j
inner join cte on cte.Id = j.Id
left join {_options.SchemaName}.State s  on j.StateId = s.Id";

            return connection.ExecuteScalar<int>(
                jobsSql,
                parameters);
        }

        private JobList<TDto> GetJobs<TDto>(
            IDbConnection connection, int from, int count, string[] tags, string stateName,
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
  from {_options.SchemaName}.Job j";

            for (var i = 0; i < tags.Length; i++)
            {
                parameters["tag" + i] = tags[i];
                jobsSql += $"  inner join {_options.SchemaName}.Set s{i} on j.Id=s{i}.Value::BIGINT and s{i}.Key=@tag{i}";
            }

            jobsSql +=
$@"
  where (@stateName IS NULL OR LENGTH(@stateName) = 0 OR j.StateName=@stateName)
)
select j.*, s.Reason as StateReason, s.Data as StateData
from {_options.SchemaName}.Job j
inner join cte on cte.Id = j.Id
left join {_options.SchemaName}.State s on j.StateId = s.Id
where cte.row_num between @start and @end
order by j.Id desc";

            var jobs = connection.Query<SqlJob>(
                    jobsSql,
                    parameters)
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
        /// Overloaded dictionary that doesn't throw if given an invalid key Fixes issues such as https://github.com/HangfireIO/Hangfire/issues/871
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
