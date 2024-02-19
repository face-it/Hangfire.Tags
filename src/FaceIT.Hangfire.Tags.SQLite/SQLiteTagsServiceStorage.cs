using System;
using System.Collections.Generic;
using System.Linq;
using Hangfire.Common;
using Hangfire.Storage;
using Hangfire.Storage.Monitoring;
using Hangfire.Tags.Dashboard.Monitoring;
using Hangfire.Tags.Storage;
using SQLite;
using Hangfire.Storage.SQLite;

namespace Hangfire.Tags.SQLite
{
    public class SQLiteTagsServiceStorage : ObsoleteBaseStorage, ITagsServiceStorage
    {
        private readonly SQLiteStorageOptions _options;

        private SQLiteTagsMonitoringApi GetMonitoringApi(JobStorage jobStorage)
        {
            return new SQLiteTagsMonitoringApi(jobStorage.GetMonitoringApi());
        }

        public SQLiteTagsServiceStorage()
            : this(new SQLiteStorageOptions())
        {
        }

        public SQLiteTagsServiceStorage(SQLiteStorageOptions options)
        {
            _options = options;
        }

        public override ITagsTransaction GetTransaction(IWriteOnlyTransaction transaction)
        {
            return new SQLiteTagsTransaction(_options, transaction);
        }

        public override IEnumerable<TagDto> SearchWeightedTags(JobStorage jobStorage, string tag = null, string setKey = "tags")
        {
            var monitoringApi = GetMonitoringApi(jobStorage);
            return monitoringApi.UseConnection(connection =>
            {
                if (string.IsNullOrEmpty(tag))
                {
                    var sql = @"select count(*) as Amount from [Set] s 
                                where s.Key like ? || ':%' AND NOT s.Key GLOB ? || ':[0-9]*'";

                    var total = connection.Database.ExecuteScalar<int>(sql, setKey, setKey);

                    sql = $@"select SUBSTR([Key], {setKey.Length + 2}) AS [Tag], COUNT(*) AS [Amount], CAST(ROUND(count(*) * 1.0 / ? * 100, 0) AS SIGNED) as [Percentage] 
                             from [Set] s where s.Key like ? || ':%' AND NOT s.Key GLOB ? || ':[0-9]*' group by s.Key";

                    return connection.Database.Query<TagDto>(sql, total, setKey, setKey);
                }
                else
                {
                    var sql = @"select count(*) as Amount from [Set] s 
                                where s.Key like ? || ':%' || ? || '%'";

                    var total = connection.Database.ExecuteScalar<int>(sql, setKey, tag);

                    sql = $@"select SUBSTR([Key], {setKey.Length + 2}) AS [Tag], COUNT(*) AS [Amount], CAST(ROUND(count(*) * 1.0 / ? * 100, 0) AS SIGNED) as [Percentage] 
                             from [Set] s where s.Key like ? || ':%' || ? || '%' group by s.Key";

                    return connection.Database.Query<TagDto>(sql, total, setKey, tag);
                }
            });
        }

        public override IEnumerable<string> SearchRelatedTags(JobStorage jobStorage, string tag, string setKey = "tags")
        {
            var monitoringApi = GetMonitoringApi(jobStorage);
            return monitoringApi.UseConnection(connection =>
            {
                if (string.IsNullOrEmpty(tag))
                {
                    var sql =
                        $@"select distinct SUBSTR(sr.[Key], {setKey.Length + 2}) from [Set] s INNER JOIN [Set] sr ON s.[Value]=sr.[Value] AND s.[Key] <> sr.[Key]
                        where s.Key like ? || ':%' AND NOT s.Key GLOB ? || ':[0-9]*'";

                    return connection.Database.QueryScalars<string>(sql, setKey, setKey);
                }
                else
                {
                    var sql =
                        $@"select distinct SUBSTR(sr.[Key], {setKey.Length + 2}) from [Set] s INNER JOIN [Set] sr ON s.[Value]=sr.[Value] AND s.[Key] <> sr.[Key]
                        where s.Key like ? || ':%' || ? || '%'";

                    return connection.Database.QueryScalars<string>(sql, setKey, tag);
                }
            });
        }

        public override int GetJobCount(JobStorage jobStorage, string[] tags, string stateName = null)
        {
            var monitoringApi = GetMonitoringApi(jobStorage);
            return monitoringApi.UseConnection(connection => GetJobCount(connection.Database, tags, stateName));
        }

        public override IDictionary<string, int> GetJobStateCount(JobStorage jobStorage, string[] tags, int maxTags = 50)
        {
            var monitoringApi = GetMonitoringApi(jobStorage);
            return monitoringApi.UseConnection(connection =>
            {
                var jobsSql =
                    @"with cte as 
(
  select j.Id, row_number() over (order by j.Id desc) as row_num
  from [Job] j";

                for (var i = 0; i < tags.Length; i++)
                {
                    jobsSql += $"  inner join [Set] s{i} on j.Id=s{i}.[Value] and s{i}.[Key] = ?";
                }

                jobsSql +=
                    $@")
select j.StateName AS [Key], count(*) AS [Value]
from [Job] j 
inner join cte on cte.Id = j.Id 
inner join [State] s on j.StateId = s.Id
group by j.StateName order by count(*) desc
LIMIT {maxTags};";

                return connection.Database.Query<KeyValuePair<string, int>>(jobsSql, tags)
                    .ToDictionary(d => d.Key, d => d.Value);
            });
        }

        public override JobList<MatchingJobDto> GetMatchingJobs(JobStorage jobStorage, string[] tags, int from, int count, string stateName = null)
        {
            var monitoringApi = GetMonitoringApi(jobStorage);
            return monitoringApi.UseConnection(connection => GetJobs(connection.Database, from, count, tags, stateName,
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

        private int GetJobCount(SQLiteConnection connection, string[] tags, string stateName)
        {
            var jobsSql =
                @"with cte as
(
  select j.Id, row_number() over (order by j.Id desc) as row_num
  from [Job] j";

            for (var i = 0; i < tags.Length; i++)
            {
                jobsSql += $"  inner join [Set] s{i} on j.Id=s{i}.[Value] and s{i}.[Key] = ?";
            }

            jobsSql +=
                $@"
  where ('{stateName}' IS NULL OR LENGTH('{stateName}')=0 OR j.StateName='{stateName}')
)
select count(*)
from [Job] j
inner join cte on cte.Id = j.Id 
left join [State] s on j.StateId = s.Id;";

            return connection.ExecuteScalar<int>(jobsSql, tags);
        }

        private JobList<TDto> GetJobs<TDto>(
            SQLiteConnection connection, int from, int count, string[] tags, string stateName,
            Func<SQLiteJob, Job, SafeDictionary<string, string>, TDto> selector)
        {
            var jobsSql =
                @"with cte as
(
  select j.Id, row_number() over (order by j.Id desc) as row_num
  from [Job] j";

            for (var i = 0; i < tags.Length; i++)
            {
                jobsSql += $"  inner join [Set] s{i} on j.Id=s{i}.[Value] and s{i}.[Key] = ?";
            }

            jobsSql +=
                $@"
  where ('{stateName}' IS NULL OR LENGTH('{stateName}') = 0 OR j.StateName='{stateName}')
)
select j.*, s.Reason as StateReason, s.Data as StateData
from [Job] j
inner join cte on cte.Id = j.Id 
left join [State] s on j.StateId = s.Id
where cte.row_num between {from + 1} and {from + count} 
order by j.Id desc;";

            var jobs = connection.Query<SQLiteJob>(jobsSql, tags) .ToList();

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
            ICollection<SQLiteJob> jobs,
            Func<SQLiteJob, Job, SafeDictionary<string, string>, TDto> selector)
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
