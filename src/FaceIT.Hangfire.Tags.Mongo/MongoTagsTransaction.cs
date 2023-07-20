using System;
using System.Collections.Generic;
using System.Reflection;
using Hangfire.Mongo;
using Hangfire.Mongo.Dto;
using Hangfire.Storage;
using Hangfire.Tags.Storage;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Hangfire.Tags.Mongo
{
    internal class MongoTagsTransaction : ITagsTransaction
    {
        private readonly MongoWriteOnlyTransaction _transaction;

        private readonly List<WriteModel<BsonDocument>> _writeModels;

        private static Type _type;
        private static FieldInfo _writeModelsFieldInfo;
        
        public MongoTagsTransaction(MongoStorageOptions options, IWriteOnlyTransaction transaction)
        {
            _transaction = transaction as MongoWriteOnlyTransaction;
            if (_transaction == null)
                throw new ArgumentException("The transaction is not an Mongo transaction", nameof(transaction));

            // Dirty, but lazy...we would like to execute these commands in the same transaction, so we're resorting to reflection for now
            if (_type != transaction.GetType())
            {
                _writeModelsFieldInfo = null;
                _type = transaction.GetType();
            }

            if (_writeModelsFieldInfo == null)
            {
                _writeModelsFieldInfo = transaction.GetType().GetTypeInfo()
                    .GetField("_writeModels", BindingFlags.NonPublic | BindingFlags.Instance);
            }

            if (_writeModelsFieldInfo == null)
                throw new ArgumentException("The field _writeModels cannot be found.");

            _writeModels = (List<WriteModel<BsonDocument>>)_writeModelsFieldInfo.GetValue(transaction);
        }

        public void ExpireSetValue(string key, string value, TimeSpan expireIn)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            var filter = _transaction.CreateSetFilter(key, value);

            var update = new BsonDocument("$set",
                new BsonDocument(nameof(SetDto.ExpireAt), DateTime.UtcNow.Add(expireIn)));

            var writeModel = new UpdateManyModel<BsonDocument>(filter, update);
            _writeModels.Add(writeModel);
        }

        public void PersistSetValue(string key, string value)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            var filter = _transaction.CreateSetFilter(key, value);

            var update = new BsonDocument("$set",
                new BsonDocument(nameof(SetDto.ExpireAt), BsonNull.Value));

            var writeModel = new UpdateManyModel<BsonDocument>(filter, update);
            _writeModels.Add(writeModel);        
        }
    }
}
