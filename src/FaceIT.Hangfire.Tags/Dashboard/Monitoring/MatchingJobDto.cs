using System;
using Hangfire.Common;

namespace Hangfire.Tags.Dashboard.Monitoring
{
    public class MatchingJobDto
    {
        public Job Job { get; set; }
        public string State { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? ResultAt {get; set;}
        public DateTime? EnqueueAt { get; set; }
    }
}
