using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace PlainQueryExtensions
{
    public static partial class QueryExtensions
    {
        public static Func<DbDataReader, T> GetMaterializer<T>(this DbDataReader reader, DbCommand command)
        {
            var destinationType = typeof(T);

            var key = new MaterializerKey(command.CommandText, command.CommandType, command?.Connection?.ConnectionString);

            var dictionary = ReadCache<T>.Dictionary;
            
            if (dictionary.TryGetValue(key, out var func))
                return func;

            lock (dictionary)
            {
                var dynamicMethod = new DynamicMethod(System.Guid.NewGuid().ToString("N"), destinationType,
                    new[] {typeof(DbDataReader)}, true);

                var ilGenerator = dynamicMethod.GetILGenerator();
                var readMethod = GetReadMethod(destinationType);
                if (readMethod != null)
                {
                    ilGenerator.Emit(OpCodes.Ldarg_0);
                    ilGenerator.Emit(OpCodes.Ldc_I4_0);
                    ilGenerator.EmitCall(readMethod.IsVirtual ? OpCodes.Callvirt : OpCodes.Call, readMethod, null);
                    ilGenerator.Emit(OpCodes.Ret);
                }
                else
                {
                    ilGenerator.Emit(OpCodes.Newobj, destinationType.GetConstructor(Array.Empty<Type>())!);
                    var propertiesByName = destinationType.GetProperties().ToDictionary(_ => _.Name);
                    for (var ordinal = 0; ordinal < reader.FieldCount; ordinal++)
                    {
                        if (!propertiesByName.TryGetValue(reader.GetName(ordinal), out var info))
                            continue;

                        ilGenerator.Emit(OpCodes.Dup);
                        ilGenerator.Emit(OpCodes.Ldarg_0);
                        EmitOrdinal(ilGenerator, ordinal);
                        var method = GetReadMethod(info.PropertyType);
                        if (method == null)
                            throw new Exception($"Read method not fount for type '{info.PropertyType.FullName}'");
                        ilGenerator.EmitCall(method.IsVirtual ? OpCodes.Callvirt : OpCodes.Call, method, null);
                        ilGenerator.EmitCall(OpCodes.Callvirt, info.GetSetMethod()!, null);
                    }
                    ilGenerator.Emit(OpCodes.Ret);
                }

                var @delegate = (Func<DbDataReader, T>) dynamicMethod.CreateDelegate(typeof(Func<DbDataReader, T>));

                dictionary.TryAdd(key, @delegate);

                return @delegate;
            }
        }

        private static class ReadCache<T>
        {
            public static readonly ConcurrentDictionary<MaterializerKey, Func<DbDataReader, T>> Dictionary = new();
        }

        private record MaterializerKey(string CommandText, CommandType CommandType, string? ConnectionString)
        {
        }
        
        private static void EmitOrdinal(ILGenerator ilGenerator, int ordinal)
        {
            switch (ordinal)
            {
                case 0:
                    ilGenerator.Emit(OpCodes.Ldc_I4_0);
                    break;
                case 1:
                    ilGenerator.Emit(OpCodes.Ldc_I4_1);
                    break;
                case 2:
                    ilGenerator.Emit(OpCodes.Ldc_I4_2);
                    break;
                case 3:
                    ilGenerator.Emit(OpCodes.Ldc_I4_3);
                    break;
                case 4:
                    ilGenerator.Emit(OpCodes.Ldc_I4_4);
                    break;
                case 5:
                    ilGenerator.Emit(OpCodes.Ldc_I4_5);
                    break;
                case 6:
                    ilGenerator.Emit(OpCodes.Ldc_I4_6);
                    break;
                case 7:
                    ilGenerator.Emit(OpCodes.Ldc_I4_7);
                    break;
                case 8:
                    ilGenerator.Emit(OpCodes.Ldc_I4_8);
                    break;
                default:
                    ilGenerator.Emit(OpCodes.Ldc_I4_S, ordinal);
                    break;
            }
        }        

        private static MethodInfo? GetReadMethod(Type type)
        {
            if (ReadMethodInfos.TryGetValue(type, out var methodInfo))
                return methodInfo;
            if (type.IsEnum)
            {
                //TODO: add code generation for byte, short
                var underlyingType = Enum.GetUnderlyingType(type);
                if (underlyingType == typeof(int))
                    return _intEnum.MakeGenericMethod(type);
                if (underlyingType == typeof(long))
                    return _longEnum.MakeGenericMethod(type);
            }
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                var argType = type.GetGenericArguments().Single();
                if (argType.IsEnum)
                {
                    var underlyingType = Enum.GetUnderlyingType(argType);
                    if (underlyingType == typeof(int))
                        return _nullableIntEnum.MakeGenericMethod(argType);
                    if (underlyingType == typeof(long))
                        return _nullableLongEnum.MakeGenericMethod(argType);
                }
            }
            return null;
        }

        public static readonly Dictionary<Type, MethodInfo> ReadMethodInfos = new[]
            {
                GetMethodInfo<Func<DbDataReader, int, int>>((reader, i) => reader.Int32(i)),
                GetMethodInfo<Func<DbDataReader, int, int?>>((reader, i) => reader.NullableInt32(i)),
                GetMethodInfo<Func<DbDataReader, int, long>>((reader, i) => reader.Int64(i)),
                GetMethodInfo<Func<DbDataReader, int, long?>>((reader, i) => reader.NullableInt64(i)),
                GetMethodInfo<Func<DbDataReader, int, decimal>>((reader, i) => reader.Decimal(i)),
                GetMethodInfo<Func<DbDataReader, int, decimal?>>((reader, i) => reader.NullableDecimal(i)),
                GetMethodInfo<Func<DbDataReader, int, Guid>>((reader, i) => reader.Guid(i)),
                GetMethodInfo<Func<DbDataReader, int, Guid?>>((reader, i) => reader.NullableGuid(i)),
                GetMethodInfo<Func<DbDataReader, int, DateTime>>((reader, i) => reader.DateTime(i)),
                GetMethodInfo<Func<DbDataReader, int, DateTime?>>((reader, i) => reader.NullableDateTime(i)),
                GetMethodInfo<Func<DbDataReader, int, string?>>((reader, i) => reader.String(i)),
                GetMethodInfo<Func<DbDataReader, int, bool>>(((reader, i) => reader.Boolean(i))),
                GetMethodInfo<Func<DbDataReader, int, bool?>>((reader, i) => reader.NullableBoolean(i))
            }
            .ToDictionary(methodInfo => methodInfo.ReturnType);
        
        public static int Int32(this DbDataReader reader, int ordinal) 
            => reader.GetInt32(ordinal);

        public static long? NullableInt64(this DbDataReader reader, int ordinal)
            => reader.IsDBNull(ordinal) ? new long?() : reader.GetInt64(ordinal);
        
        public static long Int64(this DbDataReader reader, int ordinal)
            => reader.GetInt64(ordinal);

        public static int? NullableInt32(this DbDataReader reader, int ordinal)
            => reader.IsDBNull(ordinal) ? new int?() : reader.GetInt32(ordinal);
        
        public static decimal Decimal(this DbDataReader reader, int ordinal)
            => reader.GetDecimal(ordinal);

        public static decimal? NullableDecimal(this DbDataReader reader, int ordinal)
            => reader.IsDBNull(ordinal) ? new decimal?() : reader.GetDecimal(ordinal);

        public static Guid Guid(this DbDataReader reader, int ordinal)
            => reader.GetGuid(ordinal);

        public static Guid? NullableGuid(this DbDataReader reader, int ordinal)
            => reader.IsDBNull(ordinal) ? new Guid?() : reader.GetGuid(ordinal);
        
        public static DateTime DateTime(this DbDataReader reader, int ordinal)
            => reader.GetDateTime(ordinal);

        public static DateTime? NullableDateTime(this DbDataReader reader, int ordinal)
            => reader.IsDBNull(ordinal) ? new DateTime?() : reader.GetDateTime(ordinal);

        public static string? String(this DbDataReader reader, int ordinal)
            => reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
        
        public static bool Boolean(this DbDataReader reader, int ordinal)
            => reader.GetBoolean(ordinal);

        public static bool? NullableBoolean(this DbDataReader reader, int ordinal)
            => reader.IsDBNull(ordinal) ? new bool?() : reader.GetBoolean(ordinal);
        
        private static readonly MethodInfo _intEnum = GetMethodInfo<Func<DbDataReader, int, BindingFlags>>(
            (reader, ordinal) => reader.IntEnum<BindingFlags>(ordinal)).GetGenericMethodDefinition();
        
        private static readonly MethodInfo _longEnum = GetMethodInfo<Func<DbDataReader, int, BindingFlags>>(
            (reader, ordinal) => reader.LongEnum<BindingFlags>(ordinal)).GetGenericMethodDefinition();
        
        private static readonly MethodInfo _nullableIntEnum = GetMethodInfo<Func<DbDataReader, int, BindingFlags?>>(
            (reader, ordinal) => reader.NullableIntEnum<BindingFlags>(ordinal)).GetGenericMethodDefinition();

        private static readonly MethodInfo _nullableLongEnum = GetMethodInfo<Func<DbDataReader, int, BindingFlags?>>(
            (reader, ordinal) => reader.NullableLongEnum<BindingFlags>(ordinal)).GetGenericMethodDefinition();

        public static T IntEnum<T>(this DbDataReader reader, int ordinal)
            where T : Enum
            => IntToEnumCache<T>.Func(reader.GetInt32(ordinal));
        
        public static T LongEnum<T>(this DbDataReader reader, int ordinal)
            where T : Enum
            => LongToEnumCache<T>.Func(reader.GetInt64(ordinal));

        public static T? NullableIntEnum<T>(this DbDataReader reader, int ordinal)
            where T : struct, Enum
            => reader.IsDBNull(ordinal)
                ? new T?()
                : IntToEnumCache<T>.Func(reader.GetInt32(ordinal));

        public static T? NullableLongEnum<T>(this DbDataReader reader, int ordinal)
            where T : struct, Enum
            => reader.IsDBNull(ordinal)
                ? new T?()
                : LongToEnumCache<T>.Func(reader.GetInt64(ordinal));

        private static class IntToEnumCache<T>
        {
            public static readonly Func<int, T> Func;

            static IntToEnumCache()
            {
                var dynamicMethod = new DynamicMethod(System.Guid.NewGuid().ToString("N"), typeof(T),
                    new[] {typeof(int)}, true);
                var ilGenerator = dynamicMethod.GetILGenerator();
                ilGenerator.Emit(OpCodes.Ldarg_0);
                ilGenerator.Emit(OpCodes.Ret);
                Func = (Func<int, T>) dynamicMethod.CreateDelegate(typeof(Func<int, T>));
            }
        }
        
        private static class LongToEnumCache<T>
        {
            public static readonly Func<long, T> Func;

            static LongToEnumCache()
            {
                var dynamicMethod = new DynamicMethod(System.Guid.NewGuid().ToString("N"), typeof(T),
                    new[] {typeof(long)}, true);
                var ilGenerator = dynamicMethod.GetILGenerator();
                ilGenerator.Emit(OpCodes.Ldarg_0);
                ilGenerator.Emit(OpCodes.Ret);
                Func = (Func<long, T>) dynamicMethod.CreateDelegate(typeof(Func<long, T>));
            }
        }
    }
}