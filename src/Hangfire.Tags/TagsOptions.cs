using System.Collections.Generic;
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

        internal static Dictionary<string, TagsOptions> OptionsDictionary { get; set; } = new Dictionary<string, TagsOptions>();

        public string TagColor { get; set; }

        public string TextColor { get; set; }

        public TagsListStyle TagsListStyle { get; set; } = TagsListStyle.LinkButton; // default
    }
}