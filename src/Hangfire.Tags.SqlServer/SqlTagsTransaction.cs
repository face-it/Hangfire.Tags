using System;
using System.Data.SqlClient;
using System.Reflection;
using Hangfire.SqlServer;
using Hangfire.Storage;
using Hangfire.Tags.Storage;

namespace Hangfire.Tags.SqlServer
{
    internal class SqlTagsTransaction : ITagsTransaction
    {
        private readonly SqlServerStorageOptions _options;
        private readonly IWriteOnlyTransaction _transaction;

        private static Type _type;
        private static MethodInfo _acquireSetLock;
        private static MethodInfo _queueCommand;

        private static MethodInfo _addCommand;

        public SqlTagsTransaction(SqlServerStorageOptions options, IWriteOnlyTransaction transaction)
        {
            if (transaction.GetType().Name != "SqlServerWriteOnlyTransaction")
                throw new ArgumentException("The transaction is not an SQL transaction", nameof(transaction));

            _options = options;
            _transaction = transaction;

            // Dirty, but lazy...we would like to execute these commands in the same transaction, so we're resorting to reflection for now

            // Other transaction type, clear cached methods
            if (_type != transaction.GetType())
            {
                _acquireSetLock = null;
                _queueCommand = null;
                _addCommand = null;

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

            if (_queueCommand == null && _addCommand == null)
            {
                _addCommand = transaction.GetType().GetTypeInfo().GetMethod(nameof(AddCommand),
                    BindingFlags.NonPublic | BindingFlags.Instance);
                _addCommand = _addCommand?.MakeGenericMethod(typeof(string));
            }

            if (_queueCommand == null && _addCommand == null)
                throw new ArgumentException("The functions QueueCommand and AddCommand cannot be found.");
        }

        private void AcquireSetLock()
        {
            _acquireSetLock.Invoke(_transaction, null);
        }

        private void QueueCommand(string commandText, params SqlParameter[] parameters)
        {
            _queueCommand.Invoke(_transaction, new object[] { commandText, parameters });
        }

        private void AddCommand(string commandText, string key, params SqlParameter[] parameters)
        {
            object _setCommands = _transaction.GetType().GetTypeInfo().GetField("_setCommands", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(_transaction);
            _addCommand.Invoke(_transaction, new object[] { _setCommands, key, commandText, parameters });
        }

        public void ExpireSetValue(string key, string value, TimeSpan expireIn)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));

            var query = $@"
update [{_options.SchemaName}].[Set] set ExpireAt = @expireAt where [Key] = @key and [Value] = @value";

            AcquireSetLock();
            if (_queueCommand == null)
            {
                AddCommand(query,
                    key,
                    new SqlParameter("@key", key),
                    new SqlParameter("@value", value),
                    new SqlParameter("@expireAt", DateTime.UtcNow.Add(expireIn)));
            }
            else
            {
                QueueCommand(query,
                    new SqlParameter("@key", key),
                    new SqlParameter("@value", value),
                    new SqlParameter("@expireAt", DateTime.UtcNow.Add(expireIn)));
            }

        }

        public void PersistSetValue(string key, string value)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));

            string query = $@"
update [{_options.SchemaName}].[Set] set ExpireAt = null where [Key] = @key and [Value] = @value";

            AcquireSetLock();
            if (_queueCommand == null)
            {
                AddCommand(query,
                key,
                new SqlParameter("@key", key),
                new SqlParameter("@value", value));
            }
            else
            {
                QueueCommand(query,
                    new SqlParameter("@key", key),
                    new SqlParameter("@value", value));
            }
        }
    }
}
