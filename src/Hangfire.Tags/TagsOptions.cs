using Hangfire.Tags.Storage;

namespace Hangfire.Tags
{
    public enum TagsListStyle
    {
        LinkButton,

        Dropdown
    }

    /// <summary>
    /// Configuration options for tags
    /// </summary>
    public class TagsOptions
    {
        public ITagsServiceStorage Storage { get; set; }

        internal static TagsOptions Options { get; set; }

        public string TagColor { get; set; }

        public string TextColor { get; set; }

        public TagsListStyle TagsListStyle { get; set; } = TagsListStyle.LinkButton; // default
    }
}
