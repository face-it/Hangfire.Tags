using System;
using System.Reflection;
using Hangfire.Storage.SQLite;
using Hangfire.Storage;
using Hangfire.Tags.Storage;

namespace Hangfire.Tags.SQLite
{
    internal class SQLiteTagsTransaction : ITagsTransaction
    {
        private readonly SQLiteStorageOptions _options;
        private readonly IWriteOnlyTransaction _transaction;

        private static Type _type;
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
                _queueCommand = null;

                _type = transaction.GetType();
            }

            if (_queueCommand == null)
                _queueCommand = transaction.GetType().GetTypeInfo().GetMethod(nameof(QueueCommand),
                    BindingFlags.NonPublic | BindingFlags.Instance);

            if (_queueCommand == null)
                throw new ArgumentException("The function QueueCommand cannot be found.");
        }

        private void QueueCommand(Action<HangfireDbContext> action)
        {
            _queueCommand.Invoke(_transaction, new object[] { action });
        }

        public void ExpireSetValue(string key, string value, TimeSpan expireIn)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));

            var query = "update [Set] set ExpireAt = ? where [Key] = ? and [Value] = ?";

            //AcquireSetLock(key);
            QueueCommand(connection =>
                connection.Database.Execute(query, DateTime.UtcNow.Add(expireIn), key, value));
        }

        public void PersistSetValue(string key, string value)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));

            var query = "update [Set] set ExpireAt = null where [Key] = ? and [Value] = ?";

            //AcquireSetLock(key);
            QueueCommand(connection =>
                connection.Database.Execute(query, key, value));
        }
    }
}
