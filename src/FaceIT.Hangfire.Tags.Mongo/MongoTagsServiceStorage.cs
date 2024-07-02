using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Hangfire.Common;
using Hangfire.Mongo;
using Hangfire.Mongo.Database;
using Hangfire.Mongo.Dto;
using Hangfire.Storage;
using Hangfire.Storage.Monitoring;
using Hangfire.Tags.Dashboard.Monitoring;
using Hangfire.Tags.Storage;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Hangfire.Tags.Mongo
{
    public class MongoTagsServiceStorage : ObsoleteBaseStorage, ITagsServiceStorage
    {
        private readonly MongoStorageOptions _options;

        private static MongoTagsMonitoringApi GetMonitoringApi(JobStorage jobStorage)
        {
            return new MongoTagsMonitoringApi(jobStorage.GetMonitoringApi());
        }

        public MongoTagsServiceStorage(MongoStorageOptions options = null)
        {
            _options = options ?? new MongoStorageOptions();
        }

        public override ITagsTransaction GetTransaction(IWriteOnlyTransaction transaction)
        {
            return new MongoTagsTransaction(_options, transaction);
        }

        public override IEnumerable<TagDto> SearchWeightedTags(JobStorage jobStorage, string tag = null, string setKey = "tags")
        {
            // Hangfire.Mongo stores Set values in the Key property of a document.
            // The value of the Key property is formatted Key<Value>, e.g.: recurring-jobs<Failed Task>
            // Tags are stored twice (it's a double linked list). If job with Id 1 has a tag 'b', we store the following sets:
            // Key          Value
            // tags         b
            // tags:1       b
            // tags:b       1
            // Hangfire.Mongo stores this different, because it uses the Key property only:
            // Key              SetType     Value
            // tags:b<1>        tags
            // tags:1<b>        tags
            // tags<b>          tags        b
            // tags<1>          tags        1
            // Since we're only interested in the document with key tags<b>, we're going to exclude tags<1> with a regular expression (Hangfire.Mongo uses 24 character id's).

            // db.getCollection("<prefix>.jobGraph").find({ Key: { $regex: /tags<.+>$/ }, _t: "SetDto", SetType: "tags", Key: { $not: { $regex: /tags<[0-9a-f]{24}>/ } } })
            
            var monitoringApi = GetMonitoringApi(jobStorage);

            var excludeTagsFilter = setKey + "<[0-9a-f]{24}>";
            if (string.IsNullOrEmpty(tag))
                tag = ".+";

            var filterRegex = $@"{setKey}<{tag}>$";
            var builderBson = Builders<BsonDocument>.Filter;
            var filterBson = CreateFilter(builderBson, filterRegex, excludeTagsFilter);

            var mainQuery = monitoringApi.DbContext.Database.GetCollection<BsonDocument>($"{_options.Prefix}.jobGraph").Aggregate()
                .Match(filterBson).As<SetDto>();
            
            var total = mainQuery.Count().Single().Count;
            var grp = mainQuery.Group(x => x.Value,
                x => new TagDto
                {
                    Tag = x.Key, Amount = x.Count()
                }).ToList();
            
            grp.ForEach(g => g.Percentage = g.Amount * 1.0 / total * 100.0);

            return grp;
        }

        private static FilterDefinition<T> CreateFilter<T>(FilterDefinitionBuilder<T> builder, string filterRegex, string excludeTagsFilter) =>
            builder.Regex("Key", new BsonRegularExpression(filterRegex))
            & builder.Eq("_t", "SetDto")
            & builder.Eq("SetType", "tags")
            & builder.Not(builder.Regex("Key", excludeTagsFilter));

        public override IEnumerable<string> SearchRelatedTags(JobStorage jobStorage, string tag, string setKey = "tags")
        {
            var monitoringApi = GetMonitoringApi(jobStorage);
            
            // By passing the tag twice, the method will search for related tags, and return them in the Tags property
            var baseQuery = CreateBaseQuery(monitoringApi.DbContext, new[] { setKey + ":" + tag, setKey + ":" + tag }, null);
            
            // The baseQuery now contains a JobDto with a Tags property, which is filled with the second tags
            // We will extract those tags, and add them to a single distinct array. The next pipeline expressions are doing that:
            // { "$project": { "_id": 0, "Tags": "$Tags.SetType" } },
            // { "$unwind": "$Tags" },
            // { "$match": { "$expr": { "$and": [ {"$gt": [ { "$strLenCP": "$Tags" }, 5 ] }, { "$not": { "$regexMatch": { "input": "$Tags", "regex": /tags:[0-9a-f]{24}/ } } } ] } } }
            // { "$group": { "_id": null, "Tags": { "$addToSet": { "$substr: ["$Tags", 5, <length>] } } } },
            // { "$project": { "_id": 0 } }
            
            // This will result in an object with a single property, named Tags, which is a string array. 
            
            var firstProjectQuery = BsonDocument.Parse("{ \"_id\": 0, \"Tags\": \"$Tags.SetType\" }");
            var secondProjectQuery = BsonDocument.Parse("{ \"_id\": 0 }");
            var excludeEmptyAndIdTags =
                BsonDocument.Parse(
                    "{ \"$expr\": { \"$and\": [ {\"$gt\": [ { \"$strLenCP\": \"$Tags\" }, " + (setKey.Length + 1) +
                    " ] }, { \"$not\": { \"$regexMatch\": { \"input\": \"$Tags\", \"regex\": /" + setKey +
                    ":[0-9a-f]{24}/ } } } ] } }");
            var groupQuery =
                BsonDocument.Parse(
                    "{ \"_id\": null, \"Tags\": { \"$addToSet\": { \"$substr\": [\"$Tags\", " +
                    (setKey.Length + 1) + ", -1] } } }");
            var qry = baseQuery.Project(firstProjectQuery).Unwind("Tags").Match(excludeEmptyAndIdTags).Group(groupQuery)
                .Project(secondProjectQuery).As<TagListDto>();

            return qry.SingleOrDefault()?.Tags ?? Enumerable.Empty<string>();
        }

        public override int GetJobCount(JobStorage jobStorage, string[] tags, string stateName = null)
        {
            var monitoringApi = GetMonitoringApi(jobStorage);
            return GetJobCount(monitoringApi.DbContext, tags, stateName);
        }

        public override IDictionary<string, int> GetJobStateCount(JobStorage jobStorage, string[] tags, int maxTags = 50)
        {
            var monitoringApi = GetMonitoringApi(jobStorage);
            var baseQry = CreateBaseQuery(monitoringApi.DbContext, tags, null);

            var qry = baseQry
                .Group(x => x.StateName,
                    x => new { StateName = x.Key, Amount = x.Count() }).Limit(maxTags);

            return qry.ToList().ToDictionary(l => l.StateName, l => l.Amount);
        }

        public override JobList<MatchingJobDto> GetMatchingJobs(JobStorage jobStorage, string[] tags, int from, int count, string stateName = null)
        {
            var monitoringApi = GetMonitoringApi(jobStorage);
            return GetJobs(monitoringApi.DbContext, from, count, tags, stateName,
                (sqlJob, job, stateData) =>
                    new MatchingJobDto
                    {
                        Job = job,
                        State = sqlJob.StateName,
                        CreatedAt = sqlJob.CreatedAt,
                        ResultAt = GetStateDate(stateData, sqlJob.StateName),
                        EnqueueAt = GetNullableStateDate(stateData, "Enqueue")
                    });
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

        private int GetJobCount(HangfireDbContext context, string[] tags, string stateName)
        {
            var qry = CreateBaseQuery(context, tags, stateName);
            return (int) qry.Count().Single().Count;
        }

        private JobList<TDto> GetJobs<TDto>(
            HangfireDbContext connection, int from, int count, string[] tags, string stateName,
            Func<JobDto, Job, SafeDictionary<string, string>, TDto> selector)
        {
            var baseQry = CreateBaseQuery(connection, tags, stateName);

            var qry = baseQry
                .Sort(Builders<JobDto>.Sort.Descending("Parameters.Time")).Skip(from).Limit(count);
            
            var jobs = qry.ToList();

            return DeserializeJobs(jobs, selector);
        }

        private IAggregateFluent<JobDto> CreateBaseQuery(HangfireDbContext connection, string[] tags, string stateName)
        {
            // A list of all job id's for a the tags:
            // db.getCollection("<prefix>.jobGraph").find({"_t": "SetDto", SetType: { $in: [<comma seperated quoted tags>]}})

            // A list of all jobs in a state:
            // db.getCollection("<prefix>.jobGraph").find({"_t": "JobDto", StateName: '<statename>'})

            // We're going to use the following query for this
            // db.getCollection("<prefix>.jobGraph).aggregate([
            //      // 1. Get jobs only, conditionally for a specific state
            //      { $match: { "_t": "JobDto" } } -or-
            //      { $match: { "_t": "JobDto", "StateName": "<statename>" } }
            //      // 2. For every tag, add a lookup with a match not null (inner join in regular SQL)
            //      { $lookup:
            //        {
            //            from: "<prefix>.jobGraph",
            //            let: { jobId<tagIndex>: { $toString: "$_id" } },
            //            pipeline: [
            //              { $match:
            //                  { $expr:
            //                      { $and: 
            //                          { $in: [ "SetDto", "$_t" ] },
            //                          { $eq: [ "$SetType", "<tag>"] },
            //                          { $eq: [ "$Value", "$$jobId<tagIndex> ] }
            //                        ]
            //                      }
            //                  }
            //               }
            //            ],
            //            as: "Tag<tagIndex>"
            //        }
            //      },
            //      { $match: { "Tag<tagindex>": { $ne: [] } } },
            //      { $project: { "Tag<tagindex>": 0 } }
            //      3. Return a list of JobDto objects
            // ])

            var builder = Builders<BsonDocument>.Filter;

            var filterStep1 = string.IsNullOrEmpty(stateName)
                ? builder.Eq("_t", "JobDto")
                : builder.And(builder.Eq("_t", "JobDto"), builder.Eq("StateName", stateName)); 

            var collection = connection.Database.GetCollection<BsonDocument>($"{_options.Prefix}.jobGraph");

            var searchRelatedTags = tags.Length == 2 && tags[0] == tags[1];
            
            var step1 = collection.Aggregate().Match(filterStep1);

            var step2 = step1;
            for (var i = 0; i < tags.Length; i++)
            {
                var tag = tags[i];

                // If we're searching for related tags, the first tag should match the given tag, but the second tag should NOT match the given tag.
                // This way, we can return a list of additional tags, which are used in combination with the first tag.
                var operation = searchRelatedTags && i == 1 ? "ne" : "eq";
                
                // https://jira.mongodb.org/browse/CSHARP-2084
                var expression =
                    "{ $expr: { $and: [{ $in: [ \"SetDto\", \"$_t\" ] }, { $" + operation + ": [ \"$SetType\", \"" + tag +
                    "\" ] }, { $eq: [ \"$Value\", \"$$jobId\" ] } ] } }";
                
                var let = new BsonDocument("jobId", new BsonDocument("$toString", "$_id"));
                var lookupFilter = PipelineStageDefinitionBuilder.Match<BsonDocument>(expression);
                var pipeline = new EmptyPipelineDefinition<BsonDocument>()
                    .AppendStage(lookupFilter);
                
                step2 = step2.Lookup(collection, let, pipeline, "Tags");
                
                var notEmptyFilter = builder.Ne("Tags", Array.Empty<BsonDocument>());
                step2 = step2.Match(notEmptyFilter);

                // Keep the last Tags property, if we're searching for related tags, we are interested in them
                if (searchRelatedTags && i == 1) continue;
                
                var project = new BsonDocument("Tags", 0);
                step2 = step2.Project(project);
            }
            
            return step2.As<JobDto>();
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
            ICollection<JobDto> jobs,
            Func<JobDto, Job, SafeDictionary<string, string>, TDto> selector)
        {
            var result = new List<KeyValuePair<string, TDto>>(jobs.Count);

            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach (var job in jobs)
            {
                var dto = default(TDto);

                if (job.InvocationData != null)
                {
                    var deserializedData = job.StateHistory.Last().Data;
                    var stateData = deserializedData != null
                        ? new SafeDictionary<string, string>(deserializedData, StringComparer.OrdinalIgnoreCase)
                        : null;

                    dto = selector(job, DeserializeJob(job.InvocationData, job.Arguments), stateData);
                }

                result.Add(new KeyValuePair<string, TDto>(job.Id.ToString(), dto));
            }

            return new JobList<TDto>(result);
        }

        public override long GetTagCount(JobStorage jobStorage, string setKey = "tags")
        {
            var monitoringApi = GetMonitoringApi(jobStorage);

            var excludeTagsFilter = setKey + "<[0-9a-f]{24}>";
            var filterRegex = $@"{setKey}<.+>$";
            var builderBson = Builders<BsonDocument>.Filter;
            var filterBson = CreateFilter(builderBson, filterRegex, excludeTagsFilter);

            var mainQuery = monitoringApi.DbContext.Database.GetCollection<BsonDocument>($"{_options.Prefix}.jobGraph").Aggregate()
                .Match(filterBson).As<SetDto>();
            
            return mainQuery.Count().Single().Count;
        }

        public override string[] GetTags(JobStorage jobStorage, string jobId)
        {
            var regex = new Regex("[0-9a-f]{24}");
            return base.GetTags(jobStorage, jobId).Where(t => !regex.IsMatch(t)).ToArray();
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
