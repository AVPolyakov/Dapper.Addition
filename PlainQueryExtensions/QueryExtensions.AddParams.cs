using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using PlainQuery;

namespace PlainQueryExtensions
{
    public static partial class QueryExtensions
    {
        private static void AddParams(DbCommand command, Query query)
        {
            foreach (var param in query.Parameters)
            {
                var type = param.GetType();
                Action<DbCommand, object> action;
                if (_addParamDictionary.TryGetValue(type, out var value))
                    action = value;
                else
                {
                    var methodInfo = GetMethodInfo<Func<Action<DbCommand, object>>>(() => GetAddParamAction<object>());
                    var method = methodInfo.GetGenericMethodDefinition().MakeGenericMethod(type);
                    action = (Action<DbCommand, object>) method.Invoke(null, null)!;
                    _addParamDictionary.TryAdd(type, action);
                }
                action(command, param);
            }
        }
        
        private static readonly ConcurrentDictionary<Type, Action<DbCommand, object>> _addParamDictionary = new();
        
        private static Action<DbCommand, object> GetAddParamAction<T>() 
            => (command, param) => AddParamsCache<T>.Action(command, (T) param);
        
        private static class AddParamsCache<T>
        {
            public static readonly Action<DbCommand, T> Action;

            static AddParamsCache()
            {
                var dynamicMethod = new DynamicMethod(System.Guid.NewGuid().ToString("N"), null,
                    new[] {typeof(DbCommand), typeof(T)}, true);

                var ilGenerator = dynamicMethod.GetILGenerator();

                foreach (var info in typeof(T).GetProperties())
                {
                    ilGenerator.Emit(OpCodes.Ldarg_0);
                    ilGenerator.Emit(OpCodes.Ldstr, "@" + info.Name);
                    ilGenerator.Emit(OpCodes.Ldarg_1);
                    ilGenerator.EmitCall(OpCodes.Callvirt, info.GetGetMethod()!, null);
                    ilGenerator.EmitCall(OpCodes.Call, GetAddParamMethod(info.PropertyType), null);
                    ilGenerator.Emit(OpCodes.Pop);
                }
                ilGenerator.Emit(OpCodes.Ret);

                Action = (Action<DbCommand, T>)
                    dynamicMethod.CreateDelegate(typeof(Action<DbCommand, T>));
            }
        }

        private static MethodInfo GetAddParamMethod(Type type)
        {
            if (AddParamsMethods.TryGetValue(type, out var methodInfo))
                return methodInfo;
            if (type.IsEnum)
            {
                //TODO: add code generation for byte, short
                var underlyingType = Enum.GetUnderlyingType(type);
                if (underlyingType == typeof(int))
                    return _intEnumParam.MakeGenericMethod(type);
                if (underlyingType == typeof(long))
                    return _longEnumParam.MakeGenericMethod(type);
            }
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                var argType = type.GetGenericArguments().Single();
                if (argType.IsEnum)
                {
                    var underlyingType = Enum.GetUnderlyingType(argType);
                    if (underlyingType == typeof(int))
                        return _nullableIntEnumParam.MakeGenericMethod(argType);
                    if (underlyingType == typeof(long))
                        return _nullableLongEnumParam.MakeGenericMethod(argType);
                }
            }
            throw new Exception($"Method of parameter adding not found for type '{type.FullName}'");
        }

        public static readonly Dictionary<Type, MethodInfo> AddParamsMethods = new[]
            {
                GetMethodInfo<Func<DbCommand, string, int, DbParameter>>((command, name, value) => command.AddParam(name, value)),
                GetMethodInfo<Func<DbCommand, string, int?, DbParameter>>((command, name, value) => command.AddParam(name, value)),
                GetMethodInfo<Func<DbCommand, string, bool, DbParameter>>((command, name, value) => command.AddParam(name, value)),
                GetMethodInfo<Func<DbCommand, string, bool?, DbParameter>>((command, name, value) => command.AddParam(name, value)),
                GetMethodInfo<Func<DbCommand, string, byte, DbParameter>>((command, name, value) => command.AddParam(name, value)),
                GetMethodInfo<Func<DbCommand, string, byte?, DbParameter>>((command, name, value) => command.AddParam(name, value)),
                GetMethodInfo<Func<DbCommand, string, long, DbParameter>>((command, name, value) => command.AddParam(name, value)),
                GetMethodInfo<Func<DbCommand, string, long?, DbParameter>>((command, name, value) => command.AddParam(name, value)),
                GetMethodInfo<Func<DbCommand, string, decimal, DbParameter>>((command, name, value) => command.AddParam(name, value)),
                GetMethodInfo<Func<DbCommand, string, decimal?, DbParameter>>((command, name, value) => command.AddParam(name, value)),
                GetMethodInfo<Func<DbCommand, string, Guid, DbParameter>>((command, name, value) => command.AddParam(name, value)),
                GetMethodInfo<Func<DbCommand, string, Guid?, DbParameter>>((command, name, value) => command.AddParam(name, value)),
                GetMethodInfo<Func<DbCommand, string, DateTime, DbParameter>>((command, name, value) => command.AddParam(name, value)),
                GetMethodInfo<Func<DbCommand, string, DateTime?, DbParameter>>((command, name, value) => command.AddParam(name, value)),
                GetMethodInfo<Func<DbCommand, string, string?, DbParameter>>((command, name, value) => command.AddParam(name, value)),
            }
            .ToDictionary(methodInfo => methodInfo.GetParameters()[2].ParameterType);

        public static MethodInfo GetMethodInfo<T>(Expression<T> expression)
            => ((MethodCallExpression) expression.Body).Method;

        public static DbParameter AddParameterWithValue(this DbCommand command, string parameterName, object parameterValue)
        {
            var parameter = command.CreateParameter();
            parameter.ParameterName = parameterName;
            parameter.Value = parameterValue;
            command.Parameters.Add(parameter);
            return parameter;
        }
        
        private static DbParameter AddDBNull(this DbCommand command, string parameterName, DbType dbType)
        {
            var parameter = command.CreateParameter();
            parameter.ParameterName = parameterName;
            parameter.DbType = dbType;
            parameter.Value = DBNull.Value;
            command.Parameters.Add(parameter);
            return parameter;
        }
        
        public static DbParameter AddParam(this DbCommand command, string parameterName, int? value) 
            => value.HasValue 
                ? AddParam(command, parameterName, value.Value) 
                : command.AddDBNull(parameterName, DbType.Int32);
        
        public static DbParameter AddParam(this DbCommand command, string parameterName, int value)
            => command.AddParameterWithValue(parameterName, value);

        public static DbParameter AddParam(this DbCommand command, string parameterName, bool? value)
            => value.HasValue
                ? command.AddParam(parameterName, value.Value)
                : command.AddDBNull(parameterName, DbType.Boolean);

        public static DbParameter AddParam(this DbCommand command, string parameterName, bool value)
            => command.AddParameterWithValue(parameterName, value);

        public static DbParameter AddParam(this DbCommand command, string parameterName, byte? value)
            => value.HasValue
                ? command.AddParam(parameterName, value.Value)
                : command.AddDBNull(parameterName, DbType.Byte);

        public static DbParameter AddParam(this DbCommand command, string parameterName, byte value)
            => command.AddParameterWithValue(parameterName, value);

        public static DbParameter AddParam(this DbCommand command, string parameterName, long? value)
            => value.HasValue
                ? command.AddParam(parameterName, value.Value)
                : command.AddDBNull(parameterName, DbType.Int64);

        public static DbParameter AddParam(this DbCommand command, string parameterName, long value)
            => command.AddParameterWithValue(parameterName, value);

        public static DbParameter AddParam(this DbCommand command, string parameterName, decimal? value)
            => value.HasValue
                ? command.AddParam(parameterName, value.Value)
                : command.AddDBNull(parameterName, DbType.Decimal);

        public static DbParameter AddParam(this DbCommand command, string parameterName, decimal value)
        {
            var parameter = command.AddParameterWithValue(parameterName, value);
            const byte defaultPrecision = 38;
            if (parameter.Precision < defaultPrecision) parameter.Precision = defaultPrecision;
            const byte defaultScale = 8;
            if (parameter.Scale < defaultScale) parameter.Scale = 8;
            return parameter;
        }

        public static DbParameter AddParam(this DbCommand command, string parameterName, Guid value)
            => command.AddParameterWithValue(parameterName, value);

        public static DbParameter AddParam(this DbCommand command, string parameterName, Guid? value)
            => value.HasValue
                ? command.AddParam(parameterName, value.Value)
                : command.AddDBNull(parameterName, DbType.Guid);

        public static DbParameter AddParam(this DbCommand command, string parameterName, DateTime? value)
            => value.HasValue
                ? command.AddParam(parameterName, value.Value)
                : command.AddDBNull(parameterName, DbType.DateTime);

        public static DbParameter AddParam(this DbCommand command, string parameterName, DateTime value)
            => command.AddParameterWithValue(parameterName, value);

        public static DbParameter AddParam(this DbCommand command, string parameterName, string? value)
        {
            var parameter = value == null
                ? command.AddDBNull(parameterName, DbType.String)
                : command.AddParameterWithValue(parameterName, value);
            if (parameter.Size < DefaultLength && parameter.Size >= 0) parameter.Size = DefaultLength;
            return parameter;
        }

        public const int DefaultLength = 4000;

        private static readonly MethodInfo _intEnumParam = GetMethodInfo<Func<DbCommand, string, BindingFlags, DbParameter>>(
            (command, name, value) => command.AddIntEnumParam(name, value)).GetGenericMethodDefinition();

        private static readonly MethodInfo _longEnumParam = GetMethodInfo<Func<DbCommand, string, BindingFlags, DbParameter>>(
            (command, name, value) => command.AddLongEnumParam(name, value)).GetGenericMethodDefinition();

        private static readonly MethodInfo _nullableIntEnumParam = GetMethodInfo<Func<DbCommand, string, BindingFlags?, DbParameter>>(
            (command, name, value) => command.AddIntEnumParam(name, value)).GetGenericMethodDefinition();
        
        private static readonly MethodInfo _nullableLongEnumParam = GetMethodInfo<Func<DbCommand, string, BindingFlags?, DbParameter>>(
            (command, name, value) => command.AddLongEnumParam(name, value)).GetGenericMethodDefinition();
        
        public static DbParameter AddIntEnumParam<T>(this DbCommand command, string parameterName, T value)
            where T : Enum 
            => command.AddParam(parameterName, ToIntCache<T>.Func(value));

        public static DbParameter AddLongEnumParam<T>(this DbCommand command, string parameterName, T value)
            where T : Enum
            => command.AddParam(parameterName, ToLongCache<T>.Func(value));

        public static DbParameter AddIntEnumParam<T>(this DbCommand command, string parameterName, T? value)
            where T : struct, Enum
        {
            int? intValue;
            if (value.HasValue)
                intValue = ToIntCache<T>.Func(value.Value);
            else
                intValue = null;
            return command.AddParam(parameterName, intValue);
        }
        
        public static DbParameter AddLongEnumParam<T>(this DbCommand command, string parameterName, T? value)
            where T : struct, Enum
        {
            long? intValue;
            if (value.HasValue)
                intValue = ToLongCache<T>.Func(value.Value);
            else
                intValue = null;
            return command.AddParam(parameterName, intValue);
        }
        
        private static class ToIntCache<T>
        {
            public static readonly Func<T, int> Func;

            static ToIntCache()
            {
                var dynamicMethod = new DynamicMethod(System.Guid.NewGuid().ToString("N"), typeof(int),
                    new[] {typeof(T)}, true);
                var ilGenerator = dynamicMethod.GetILGenerator();
                ilGenerator.Emit(OpCodes.Ldarg_0);
                ilGenerator.Emit(OpCodes.Ret);
                Func = (Func<T, int>) dynamicMethod.CreateDelegate(typeof(Func<T, int>));
            }
        }

        private static class ToLongCache<T>
        {
            public static readonly Func<T, long> Func;

            static ToLongCache()
            {
                var dynamicMethod = new DynamicMethod(System.Guid.NewGuid().ToString("N"), typeof(long),
                    new[] {typeof(T)}, true);
                var ilGenerator = dynamicMethod.GetILGenerator();
                ilGenerator.Emit(OpCodes.Ldarg_0);
                ilGenerator.Emit(OpCodes.Ret);
                Func = (Func<T, long>) dynamicMethod.CreateDelegate(typeof(Func<T, long>));
            }
        }
    }
}