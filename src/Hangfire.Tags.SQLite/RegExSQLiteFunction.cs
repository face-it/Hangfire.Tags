using System.Text.RegularExpressions;

namespace Hangfire.Tags.SQLite
{
    internal class RegExSQLiteFunction
#if !NETSTANDARD
        : System.Data.SQLite.SQLiteFunction
#endif
    {
        public bool IsMatch(string pattern, string input)
        {
            return Regex.IsMatch(input, pattern);
        }

#if !NETSTANDARD
        public override object Invoke(object[] args)
        {
            return IsMatch(args[0].ToString(), args[1].ToString());
        }
#endif
    }
}
