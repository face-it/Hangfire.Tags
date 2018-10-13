using System.Linq;

namespace Hangfire.Tags
{
    internal static class IdExtensions
    {
        public static string GetSetKey(this string jobId)
        {
            return $"tags:{jobId}";
        }

        public static string Clean(this string tag)
        {
            return new string(tag.ToLower().Where(c => char.IsLetterOrDigit(c) || c == ' ' || c == '-').ToArray())
                .Replace(' ', '-');
        }
    }
}
