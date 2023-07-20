using System;
using System.Data;
using System.Reflection;
using Hangfire.PostgreSql;
using Hangfire.Storage;
using Hangfire.Tags.Storage;
using Dapper;
using Npgsql;

namespace Hangfire.Tags.PostgreSql
{
    public class PostgreSqlTagsTransaction : ITagsTransaction
    {
        private readonly PostgreSqlStorageOptions _options;

        private readonly IWriteOnlyTransaction _transaction;

        private static MethodInfo _queueCommand;

        public PostgreSqlTagsTransaction(PostgreSqlStorageOptions options, IWriteOnlyTransaction transaction)
        {
            if (transaction.GetType().Name != "PostgreSqlWriteOnlyTransaction")
                throw new ArgumentException("The transaction is not a PostgreSql transaction", nameof(transaction));

            _options = options;
            _transaction = transaction;

            if (_queueCommand == null)
                _queueCommand = transaction.GetType().GetTypeInfo().GetMethod(nameof(QueueCommand),
                    BindingFlags.NonPublic | BindingFlags.Instance);

            if (_queueCommand == null)
                throw new ArgumentException("The functions QueueCommand cannot be found.");
        }

        private void QueueCommand(Action<IDbConnection> action)
        {
            _queueCommand.Invoke(_transaction, new object[] { action });
        }

        public void ExpireSetValue(string key, string value, TimeSpan expireIn)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));

            var query = $@"
update {_options.SchemaName}.Set set ExpireAt = @expireAt where Key = @key and Value = @value";

            QueueCommand((connection) => connection.Execute(
                    query,
                    new
                    {
                        key,
                        value,
                        expireAt = DateTime.UtcNow.Add(expireIn)
                    }));
        }

        public void PersistSetValue(string key, string value)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));

            var query = $@"
update {_options.SchemaName}.Set set ExpireAt = null where Key = @key and Value = @value";

            QueueCommand((connection) => connection.Execute(query,
                 new { key, value }));
        }
    }
}
