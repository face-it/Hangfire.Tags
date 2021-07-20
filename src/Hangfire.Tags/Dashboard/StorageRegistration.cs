using System;
using System.Collections.Generic;
using Hangfire.Tags.Storage;

namespace Hangfire.Tags.Dashboard
{
    internal static class StorageRegistration
    {
        private static readonly Dictionary<JobStorage, Tuple<TagsOptions, ITagsServiceStorage>> ServiceStorages =
            new Dictionary<JobStorage, Tuple<TagsOptions, ITagsServiceStorage>>();

        internal static void Register(JobStorage jobStorage, TagsOptions options, ITagsServiceStorage serviceStorage)
        {
            ServiceStorages[jobStorage ?? JobStorage.Current] = new Tuple<TagsOptions, ITagsServiceStorage>(options, serviceStorage);
        }

        internal static Tuple<TagsOptions, ITagsServiceStorage> FindRegistration(this JobStorage storage)
        {
            if (ServiceStorages.TryGetValue(storage, out var jobStorageOptions))
                return jobStorageOptions;

            return ServiceStorages.TryGetValue(JobStorage.Current, out var currentOptions) ? currentOptions : default;
        }

    }
}
