using System;
using System.Text;

namespace Hangfire.MvcApplication
{
    public static class TextBuffer
    {
        private static readonly StringBuilder Buffer = new StringBuilder();

        public static void WriteLine(string value)
        {
            lock (Buffer)
            {
                Buffer.AppendLine(String.Format("{0} {1}", DateTime.Now, value));
            }
        }

        public new static string ToString()
        {
            lock (Buffer)
            {
                return Buffer.ToString();
            }
        }
    }
}