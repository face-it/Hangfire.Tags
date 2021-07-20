namespace Hangfire.Tags
{
    /// <summary>
    /// Configuration options for tags
    /// </summary>
    public class TagsOptions
    {
        public string TagColor { get; set; }

        public string TextColor { get; set; }

        public TagsListStyle TagsListStyle { get; set; } = TagsListStyle.LinkButton; // default
    }
}