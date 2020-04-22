using System;
using Hangfire.Storage;

namespace Hangfire.Tags.Storage
{
    internal class TagExpirationTransaction : IDisposable
    {
        private readonly TagsStorage _tagsStorage;
        private readonly JobStorageTransaction _transaction;

        public TagExpirationTransaction(JobStorage jobStorage, JobStorageTransaction transaction)
        : this(new TagsStorage(jobStorage), transaction)
        {
        }

        public TagExpirationTransaction(TagsStorage tagsStorage, JobStorageTransaction transaction)
        {
            _tagsStorage = tagsStorage ?? throw new ArgumentNullException(nameof(tagsStorage));
            _transaction = transaction ?? throw new ArgumentNullException(nameof(transaction));
        }

        public void Dispose()
        {
            _transaction.Dispose();
            _tagsStorage.Dispose();
        }

        public void Expire(string jobid, TimeSpan expireIn)
        {
            if (string.IsNullOrEmpty(jobid))
                throw new ArgumentNullException(nameof(jobid));

            _transaction.ExpireSet(jobid.GetSetKey(), expireIn);

            var tagTransaction = TagsOptions.Options.Storage?.GetTransaction(_transaction);
            if (tagTransaction == null)
                return;

            var tags = _tagsStorage.GetTags(jobid);
            foreach (var tag in tags)
            {
                tagTransaction.ExpireSetValue(tag.GetSetKey(), jobid, expireIn);
            }
        }

        public void Persist(string jobid)
        {
            if (string.IsNullOrEmpty(jobid))
                throw new ArgumentNullException(nameof(jobid));

            _transaction.PersistSet(jobid.GetSetKey());

            var tagTransaction = TagsOptions.Options.Storage?.GetTransaction(_transaction);
            if (tagTransaction == null)
                return;

            var tags = _tagsStorage.GetTags(jobid);
            foreach (var tag in tags)
            {
                tagTransaction.PersistSetValue(tag.GetSetKey(), jobid);
            }
        }
    }
}