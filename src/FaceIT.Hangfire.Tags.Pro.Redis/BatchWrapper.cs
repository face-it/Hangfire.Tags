using System;
using System.Reflection;
using System.Threading.Tasks;

namespace Hangfire.Tags.Pro.Redis
{
    internal class BatchWrapper
    {
        private readonly object _objFactory;

        private static readonly MethodInfo SetRemoveAsyncMethod;
        private static readonly MethodInfo SortedSetRemoveAsyncMethod;

        private static readonly MethodInfo ExecuteMethod;

        private static readonly MethodInfo RedisKeyTypeConverter;
        private static readonly MethodInfo RedisValueTypeConverter;

        private static readonly object CommandFlagNone;

        private static readonly Type RedisKeyType;

        static BatchWrapper()
        {
            var type = Type.GetType("StackExchange.Redis.KeyspaceIsolation.BatchWrapper, Hangfire.Pro.Redis");
            if (type == null)
                throw new ArgumentException("The type BatchWrapper is not found in Hangfire.Pro.Redis");

            RedisKeyType = Type.GetType("StackExchange.Redis.RedisKey, Hangfire.Pro.Redis");
            var redisValueType = Type.GetType("StackExchange.Redis.RedisValue, Hangfire.Pro.Redis");
            var commandFlagType = Type.GetType("StackExchange.Redis.CommandFlags, Hangfire.Pro.Redis");

            if (RedisKeyType == null || redisValueType == null || commandFlagType == null)
                throw new ArgumentException(
                    "One or more of the types RedisKey, RedisValue or CommandFlags are not found in Hangfire.Pro.Redis");

            RedisKeyTypeConverter = RedisKeyType.GetMethod("op_Implicit", new[] { typeof(string) });
            RedisValueTypeConverter = redisValueType.GetMethod("op_Implicit", new[] { typeof(string) });

            CommandFlagNone = Enum.Parse(commandFlagType, "None");

            SetRemoveAsyncMethod = type.GetMethod(nameof(SetRemoveAsync), new[] { RedisKeyType, redisValueType, commandFlagType });
            SortedSetRemoveAsyncMethod = type.GetMethod(nameof(SortedSetRemoveAsync), new[] { RedisKeyType, redisValueType, commandFlagType });
            ExecuteMethod = type.GetMethod(nameof(Execute));

        }

        public BatchWrapper(object batch)
        {
            _objFactory = batch;
        }

        public void Execute()
        {
            ExecuteMethod.Invoke(_objFactory, null);
        }

        public Task<bool> SortedSetRemoveAsync(string key, string value)
        {
            return (Task<bool>) SortedSetRemoveAsyncMethod.Invoke(_objFactory, new[] { ToRedisKey(key), ToRedisValue(value), CommandFlagNone });
        }

        public Task SetRemoveAsync(string key, string value)
        {
            return (Task) SetRemoveAsyncMethod.Invoke(_objFactory, new[] { ToRedisKey(key), ToRedisValue(value), CommandFlagNone });
        }

        private static object ToRedisKey(string key)
        {
            return RedisKeyTypeConverter.Invoke(null, new object[] { key });
        }

        private static object ToRedisValue(string value)
        {
            return RedisValueTypeConverter.Invoke(null, new object[] { value });
        }
    }
}