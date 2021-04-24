using Hangfire.States;
using Hangfire.Storage;
using Hangfire.Tags.Storage;

namespace Hangfire.Tags.States
{
    internal class TagsCleanupStateFilter : IApplyStateFilter
    {
        public void OnStateApplied(ApplyStateContext context, IWriteOnlyTransaction transaction)
        {
            using (var expireTransaction = new TagExpirationTransaction(context.Storage, (JobStorageTransaction)transaction))
            {
                var jobid = context.BackgroundJob.Id;

                if (context.NewState.IsFinal)
                    // Final state, so set the tags expiration
                    expireTransaction.Expire(jobid, context.JobExpirationTimeout);
                else
                    // Only remove tags if the job is going to be removed
                    expireTransaction.Persist(jobid);
            }
        }

        public void OnStateUnapplied(ApplyStateContext context, IWriteOnlyTransaction transaction)
        {
            // We don't need to unapply the cleanup of tags.
        }
    }
}
