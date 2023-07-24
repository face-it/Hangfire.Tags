namespace Hangfire.Tags
{
    public enum TagsListStyle
    {
        /// <summary>
        /// Shows a list of clickable tags in the dashboard
        /// </summary>
        LinkButton,

        /// <summary>
        /// Shows a dropdown list of tags with a search field, so the user can search for tags or parts of a tag.
        /// </summary>
        Dropdown
    }
}