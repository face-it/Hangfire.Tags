namespace Hangfire.Tags
{
    /// <summary>
    /// Configuration options for tags
    /// </summary>
    public class TagsOptions
    {
        public int? MaxTagLength { get; set; }

        public string TagColor { get; set; }

        public string TextColor { get; set; }

        public string DarkTagColor { get; set; }

        public string DarkTextColor { get; set; }

        public TagsListStyle TagsListStyle { get; set; } = TagsListStyle.LinkButton; // default
    }
}