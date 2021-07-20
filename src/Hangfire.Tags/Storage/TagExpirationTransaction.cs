using System;
using Hangfire.Storage;
using Hangfire.Tags.Dashboard;

namespace Hangfire.Tags.Storage
{
    internal class TagExpirationTransaction : IDisposable
    {
        private readonly TagsStorage _tagsStorage;
        private readonly JobStorageTransaction _transaction;
        private readonly ITagsServiceStorage _serviceStorage;

        public TagExpirationTransaction(JobStorage jobStorage, JobStorageTransaction transaction)
            : this(jobStorage.FindRegistration().Item2, new TagsStorage(jobStorage), transaction)
        {
        }

        public TagExpirationTransaction(ITagsServiceStorage tagsServiceStorage, TagsStorage tagsStorage,
            JobStorageTransaction transaction)
        {
            _tagsStorage = tagsStorage ?? throw new ArgumentNullException(nameof(tagsStorage));
            _transaction = transaction ?? throw new ArgumentNullException(nameof(transaction));
            _serviceStorage = tagsServiceStorage;
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

            var tagTransaction = _serviceStorage.GetTransaction(_transaction);
            if (tagTransaction == null)
                return;

            var tags = _tagsStorage.GetTags(jobid);
            foreach (var tag in tags)
            {
                var key = tag.GetSetKey();
                tagTransaction.ExpireSetValue(key, jobid, expireIn);
                
                if (_tagsStorage.Connection.GetSetCount(key) == 0)
                    tagTransaction.ExpireSetValue("tags", key, expireIn);
            }
        }

        public void Persist(string jobid)
        {
            if (string.IsNullOrEmpty(jobid))
                throw new ArgumentNullException(nameof(jobid));

            _transaction.PersistSet(jobid.GetSetKey());

            var tagTransaction = _serviceStorage.GetTransaction(_transaction);
            if (tagTransaction == null)
                return;

            var tags = _tagsStorage.GetTags(jobid);
            foreach (var tag in tags)
            {
                var key = tag.GetSetKey();
                tagTransaction.PersistSetValue(key, jobid);
                tagTransaction.PersistSetValue("tags", key);
            }
        }
    }
}