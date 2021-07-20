using Hangfire.Server;
using Hangfire.Tags;
using JobsInterfaces;

namespace HangfireServerTwo
{
    public class OtherDummyJob : IOtherDummyJob
    {
        public void DoJob(PerformContext context)
        {
            context?.AddTags("server-two-tag-three", "server-two-tag-four", "server-two-tag-five");
        }
    }
}
