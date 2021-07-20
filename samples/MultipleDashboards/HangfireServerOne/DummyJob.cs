namespace HangfireServerOne
{
    using Hangfire.Server;
    using Hangfire.Tags;

    using JobsInterfaces;

    public class DummyJob : IDummyJob
    {
        public void DoJob(PerformContext context)
        {
            context?.AddTags("server-one-tag-one", "server-one-tag-two");
        }
    }
}
