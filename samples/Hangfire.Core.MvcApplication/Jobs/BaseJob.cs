using System.ComponentModel;
using Hangfire.Tags.Attributes;

namespace Hangfire.Core.MvcApplication.Jobs
{
    [Tag("base-job-class")]
    public class BaseJob
    {
        [Tag("base-job-method")]
        [DisplayName("base-job-method")]
        public virtual void Run()
        {
        }
    }
}
