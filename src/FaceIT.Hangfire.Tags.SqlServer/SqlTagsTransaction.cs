using System;
using System.Data;
using System.Linq;
using System.Reflection;
using Hangfire.SqlServer;
using Hangfire.Storage;
using Hangfire.Tags.Storage;

namespace Hangfire.Tags.SqlServer
{
    public class SqlCommandBatchParameter
    {
        public string ParameterName { get; }

        public DbType DbType { get; }

        public object Value { get; }

        public SqlCommandBatchParameter(string parameterName, DbType dbType, object value) => (ParameterName, DbType, Value) = (parameterName, dbType, value);
    }

    internal class SqlTagsTransaction : ITagsTransaction
    {
        private readonly SqlServerStorageOptions _options;
        private readonly IWriteOnlyTransaction _transaction;

        private static Type _type;
        private static MethodInfo _acquireSetLock;
        private static Type _sqlCommandBatchParameter;
        private static ConstructorInfo _sqlCommandBatchParameterCtor;
        private static PropertyInfo _sqlCommandBatchParameterValue;

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
                _addCommand = null;

                _type = transaction.GetType();
            }

            if (_acquireSetLock == null)
                _acquireSetLock = transaction.GetType().GetTypeInfo().GetMethod(nameof(AcquireSetLock),
                    BindingFlags.NonPublic | BindingFlags.Instance);

            if (_acquireSetLock == null)
                throw new ArgumentException("The function AcquireSetLock cannot be found.");

            if (_addCommand == null)
            {
                _addCommand = transaction.GetType().GetTypeInfo().GetMethod(nameof(AddCommand),
                    BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                _addCommand = _addCommand?.MakeGenericMethod(typeof(string));
            }

            if (_addCommand == null)
                throw new ArgumentException("The functions QueueCommand and AddCommand cannot be found.");

            if (_sqlCommandBatchParameter == null)
            {
                _sqlCommandBatchParameter = Type.GetType("Hangfire.SqlServer.SqlCommandBatchParameter, Hangfire.SqlServer");
                _sqlCommandBatchParameterCtor =
                    _sqlCommandBatchParameter.GetConstructor(new Type[] {typeof(string), typeof(DbType), typeof(int?)});
                _sqlCommandBatchParameterValue = _sqlCommandBatchParameter.GetProperty("Value");
            }
        }

        private void AcquireSetLock(string key)
        {
            object[] parameters = _acquireSetLock.GetParameters().Length > 0 ? new object[] { key } : null;
            _acquireSetLock.Invoke(_transaction, parameters);
        }

        private void AddCommand(string commandText, string key, params SqlCommandBatchParameter[] parameters)
        {
            var batchParams = Array.CreateInstance(_sqlCommandBatchParameter, parameters.Length);
            for (var i = 0; i < parameters.Length; i++)
            {
                var p = parameters[i];
                var param = _sqlCommandBatchParameterCtor.Invoke(new object[] { p.ParameterName, p.DbType, null });
                _sqlCommandBatchParameterValue.SetValue(param, p.Value);
                batchParams.SetValue(param, i);
            }

            var setCommands = _transaction.GetType().GetTypeInfo().GetField("_setCommands", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(_transaction);
            _addCommand.Invoke(_transaction, new object[] { setCommands, key, commandText, batchParams });
        }

        public void ExpireSetValue(string key, string value, TimeSpan expireIn)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));

            var query = $@"
update [{_options.SchemaName}].[Set] set ExpireAt = @expireAt where [Key] = @key and [Value] = @value";

            AcquireSetLock(key);
            AddCommand(query,
                key,
                new SqlCommandBatchParameter("@key", DbType.String, key),
                new SqlCommandBatchParameter("@value", DbType.String, value),
                new SqlCommandBatchParameter("@expireAt", DbType.DateTime, DateTime.UtcNow.Add(expireIn)));
        }

        public void PersistSetValue(string key, string value)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));

            string query = $@"
update [{_options.SchemaName}].[Set] set ExpireAt = null where [Key] = @key and [Value] = @value";

            AcquireSetLock(key);
            AddCommand(query,
                key,
                new SqlCommandBatchParameter("@key", DbType.String, key),
                new SqlCommandBatchParameter("@value", DbType.String, value));
        }
    }
}
