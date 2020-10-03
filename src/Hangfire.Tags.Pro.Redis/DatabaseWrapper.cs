using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Hangfire.Pro.Redis;

namespace Hangfire.Tags.Pro.Redis
{
    internal class DatabaseWrapper
    {
        private readonly object _objFactory;
        private readonly RedisStorageOptions _options;

        private static readonly PropertyInfo SortedSetEntryElementProperty;

        private static readonly MethodInfo SortedSetScanMethod;
        private static readonly MethodInfo SortedSetCombineAndStoreMethod;
        private static readonly MethodInfo SortedSetAddMethod;
        private static readonly MethodInfo SortedSetRemoveMethod;
        private static readonly MethodInfo SortedSetRangeByScoreMethod;
        private static readonly MethodInfo KeyDeleteMethod;
        private static readonly MethodInfo SortedSetLengthMethod;
        private static readonly MethodInfo HashGetAllMethod;
        private static readonly PropertyInfo HashEntryNameProperty;
        private static readonly PropertyInfo HashEntryValueProperty;

        private static readonly MethodInfo RedisKeyTypeConverter;
        private static readonly MethodInfo RedisValueTypeConverter;
        
        private static readonly object CommandFlagNone;
        private static readonly object SetOperationIntersect;
        private static readonly object AggregateSum;
        private static readonly object ExcludeNone;
        private static readonly object OrderDescending;

        private static readonly Type RedisKeyType;

        static DatabaseWrapper()
        {
            var type = Type.GetType("StackExchange.Redis.KeyspaceIsolation.DatabaseWrapper, Hangfire.Pro.Redis");
            if (type == null)
                throw new ArgumentException("The type DatabaseWrapper is not found in Hangfire.Pro.Redis");

            RedisKeyType = Type.GetType("StackExchange.Redis.RedisKey, Hangfire.Pro.Redis");
            var redisValueType = Type.GetType("StackExchange.Redis.RedisValue, Hangfire.Pro.Redis");
            var commandFlagType = Type.GetType("StackExchange.Redis.CommandFlags, Hangfire.Pro.Redis");
            var setOperationType = Type.GetType("StackExchange.Redis.SetOperation, Hangfire.Pro.Redis");
            var aggregateType = Type.GetType("StackExchange.Redis.Aggregate, Hangfire.Pro.Redis");
            var excludeType = Type.GetType("StackExchange.Redis.Exclude, Hangfire.Pro.Redis");
            var orderType = Type.GetType("StackExchange.Redis.Order, Hangfire.Pro.Redis");

            if (RedisKeyType == null || redisValueType == null || commandFlagType == null || setOperationType == null ||
                aggregateType == null || excludeType == null || orderType == null)
                throw new ArgumentException(
                    "One or more of the types RedisKey, RedisValue, CommandFlags, SetOperation, Aggregate, Exclude or Order are not found in Hangfire.Pro.Redis");

            RedisKeyTypeConverter = RedisKeyType.GetMethod("op_Implicit", new[] {typeof(string)});
            RedisValueTypeConverter = redisValueType.GetMethod("op_Implicit", new[] {typeof(string)});

            CommandFlagNone = Enum.Parse(commandFlagType, "None");
            SetOperationIntersect = Enum.Parse(setOperationType, "Intersect");
            AggregateSum = Enum.Parse(aggregateType, "Sum");
            ExcludeNone = Enum.Parse(excludeType, "None");
            OrderDescending = Enum.Parse(orderType, "Descending");

            var redisKeyArrayType = RedisKeyType.MakeArrayType();

            SortedSetScanMethod = type.GetMethod(nameof(SortedSetScan),
                new[] {RedisKeyType, redisValueType, typeof(int), typeof(long), typeof(int), commandFlagType});
            SortedSetCombineAndStoreMethod = type.GetMethod(nameof(SortedSetCombineAndStore),
                new[]
                {
                    setOperationType, RedisKeyType, redisKeyArrayType, typeof(double[]), aggregateType, commandFlagType
                });
            SortedSetLengthMethod = type.GetMethod(nameof(SortedSetLength),
                new[] {RedisKeyType, typeof(double), typeof(double), excludeType, commandFlagType});
            SortedSetAddMethod = type.GetMethod(nameof(SortedSetAdd), new[] {RedisKeyType, redisValueType, typeof(double), commandFlagType});
            SortedSetRemoveMethod = type.GetMethod(nameof(SortedSetRemove), new[] {RedisKeyType, redisValueType, commandFlagType});
            SortedSetRangeByScoreMethod = type.GetMethod(nameof(SortedSetRangeByScore),
                new[]
                {
                    RedisKeyType, typeof(double), typeof(double), excludeType, orderType, typeof(long), typeof(long),
                    commandFlagType
                });

            HashGetAllMethod = type.GetMethod(nameof(HashGetAll), new[] {RedisKeyType, commandFlagType});
            KeyDeleteMethod = type.GetMethod(nameof(KeyDelete), new[] {RedisKeyType, commandFlagType});

            type = Type.GetType("StackExchange.Redis.SortedSetEntry, Hangfire.Pro.Redis");
            if (type == null)
                throw new ArgumentException("The type SortedSetEntry is not found in Hangfire.Pro.Redis");

            SortedSetEntryElementProperty = type.GetProperty("Element");

            type = Type.GetType("StackExchange.Redis.HashEntry, Hangfire.Pro.Redis");
            if (type == null)
                throw new ArgumentException("The type HashEntry is not found in Hangfire.Pro.Redis");

            HashEntryNameProperty = type.GetProperty("Name");
            HashEntryValueProperty = type.GetProperty("Value");
        }

        public DatabaseWrapper(object objFactory, RedisStorageOptions options)
        {
            _objFactory = objFactory;
            _options = options;
        }

        private static object ToRedisKey(string key)
        {
            return RedisKeyTypeConverter.Invoke(null, new object[] {key});
        }

        private static Array ToRedisKeys(IEnumerable<string> key)
        {
            var array = key.Select(ToRedisKey).ToArray();
            var destinationArray = Array.CreateInstance(RedisKeyType, array.Length);
            Array.Copy(array, destinationArray, array.Length);
            return destinationArray;
        }

        private static object ToRedisValue(string value)
        {
            return RedisValueTypeConverter.Invoke(null, new object[] {value});
        }

        // private static object[] ToRedisValues(string[] values)
        // {
        //     return values.Select(v => RedisKeyTypeConverter.ConvertFromString(v)).ToArray();
        // }

        public IEnumerable<string> SortedSetScan(string key, string pattern = default)
        {
            var enumerable = (IEnumerable) SortedSetScanMethod.Invoke(_objFactory,
                new[] {ToRedisKey(key), ToRedisValue(pattern), _options.MaxSucceededListLength, 0L, 0, CommandFlagNone});
            foreach (var obj in enumerable)
            {
                yield return SortedSetEntryElementProperty.GetValue(obj).ToString();
            }
        }

        public long SortedSetCombineAndStore(string outKey, IEnumerable<string> redisKeys)
        {
            return (long) SortedSetCombineAndStoreMethod.Invoke(_objFactory,
                new[]
                {
                    SetOperationIntersect, ToRedisKey(outKey), ToRedisKeys(redisKeys), null, AggregateSum,
                    CommandFlagNone
                });
        }

        public long SortedSetLength(string key)
        {
            return (long) SortedSetLengthMethod.Invoke(_objFactory,
                new[]
                {
                    ToRedisKey(key), double.NegativeInfinity, double.PositiveInfinity, ExcludeNone, CommandFlagNone
                });
        }

        public void KeyDelete(string key)
        {
            KeyDeleteMethod.Invoke(_objFactory, new[] {ToRedisKey(key), CommandFlagNone});
        }

        public void SortedSetAdd(string key, string value, long score)
        {
            SortedSetAddMethod.Invoke(_objFactory, new[] {ToRedisKey(key), ToRedisValue(value), score, CommandFlagNone});
        }

        public void SortedSetRemove(string key, string value)
        {
            SortedSetRemoveMethod.Invoke(_objFactory, new[] {ToRedisKey(key), ToRedisValue(value), CommandFlagNone});
        }

        public IEnumerable<string> SortedSetRangeByScore(string key, int from, int count)
        {
            var enumerable = (IEnumerable) SortedSetRangeByScoreMethod.Invoke(_objFactory,
                new[]
                {
                    ToRedisKey(key), double.NegativeInfinity, double.PositiveInfinity, ExcludeNone, OrderDescending,
                    from, count, CommandFlagNone
                });
            foreach (var obj in enumerable)
            {
                yield return obj.ToString();
            }
        }

        public IEnumerable<KeyValuePair<string, string>> HashGetAll(string redisKey)
        {
            var enumerable =
                (IEnumerable) HashGetAllMethod.Invoke(_objFactory, new[] {ToRedisKey(redisKey), CommandFlagNone});
            foreach (var obj in enumerable)
            {
                yield return new KeyValuePair<string, string>(
                    HashEntryNameProperty.GetValue(obj).ToString(), HashEntryValueProperty.GetValue(obj).ToString()
                );
            }
        }
    }
}
