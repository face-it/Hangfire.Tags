
namespace JobsInterfaces
{
    using Hangfire.Server;

    public interface IOtherDummyJob
    {
        void DoJob(PerformContext context);
    }
}
