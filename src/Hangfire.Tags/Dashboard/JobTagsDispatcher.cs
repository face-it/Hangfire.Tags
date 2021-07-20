using System;
using System.Net;
using System.Threading.Tasks;
using Hangfire.Dashboard;
using Hangfire.Tags.Storage;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Hangfire.Tags.Dashboard
{
    public class JobTagsDispatcher : IDashboardDispatcher
    {
        internal static readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings
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

            var jobid = context.UriMatch.Groups[1].Value;

            using (var storage = new TagsStorage(context.Storage))
            {
                var tags = storage.GetTags(jobid);
                var serialized = JsonConvert.SerializeObject(tags, JsonSettings);

                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(serialized);
            }
        }
    }
}