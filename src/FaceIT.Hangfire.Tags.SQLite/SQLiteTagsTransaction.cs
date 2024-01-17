using System;
using System.Data.Common;
using System.Reflection;
using Dapper;
using Hangfire.SQLite;
using Hangfire.Storage;
using Hangfire.Tags.Storage;

namespace Hangfire.Tags.SQLite
{
    internal class SQLiteTagsTransaction : ITagsTransaction
    {
        private readonly SQLiteStorageOptions _options;
        private readonly IWriteOnlyTransaction _transaction;

        private static Type _type;
        private static MethodInfo _acquireSetLock;
        private static MethodInfo _queueCommand;

        public SQLiteTagsTransaction(SQLiteStorageOptions options, IWriteOnlyTransaction transaction)
        {
            if (transaction.GetType().Name != "SQLiteWriteOnlyTransaction")
                throw new ArgumentException("The transaction is not an SQLite transaction", nameof(transaction));

            _options = options;
            _transaction = transaction;

            // Dirty, but lazy...we would like to execute these commands in the same transaction, so we're resorting to reflection for now

            // Other transaction type, clear cached methods
            if (_type != transaction.GetType())
            {
                _acquireSetLock = null;
                _queueCommand = null;

                _type = transaction.GetType();
            }

            if (_acquireSetLock == null)
                _acquireSetLock = transaction.GetType().GetTypeInfo().GetMethod(nameof(AcquireSetLock),
                    BindingFlags.NonPublic | BindingFlags.Instance);

            if (_acquireSetLock == null)
                throw new ArgumentException("The function AcquireSetLock cannot be found.");

            if (_queueCommand == null)
                _queueCommand = transaction.GetType().GetTypeInfo().GetMethod(nameof(QueueCommand),
                    BindingFlags.NonPublic | BindingFlags.Instance);

            if (_queueCommand == null)
                throw new ArgumentException("The function QueueCommand cannot be found.");
        }

        private void AcquireSetLock(string key)
        {
            object[] parameters = _acquireSetLock.GetParameters().Length > 0 ? new object[] { key } : null;
            _acquireSetLock.Invoke(_transaction, parameters);
        }

        private void QueueCommand(Action<DbConnection, DbTransaction> action)
        {
            _queueCommand.Invoke(_transaction, new object[] { action });
        }

        public void ExpireSetValue(string key, string value, TimeSpan expireIn)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));

            var query = $@"
update [{_options.SchemaName}.Set] set ExpireAt = @expireAt where [Key] = @key and [Value] = @value";

            AcquireSetLock(key);
            QueueCommand((connection, transaction) => connection.Execute(
                query, new {key, value, expireAt = DateTime.UtcNow.Add(expireIn)}, transaction));
        }

        public void PersistSetValue(string key, string value)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));

            string query = $@"
update [{_options.SchemaName}.Set] set ExpireAt = null where [Key] = @key and [Value] = @value";

            AcquireSetLock(key);
            QueueCommand((connection, transaction) => connection.Execute(
                query, new { key, value }, transaction));
        }
    }
}
