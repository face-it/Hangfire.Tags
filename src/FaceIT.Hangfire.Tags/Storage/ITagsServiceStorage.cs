using System;
using System.Collections.Generic;
using Hangfire.Storage;
using Hangfire.Storage.Monitoring;
using Hangfire.Tags.Dashboard.Monitoring;

namespace Hangfire.Tags.Storage
{
    public interface ITagsServiceStorage
    {
        ITagsTransaction GetTransaction(IWriteOnlyTransaction transaction);

        /// <summary>
        /// Search for matching tags and returns the tags with the weight of the tag, calculated as a percentage.
        /// </summary>
        /// <param name="tag">An optional search string containing the partial or full tag.</param>
        /// <param name="setKey">The keyprefix used for the keys in the set.</param>
        /// <returns>A list of tags that match the specified name, combined with the weight of the tag, calculated as a percentage.</returns>
        [Obsolete("Pass the JobStorage when invocing this method")]
        IEnumerable<TagDto> SearchWeightedTags(string tag = null, string setKey = IdExtensions.SetKey);

        /// <summary>
        /// Search for matching tags and returns the tags with the weight of the tag, calculated as a percentage.
        /// </summary>
        /// <param name="jobStorage">The jobstorage to use when multiple storages are defined.</param>
        /// <param name="tag">An optional search string containing the partial or full tag.</param>
        /// <param name="setKey">The keyprefix used for the keys in the set.</param>
        /// <returns>A list of tags that match the specified name, combined with the weight of the tag, calculated as a percentage.</returns>
        IEnumerable<TagDto> SearchWeightedTags(JobStorage jobStorage, string tag = null, string setKey = IdExtensions.SetKey);

        /// <summary>
        /// Searches for tags that are used in combination with the specified tag.
        /// </summary>
        /// <param name="tag">A search string containing the partial or full tag.</param>
        /// <param name="setKey">The keyprefix used for the keys in the set.</param>
        /// <returns>A list of tags that are used in combination with the specified tag.</returns>
        [Obsolete("Pass the JobStorage when invocing this method")]
        IEnumerable<string> SearchRelatedTags(string tag, string setKey = IdExtensions.SetKey);

        /// <summary>
        /// Searches for tags that are used in combination with the specified tag.
        /// </summary>
        /// <param name="jobStorage">The jobstorage to use when multiple storages are defined.</param>
        /// <param name="tag">A search string containing the partial or full tag.</param>
        /// <param name="setKey">The keyprefix used for the keys in the set.</param>
        /// <returns>A list of tags that are used in combination with the specified tag.</returns>
        IEnumerable<string> SearchRelatedTags(JobStorage jobStorage, string tag, string setKey = IdExtensions.SetKey);

        /// <summary>
        /// Get the amount of matching jobs using the specified tags. The tags should be a full tag, not a partial tag name.
        /// </summary>
        /// <param name="tags">One or more tags which are used to filter the job list. All tags must exist on the job before it matches.</param>
        /// <param name="stateName">The optional name of the state.</param>
        /// <returns></returns>
        [Obsolete("Pass the JobStorage when invocing this method")]
        int GetJobCount(string[] tags, string stateName = null);

        /// <summary>
        /// Get the amount of matching jobs using the specified tags. The tags should be a full tag, not a partial tag name.
        /// </summary>
        /// <param name="jobStorage">The jobstorage to use when multiple storages are defined.</param>
        /// <param name="tags">One or more tags which are used to filter the job list. All tags must exist on the job before it matches.</param>
        /// <param name="stateName">The optional name of the state.</param>
        /// <returns></returns>
        int GetJobCount(JobStorage jobStorage, string[] tags, string stateName = null);

        /// <summary>
        /// Gets the different states for the specified tags, with the amount of times a tag is used in a certain state.
        /// </summary>
        /// <param name="tags">One or more tags which are used to filter the job list. All tags must exist on the job before it matches.</param>
        /// <param name="maxTags">The maximum amount of tags to return</param>
        /// <returns></returns>
        [Obsolete("Pass the JobStorage when invocing this method")]
        IDictionary<string, int> GetJobStateCount(string[] tags, int maxTags = 50);

        /// <summary>
        /// Gets the different states for the specified tags, with the amount of times a tag is used in a certain state.
        /// </summary>
        /// <param name="jobStorage">The jobstorage to use when multiple storages are defined.</param>
        /// <param name="tags">One or more tags which are used to filter the job list. All tags must exist on the job before it matches.</param>
        /// <param name="maxTags">The maximum amount of tags to return</param>
        /// <returns></returns>
        IDictionary<string, int> GetJobStateCount(JobStorage jobStorage, string[] tags, int maxTags = 50);

        /// <summary>
        /// Get matching jobs using the specified tags. The tags should be a full tag, not a partial tag name.
        /// </summary>
        /// <param name="tags">One or more tags which are used to filter the job list. All tags must exist on the job before it matches.</param>
        /// <param name="from">The starting record</param>
        /// <param name="count">The amount of results.</param>
        /// <param name="stateName">The optional name of the state.</param>
        /// <returns></returns>
        [Obsolete("Pass the JobStorage when invocing this method")]
        JobList<MatchingJobDto> GetMatchingJobs(string[] tags, int from, int count, string stateName = null);

        /// <summary>
        /// Get matching jobs using the specified tags. The tags should be a full tag, not a partial tag name.
        /// </summary>
        /// <param name="jobStorage">The jobstorage to use when multiple storages are defined.</param>
        /// <param name="tags">One or more tags which are used to filter the job list. All tags must exist on the job before it matches.</param>
        /// <param name="from">The starting record</param>
        /// <param name="count">The amount of results.</param>
        /// <param name="stateName">The optional name of the state.</param>
        /// <returns></returns>
        JobList<MatchingJobDto> GetMatchingJobs(JobStorage jobStorage, string[] tags, int from, int count, string stateName = null);

        /// <summary>
        /// Get all the tags for a specific job.
        /// </summary>
        /// <param name="jobStorage">The jobstorage to use when multiple storages are defined.</param>
        /// <param name="jobId">The job id.</param>
        /// <returns>A list of all tags which are attached to the job.</returns>
        string[] GetTags(JobStorage jobStorage, string jobId);

        /// <summary>
        /// Get the amount of unique tags 
        /// </summary>
        /// <param name="jobStorage">The jobstorage to use when multiple storages are defined.</param>
        /// <param name="setKey">The keyprefix used for the keys in the set.</param>
        /// <returns></returns>
        long GetTagCount(JobStorage jobStorage, string setKey = IdExtensions.SetKey);
    }
}
