using System;
using System.Reflection;
using Hangfire.Mongo.Database;
using Hangfire.Storage;

namespace Hangfire.Tags.Mongo
{
    internal class MongoTagsMonitoringApi
    {
        private static Type _type;
        private static FieldInfo _dbContextField;

        public MongoTagsMonitoringApi(IMonitoringApi monitoringApi)
        {
            if (monitoringApi.GetType().Name != "MongoMonitoringApi")
                throw new ArgumentException("The monitor API is not implemented using Mongo", nameof(monitoringApi));

            // Dirty, but lazy...we would like to reuse the already existing connection, so we're resorting to reflection for now to retrieve it

            // Other transaction type, clear cached methods
            if (_type != monitoringApi.GetType())
            {
                _dbContextField = null;

                _type = monitoringApi.GetType();
            }

            if (_dbContextField == null)
                _dbContextField = monitoringApi.GetType().GetTypeInfo().GetField("_dbContext",
                    BindingFlags.NonPublic | BindingFlags.Instance);

            if (_dbContextField == null)
                throw new ArgumentException("The field _dbContext cannot be found.");

            DbContext = (HangfireDbContext)_dbContextField.GetValue(monitoringApi);
        }

        public HangfireDbContext DbContext { get; private set; }
    }
}
