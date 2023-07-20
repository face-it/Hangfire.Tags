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

            var tagColor = context.Options.DarkModeEnabled ? _options.DarkTagColor : _options.TagColor;
            var textColor = context.Options.DarkModeEnabled ? _options.DarkTextColor : _options.TextColor;

            if (!string.IsNullOrEmpty(tagColor) || !string.IsNullOrEmpty(textColor))
            {
                builder.AppendLine(".tags > .label-info {");
                if (!string.IsNullOrEmpty(tagColor))
                    builder.Append("    background-color: ").Append(tagColor).AppendLine(";");
                if (!string.IsNullOrEmpty(textColor))
                    builder.Append("    color: ").Append(textColor).AppendLine(";");
                builder.AppendLine("}");
            }

            return context.Response.WriteAsync(builder.ToString());
        }
    }
}