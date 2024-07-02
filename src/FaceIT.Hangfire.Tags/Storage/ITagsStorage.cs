using System;
using System.Collections.Generic;

namespace Hangfire.Tags.Storage
{
    /// <inheritdoc />
    /// <summary>
    /// Abstraction over Hangfire's storage API
    /// </summary>
    public interface ITagsStorage : IDisposable
    {
        /// <summary>
        /// Retrieves the monitoring api for tags
        /// </summary>
        /// <returns>The monitoring API for tags</returns>
        ITagsMonitoringApi GetMonitoringApi();

        /// <summary>
        /// Returns all defined tags for the specified id.
        /// </summary>
        /// <param name="id"></param>
        /// <returns>An array with all tags.</returns>
        string[] GetTags(string id);

        /// <summary>
        /// Returns all defined tags
        /// </summary>
        /// <returns>An array with all tags.</returns>
        string[] GetAllTags();

        /// <summary>
        /// Initialize tags for the specified job
        /// </summary>
        /// <param name="id">The job id</param>
        void InitTags(string id);

        /// <summary>
        /// Add's a tag to the specified job
        /// </summary>
        /// <param name="id">The job id</param>
        /// <param name="tag">The tag to add</param>
        void AddTag(string id, string tag);

        void AddTags(string jobid, IEnumerable<string> tags);

        /// <summary>
        /// Removes a tag to the specified job
        /// </summary>
        /// <param name="id">The job id</param>
        /// <param name="tag">The tag to remove</param>
        void RemoveTag(string id, string tag);
        
        void RemoveTags(string id, IEnumerable<string> tags);

        /// <summary>
        /// Expire data for tags
        /// </summary>
        /// <param name="id">The job id</param>
        /// <param name="expireIn">The expiration time</param>
        void Expire(string id, TimeSpan expireIn);
    }
}
