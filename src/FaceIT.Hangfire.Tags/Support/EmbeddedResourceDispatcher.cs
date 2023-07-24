using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Hangfire.Dashboard;

namespace Hangfire.Tags.Support
{
    internal class Resource
    {
        public Resource(string resourceName, bool hasDarkMode)
        {
            ResourceName = resourceName;
            HasDarkMode = hasDarkMode;
        }

        public string ResourceName { get; }
        public bool HasDarkMode { get; }
    }

    /// <summary>
    /// Alternative to built-in EmbeddedResourceDispatcher, which (for some reasons) is not public.
    /// </summary>
    internal class EmbeddedResourceDispatcher : IDashboardDispatcher
    {
        private readonly Assembly _assembly;
        private readonly Resource[] _resources;
        private readonly string _contentType;

        public EmbeddedResourceDispatcher(Assembly assembly, Resource[] resources, string contentType = null)
        {
            if (resources == null || resources.Length == 0)
                throw new ArgumentNullException(nameof(resources));

            _assembly = assembly ?? throw new ArgumentNullException(nameof(assembly));
            _resources = resources;
            _contentType = contentType;
        }

        public Task Dispatch(DashboardContext context)
        {
            if (!string.IsNullOrEmpty(_contentType))
            {
                var contentType = context.Response.ContentType;

                if (string.IsNullOrEmpty(contentType))
                {
                    // content type not yet set
                    context.Response.ContentType = _contentType;
                }
                else if (contentType != _contentType)
                {
                    // content type already set, but doesn't match ours
                    throw new InvalidOperationException($"ContentType '{_contentType}' conflicts with '{context.Response.ContentType}'");
                }
            }

            var resourceNames = _resources.Select(r =>
                r.HasDarkMode ? string.Format(r.ResourceName, context.Options.DarkModeEnabled ? "dark" : "light") : r.ResourceName).ToArray();

            return WriteResourceAsync(context.Response, _assembly, resourceNames);
        }

        private static async Task WriteResourceAsync(DashboardResponse response, Assembly assembly, string[] resourceNames)
        {
            foreach (var resourceName in resourceNames)
            {
                using (var stream = assembly.GetManifestResourceStream(resourceName))
                {
                    if (stream == null)
                        throw new ArgumentException($@"Resource '{resourceName}' not found in assembly {assembly}.");

                    await stream.CopyToAsync(response.Body);
                }
            }
        }
    }
}