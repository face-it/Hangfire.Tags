using System;
using System.Data;
using System.Reflection;
using Hangfire.Storage;

namespace Hangfire.Tags.PostgreSql
{
    public class PostgreSqlTagsMonitoringApi
    {
        private readonly JobStorage _postgreSqlStorage;
        private readonly IMonitoringApi _monitoringApi;

        private static bool _useStorage;
        private static Type _type;
        private static MethodInfo _useConnection;

        public PostgreSqlTagsMonitoringApi(JobStorage postgreSqlStorage)
        {
            var monitoringApi = postgreSqlStorage.GetMonitoringApi();

            if (monitoringApi.GetType().Name != "PostgreSqlMonitoringApi")
            {
                throw new ArgumentException("The monitor API is not implemented using PostgreSql", nameof(monitoringApi));
            }

            _monitoringApi = monitoringApi;
            _postgreSqlStorage = postgreSqlStorage;

            // Other transaction type, clear cached methods
            if (_type != monitoringApi.GetType())
            {
                _useConnection = null;

                _type = monitoringApi.GetType();
            }

            if (_useConnection == null)
                _useConnection = monitoringApi.GetType().GetTypeInfo().GetMethod(nameof(UseConnection),
                    BindingFlags.NonPublic | BindingFlags.Instance);

            if (_useConnection == null)
            {
                _useConnection = postgreSqlStorage.GetType().GetTypeInfo().GetMethod(nameof(UseConnection),
                    BindingFlags.NonPublic | BindingFlags.Instance);
                _useStorage = true;
            }

            if (_useConnection == null)
                throw new ArgumentException("The function UseConnection cannot be found.");
        }

        public T UseConnection<T>(Func<IDbConnection, T> action)
        {
            var method = _useConnection.MakeGenericMethod(typeof(T));
            return (T) method.Invoke(_useStorage ? (object) _postgreSqlStorage : _monitoringApi, new object[] {action});
        }
    }
}
