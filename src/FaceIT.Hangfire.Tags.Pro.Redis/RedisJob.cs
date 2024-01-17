using System;
using System.Collections.Generic;

namespace Hangfire.Tags.Pro.Redis
{
    internal class RedisJob
    {
        public DateTime CreatedAt { get; set; }
        public string StateName { get; set; }
        public Dictionary<string, string> StateData { get; set; }
    }
}