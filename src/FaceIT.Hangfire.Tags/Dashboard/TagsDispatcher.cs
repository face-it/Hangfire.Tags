using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Hangfire.Dashboard;
using Hangfire.Tags.Storage;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Hangfire.Tags.Dashboard
{
    public class TagsDispatcher : IDashboardDispatcher
    {
        private static readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings
        {
            ContractResolver = new DefaultContractResolver()
        };

        public async Task Dispatch(DashboardContext context)
        {
            if (!"POST".Equals(context.Request.Method, StringComparison.OrdinalIgnoreCase))
            {
                context.Response.StatusCode = (int) HttpStatusCode.MethodNotAllowed;
                return;
            }

            var result = new Dictionary<string, string[]>();

            var jobids = await context.Request.GetFormValuesAsync("jobs[]");
            if (jobids.Any())
            {
                using (var storage = new TagsStorage(context.Storage))
                {
                    foreach (var job in jobids)
                    {
                        var tags = storage.GetTags(job);
                        result[job] = tags;
                    }
                }
            }

            var serialized = JsonConvert.SerializeObject(result, JsonSettings);

            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(serialized);
        }
    }
}