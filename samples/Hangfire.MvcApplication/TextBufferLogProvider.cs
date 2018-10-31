using System;
using Hangfire.Common;
using Hangfire.Logging;
using Newtonsoft.Json;

namespace Hangfire.MvcApplication
{
    public class TextBufferLogProvider : ILogProvider
    {
        public ILog GetLogger(string name)
        {
            return new TextBufferLog();
        }

        public class TextBufferLog : ILog
        {
            public bool Log(LogLevel logLevel, Func<string> messageFunc, Exception exception)
            {
                var settings = new JsonSerializerSettings
                {
                    TypeNameHandling = TypeNameHandling.Objects
                };

                JobHelper.SetSerializerSettings(settings);

                if (messageFunc == null)
                {
                    return logLevel >= LogLevel.Info;
                }

                TextBuffer.WriteLine(messageFunc());
                return true;
            }
        }
    }
}