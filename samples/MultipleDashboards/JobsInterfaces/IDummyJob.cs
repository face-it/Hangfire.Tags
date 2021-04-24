
namespace JobsInterfaces
{
    using Hangfire.Server;

    public interface IDummyJob
    {
        void DoJob(PerformContext context);
    }
}
