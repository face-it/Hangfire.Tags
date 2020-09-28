using System;
using System.Linq;
using System.Reflection;
using Hangfire.Client;
using Hangfire.Tags.Attributes;

namespace Hangfire.Tags.States
{
    internal class CreateJobFilter : IClientFilter
    {
        public void OnCreating(CreatingContext filterContext)
        {
        }

        public void OnCreated(CreatedContext filterContext)
        {
            var mi = filterContext.Job.Method;

            var attrs = mi.GetCustomAttributes<TagAttribute>()
                .Union(mi.DeclaringType?.GetCustomAttributes<TagAttribute>() ?? Enumerable.Empty<TagAttribute>())
                .SelectMany(t => t.Tag).ToList();

            if (!attrs.Any())
                return;

            if (filterContext.BackgroundJob?.Id == null)
                throw new ArgumentException("Background Job cannot be null", nameof(filterContext));

            var args = filterContext.Job.Args.ToArray();
            var tags = attrs.Select(tag => string.Format(tag, args)).Where(a => !string.IsNullOrEmpty(a));
            filterContext.BackgroundJob.Id.AddTags(tags);
        }
    }
}
