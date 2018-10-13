using Hangfire.Tags.Storage;

namespace Hangfire.Tags
{
    /// <summary>
    /// Configuration options for tags
    /// </summary>
    public class TagsOptions
    {
        public ITagsServiceStorage Storage { get; set; }

        internal static TagsOptions Options { get; set; }

        public string TagColor { get; set; }

        public string TextColor { get; set; }
    }
}
