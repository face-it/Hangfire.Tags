using Hangfire.Dashboard;
using System;
using System.Text;
using System.Threading.Tasks;

namespace Hangfire.Tags.Support
{
    /// <summary>
    /// Dispatcher for configured styles
    /// </summary>
    internal class DynamicCssDispatcher : IDashboardDispatcher
    {
        private readonly TagsOptions _options;

        public DynamicCssDispatcher(TagsOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        public Task Dispatch(DashboardContext context)
        {
            var builder = new StringBuilder();

            if (!string.IsNullOrEmpty(_options.TagColor) || !string.IsNullOrEmpty(_options.TextColor))
            {
                builder.AppendLine(".tags > .label-info {");
                if (!string.IsNullOrEmpty(_options.TagColor))
                    builder.Append("    background-color: ").Append(_options.TagColor).AppendLine(";");
                if (!string.IsNullOrEmpty(_options.TextColor))
                    builder.Append("    color: ").Append(_options.TextColor).AppendLine(";");
                builder.AppendLine("}");
            }

            return context.Response.WriteAsync(builder.ToString());
        }
    }
}