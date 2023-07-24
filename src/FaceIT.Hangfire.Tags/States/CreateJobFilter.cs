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

            if (filterContext.Job.Method.DeclaringType != filterContext.Job.Type)
            {
                // This job (or method) is from an inherited type, we should use reflection to retrieve the correct Job method
                var dmi = filterContext.Job.Type.GetMethod(filterContext.Job.Method.Name,
                    filterContext.Job.Method.GetParameters().Select(p => p.ParameterType).ToArray());
                mi = dmi ?? mi;
            }

            var attrs = mi.GetCustomAttributes<TagAttribute>()
                .Union(filterContext.BackgroundJob?.Job.Type?.GetCustomAttributes<TagAttribute>() ?? Enumerable.Empty<TagAttribute>())
                .Union(mi.DeclaringType?.GetCustomAttributes<TagAttribute>() ?? Enumerable.Empty<TagAttribute>())
                .SelectMany(t => t.Tag).ToList();

            if (!attrs.Any())
                return;

            if (filterContext.BackgroundJob?.Id == null)
                return;
//                throw new ArgumentException("Background Job cannot be null", nameof(filterContext));

            var args = filterContext.Job.Args.ToArray();
            var tags = attrs.Select(tag => string.Format(tag, args)).Where(a => !string.IsNullOrEmpty(a));
            filterContext.BackgroundJob.Id.AddTags(tags);
        }
    }
}
