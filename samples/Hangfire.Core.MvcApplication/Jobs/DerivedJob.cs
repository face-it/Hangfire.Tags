using System.ComponentModel;
using Hangfire.Tags.Attributes;

namespace Hangfire.Core.MvcApplication.Jobs
{
    [Tag("derived-job-class")]
    public class DerivedJob : BaseJob
    {
        [Tag("derived-job-method")]
        [DisplayName("derived-job-method")]
        public override void Run()
        {
            base.Run();
        }
    }
}