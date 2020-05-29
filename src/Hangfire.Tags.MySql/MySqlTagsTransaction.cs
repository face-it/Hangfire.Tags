using System;
using System.Reflection;
using Hangfire.MySql;
using Hangfire.Storage;
using Hangfire.Tags.Storage;
using Dapper;
using MySql.Data.MySqlClient;

namespace Hangfire.Tags.MySql
{
    public class MySqlTagsTransaction : ITagsTransaction
    {
        private readonly MySqlStorageOptions _options;

        private readonly IWriteOnlyTransaction _transaction;

        private static MethodInfo _queueCommand;

        public MySqlTagsTransaction(MySqlStorageOptions options, IWriteOnlyTransaction transaction)
        {
            if (transaction.GetType().Name != "MySqlWriteOnlyTransaction")
                throw new ArgumentException("The transaction is not a MySql transaction", nameof(transaction));

            _options = options;
            _transaction = transaction;

            if (_queueCommand == null)
                _queueCommand = transaction.GetType().GetTypeInfo().GetMethod(nameof(QueueCommand),
                    BindingFlags.NonPublic | BindingFlags.Instance);

            if (_queueCommand == null)
                throw new ArgumentException("The functions QueueCommand cannot be found.");
        }

        private void QueueCommand(Action<MySqlConnection> action)
        {
            _queueCommand.Invoke(_transaction, new object[] { action });
        }

        public void ExpireSetValue(string key, string value, TimeSpan expireIn)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));

            var query = $@"
update `{_options.TablesPrefix}Set` set ExpireAt = @expireAt where `Key` = @key and Value = @value";

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

            string query = $@"
update `{_options.TablesPrefix}Set` set ExpireAt = null where `Key` = @key and Value = @value";

            QueueCommand((connection) => connection.Execute(query,
                 new { key, value }));
        }
    }
}
