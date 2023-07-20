using System.Reflection;
using Hangfire.Dashboard;

namespace Hangfire.Tags
{
    internal static class HtmlHelperExtensions
    {
        /// <summary>
        /// The _page variable is a private variable, which is not directly retrievable
        /// </summary>
        private static readonly FieldInfo PageField = typeof(HtmlHelper).GetTypeInfo().GetDeclaredField("_page");

        /// <summary>
        /// Returns a <see cref="RazorPage" /> associated with <see cref="HtmlHelper"/>.
        /// </summary>
        /// <param name="helper">The Html Helper</param>
        /// <returns>The current <see cref="RazorPage"/> </returns>
        public static RazorPage GetPage(this HtmlHelper helper)
        {
            return (RazorPage) PageField.GetValue(helper);
        }
    }
}
