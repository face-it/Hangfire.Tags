using System;
using System.Data.Common;
using System.Data;
using Hangfire.Annotations;

namespace Hangfire.Tags.SqlServer
{
    internal static class DbCommandExtensions
    {
        public static DbCommand Create(
            [NotNull] this DbConnection connection,
            [NotNull] string text,
            CommandType type = CommandType.Text,
            int? timeout = null)
        {
            if (connection == null) throw new ArgumentNullException(nameof(connection));
            if (text == null) throw new ArgumentNullException(nameof(text));

            var command = connection.CreateCommand();
            command.CommandType = type;
            command.CommandText = text;

            if (timeout.HasValue)
            {
                command.CommandTimeout = timeout.Value;
            }

            return command;
        }

        public static DbCommand AddParameter(
            [NotNull] this DbCommand command,
            [NotNull] string parameterName,
            [CanBeNull] object value,
            DbType dbType,
            [CanBeNull] int? size = null)
        {
            if (command == null) throw new ArgumentNullException(nameof(command));
            if (parameterName == null) throw new ArgumentNullException(nameof(parameterName));

            var parameter = command.CreateParameter();
            parameter.ParameterName = parameterName;
            parameter.DbType = dbType;
            parameter.Value = value ?? DBNull.Value;

            if (size.HasValue) parameter.Size = size.Value;

            command.Parameters.Add(parameter);
            return command;
        }
    }
}
