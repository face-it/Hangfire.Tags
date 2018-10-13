using System.Collections.Generic;
using System.Text;
using Hangfire.Dashboard;
using Hangfire.Tags.Storage;

namespace Hangfire.Tags.Dashboard
{
    /// <summary>
    /// Renderer for the tags in Processing state
    /// </summary>
    internal class ProcessingStateRenderer
    {
        private readonly TagsOptions _options;

        public ProcessingStateRenderer(TagsOptions options)
        {
            _options = options;
        }

        public NonEscapedString Render(HtmlHelper helper, IDictionary<string, string> stateData)
        {
            var bld = new StringBuilder();

            var page = helper.GetPage();
            if (page.RequestPath.StartsWith("/jobs/details"))
            {
                // Find the jobid
                var jobid = page.RequestPath.Substring(" /jobs/details".Length);

                var tagId = jobid.GetSetKey();

                // Get tags from storage
                using (var storage = new TagsStorage(page.Storage))
                {
                    // TODO: Add the tags to the output!
                }
            }

            return new NonEscapedString(bld.ToString());
        }
    }
}
