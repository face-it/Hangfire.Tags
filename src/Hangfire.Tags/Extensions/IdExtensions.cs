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

        public static string Clean(this string tag, int? maxTagLength = null)
        {
            var retval = new string(tag.ToLower().Where(c => char.IsLetterOrDigit(c) || c == ' ' || c == '-').ToArray())
                .Replace(' ', '-').Replace("--", "-");

            if (maxTagLength.HasValue && retval.Length > maxTagLength.Value)
                retval = retval.Substring(0,
                    maxTagLength.Value -
                    5); // Make it shorter, since we'll also use the prefix tags:. Max. length is 75 characters

            return retval;
        }
    }
}
