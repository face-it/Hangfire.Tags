using System.Linq;

namespace Hangfire.Tags
{
    internal static class IdExtensions
    {
        internal const string SetKey = "tags";

        public static string GetSetKey(this string jobId)
        {
            return $"{SetKey}:{jobId}";
        }

        public static string Clean(this string tag)
        {
            return new string(tag.ToLower().Where(c => char.IsLetterOrDigit(c) || c == ' ' || c == '-').ToArray())
                .Replace(' ', '-');
        }
    }
}
