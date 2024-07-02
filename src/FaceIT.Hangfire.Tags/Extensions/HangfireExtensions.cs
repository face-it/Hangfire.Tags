using System.Collections.Generic;
using System.Linq;
using Hangfire.Server;
using Hangfire.Tags.Storage;

namespace Hangfire.Tags
{
    public static class HangfireExtensions
    {
        /// <summary>
        /// Adds the tags to the job with the specified id.
        /// </summary>
        /// <param name="jobid">The job identifier</param>
        /// <param name="tags">One or more tags</param>
        /// <returns>The job identifier</returns>
        public static string AddTags(this string jobid, IEnumerable<string> tags)
        {
            return jobid.AddTags(tags.ToArray());
        }

        /// <summary>
        /// Adds the tags to the job with the specified id.
        /// </summary>
        /// <param name="jobid">The job identifier</param>
        /// <param name="tags">One or more tags</param>
        /// <returns>The job identifier</returns>
        public static string AddTags(this string jobid, params string[] tags)
        {
            using (var storage = new TagsStorage(JobStorage.Current))
            {
                storage.AddTags(jobid, tags);
            }

            return jobid;
        }

        /// <summary>
        /// Removes the tags from the job with the specified id.
        /// </summary>
        /// <param name="jobid">The job identifier</param>
        /// <param name="tags">One or more tags</param>
        /// <returns>The job identifier</returns>
        public static string RemoveTags(this string jobid, IEnumerable<string> tags)
        {
            return jobid.RemoveTags(tags.ToArray());
        }

        /// <summary>
        /// Removes the tags from the job with the specified id.
        /// </summary>
        /// <param name="jobid">The job identifier</param>
        /// <param name="tags">One or more tags</param>
        /// <returns>The job identifier</returns>
        public static string RemoveTags(this string jobid, params string[] tags)
        {
            using (var storage = new TagsStorage(JobStorage.Current))
            {
                storage.RemoveTags(jobid, tags);
            }

            return jobid;
        }

        /// <summary>
        /// Retrieves the tags of the job with the specified id.
        /// </summary>
        /// <param name="jobid">The job identifier</param>
        /// <returns>A list of zero or more tags.</returns>
        public static string[] GetTags(this string jobid)
        {
            using (var storage = new TagsStorage(JobStorage.Current))
            {
                return storage.GetTags(jobid);
            }
        }

        /// <summary>
        /// Adds the tags to the job with the specified context.
        /// </summary>
        /// <param name="context">The job context</param>
        /// <param name="tags">One or more tags</param>
        /// <returns>The job context</returns>
        public static PerformContext AddTags(this PerformContext context, IEnumerable<string> tags)
        {
            return context.AddTags(tags.ToArray());
        }

        /// <summary>
        /// Removes the tags from the job with the specified context.
        /// </summary>
        /// <param name="context">The job context</param>
        /// <param name="tags">One or more tags</param>
        /// <returns>The job context</returns>
        public static PerformContext RemoveTags(this PerformContext context, IEnumerable<string> tags)
        {
            return context.RemoveTags(tags.ToArray());
        }

        /// <summary>
        /// Adds the tags to the job with the specified context.
        /// </summary>
        /// <param name="context">The job context</param>
        /// <param name="tags">One or more tags</param>
        /// <returns>The job context</returns>
        public static PerformContext AddTags(this PerformContext context, params string[] tags)
        {
            context.BackgroundJob.Id.AddTags(tags);
            return context;
        }

        /// <summary>
        /// Removes the tags from the job with the specified context.
        /// </summary>
        /// <param name="context">The job context</param>
        /// <param name="tags">One or more tags</param>
        /// <returns>The job context</returns>
        public static PerformContext RemoveTags(this PerformContext context, params string[] tags)
        {
            context.BackgroundJob.Id.RemoveTags(tags);
            return context;
        }

        /// <summary>
        /// Adds the tags to the job with the specified context.
        /// </summary>
        /// <param name="context">The job context</param>
        /// <param name="jobStorage">An instance of a job storage, only required if it differs from JobStorage.Current</param>
        /// <param name="tags">One or more tags</param>
        /// <returns>The job context</returns>
        public static PerformContext AddTags(this PerformContext context, JobStorage jobStorage, IEnumerable<string> tags)
        {
            return context.AddTags(jobStorage, tags.ToArray());
        }

        /// <summary>
        /// Removes the tags from the job with the specified context.
        /// </summary>
        /// <param name="context">The job context</param>
        /// <param name="jobStorage">An instance of a job storage, only required if it differs from JobStorage.Current</param>
        /// <param name="tags">One or more tags</param>
        /// <returns>The job context</returns>
        public static PerformContext RemoveTags(this PerformContext context, JobStorage jobStorage, IEnumerable<string> tags)
        {
            return context.RemoveTags(jobStorage, tags.ToArray());
        }

        /// <summary>
        /// Adds the tags to the job with the specified context.
        /// </summary>
        /// <param name="context">The job context</param>
        /// <param name="jobStorage">An instance of a job storage, only required if it differs from JobStorage.Current</param>
        /// <param name="tags">One or more tags</param>
        /// <returns>The job context</returns>
        public static PerformContext AddTags(this PerformContext context, JobStorage jobStorage, params string[] tags)
        {
            using (var storage = new TagsStorage(jobStorage ?? JobStorage.Current))
            {
                storage.AddTags(context.BackgroundJob.Id, tags);
            }

            return context;
        }

        /// <summary>
        /// Removes the tags from the job with the specified context.
        /// </summary>
        /// <param name="context">The job context</param>
        /// <param name="jobStorage">An instance of a job storage, only required if it differs from JobStorage.Current</param>
        /// <param name="tags">One or more tags</param>
        /// <returns>The job context</returns>
        public static PerformContext RemoveTags(this PerformContext context, JobStorage jobStorage, params string[] tags)
        {
            using (var storage = new TagsStorage(jobStorage ?? JobStorage.Current))
            {
                storage.RemoveTags(context.BackgroundJob.Id, tags);
            }

            return context;
        }

        /// <summary>
        /// Gets the tags monitoring API, which can be used to query tags
        /// </summary>
        /// <param name="jobStorage">The current jobstorage</param>
        /// <returns>An object which implements the ITagsMonitoringApi.</returns>
        public static ITagsMonitoringApi GetTagsMonitoringApi(this JobStorage jobStorage)
        {
            return new TagsStorage(jobStorage);
        }
    }
}
