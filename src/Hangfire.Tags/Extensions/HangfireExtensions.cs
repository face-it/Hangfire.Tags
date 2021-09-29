using System.Collections.Generic;
using System.Linq;
using Hangfire.Server;
using Hangfire.Tags.Storage;

namespace Hangfire.Tags
{
    public static class HangfireExtensions
    {
        public static string AddTags(this string jobid, IEnumerable<string> tags)
        {
            return jobid.AddTags(tags.ToArray());
        }

        public static string AddTags(this string jobid, params string[] tags)
        {
            using (var storage = new TagsStorage(JobStorage.Current))
            {
                storage.AddTags(jobid, tags);
            }

            return jobid;
        }

        public static string[] GetTags(this string jobid)
        {
            using (var storage = new TagsStorage(JobStorage.Current))
            {
                return storage.GetTags(jobid);
            }
        }

        public static PerformContext AddTags(this PerformContext context, IEnumerable<string> tags)
        {
            return context.AddTags(tags.ToArray());
        }

        public static PerformContext AddTags(this PerformContext context, params string[] tags)
        {
            context.BackgroundJob.Id.AddTags(tags);
            return context;
        }
    }
}
