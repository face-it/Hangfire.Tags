using System;

namespace Hangfire.Tags.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public class TagAttribute : Attribute
    {
        public TagAttribute(params string[] tag)
        {
            Tag = tag;
        }

        public string[] Tag { get; }
    }
}
