using System;

namespace Hangfire.Tags
{
    [Flags]
    public enum Clean
    {
        /// <summary>
        /// Cleans the tags by making them lowercase and only keep letters, digits and dashes. Spaces will be replaced by dashes.
        /// </summary>
        Default = Lowercase | Punctuation,
        /// <summary>
        /// Only makes the tags lowercase, but does not change punctuation
        /// </summary>
        Lowercase = 1,
        /// <summary>
        /// Removes all punctuation, but doesn't make the tags lowercase. Spaces will be replaced by dashes.
        /// </summary>
        Punctuation = 2,
        /// <summary>
        /// Doesn't change the tags in any way, but passes them as is.
        /// </summary>
        None = 0
    }
}