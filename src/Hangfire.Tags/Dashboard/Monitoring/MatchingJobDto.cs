using System;
using Hangfire.Common;

namespace Hangfire.Tags.Dashboard.Monitoring
{
    public class MatchingJobDto
    {
        public Job Job { get; set; }
        public string State { get; set; }
        public DateTime? EnqueuedAt { get; set; }
    }
}
