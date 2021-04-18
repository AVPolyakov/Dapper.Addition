using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading.Tasks;
using Dapper;
using PlainQuery.PostgreSql;
using PlainQuery.SqlServer;

namespace PlainQuery
{
    public static partial class QueryExtensions
    {
        private static async Task CheckMapping<T>(this Query query, DbConnection connection)
        {
            if (!MappingCheckSettings.MappingCheckEnabled)
                return;
            
            await connection.OpenIfClosedAsync();

            await using (var command = connection.CreateCommand())
            {
                command.CommandText = query.StringBuilder.ToString();

                command.AddParams(query);

                await using (var reader = await command.ExecuteReaderAsync(CommandBehavior.SchemaOnly)) 
                    reader.CheckMapping(typeof(T), connection);
            }
        }
        
        internal static async Task OpenIfClosedAsync(this DbConnection connection)
        {
            if (connection.State == ConnectionState.Closed)
                await connection.OpenAsync();
        }
        
        internal static void CheckMapping(this DbDataReader reader, Type type, DbConnection connection)
        {
            if (!MappingCheckSettings.MappingCheckEnabled)
                return;

            if (type.ReadTypeIsScalar())
            {
                if (reader.FieldCount > 1)
                    throw reader.GetException($"Count of fields is greater than one. Query has {reader.FieldCount} fields.", type);
                
                CheckFieldType(reader, 0, type, type, connection);
            }
            else
            {

                for (var ordinal = 0; ordinal < reader.FieldCount; ordinal++)
                {
                    var name = reader.GetName(ordinal);

                    var propertyInfo = type.FindProperty(name);
                    if (propertyInfo == null)
                        throw reader.GetException($"Property '{name.ToCamelCase()}' not found in destination type.", type);
                    
                    CheckFieldType(reader, ordinal, propertyInfo.PropertyType, type, connection);
                }
            }
        }

        private static readonly ConcurrentDictionary<EntityColumnNameKey, PropertyInfo?> _columnDictionary = new();

        private record EntityColumnNameKey(Type Type, string ColumnName)
        {
        }

        internal static string EntityColumnName(this Type type, string name)
        {
            var property = type.FindProperty(name);
            return property != null ? property.Name : name;
        }

        internal static PropertyInfo? FindProperty(this Type type, string name)
        {
            var key = new EntityColumnNameKey(type, name);
            if (_columnDictionary.TryGetValue(key, out var value))
                return value;

            PropertyInfo? Find()
            {
                var properties = type.GetProperties();

                {
                    var property = properties.SingleOrDefault(p => p.Name == name);
                    if (property != null)
                        return property;
                }

                if (DefaultTypeMap.MatchNamesWithUnderscores)
                {
                    var property = properties.SingleOrDefault(p => string.Equals(p.Name, name.Replace("_", ""), StringComparison.OrdinalIgnoreCase));
                    if (property != null)
                        return property;
                }
                
                return null;
            }

            var propertyInfo = Find();

            _columnDictionary.TryAdd(key, propertyInfo);
            
            return propertyInfo;
        }
        
        private static bool ReadTypeIsScalar(this Type type)
        {
            if (_scalarReadType.Contains(type))
                return true;
            if (type.IsEnum)
                return true;
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                var argType = type.GetGenericArguments().Single();
                if (argType.IsEnum)
                    return true;
            }
            return false;
        }

        private static readonly HashSet<Type> _scalarReadType = new[]
            {
                typeof(byte),
                typeof(byte?),
                typeof(int),
                typeof(int?),
                typeof(long),
                typeof(long?),
                typeof(decimal),
                typeof(decimal?),
                typeof(Guid),
                typeof(Guid?),
                typeof(DateTime),
                typeof(DateTime?),
                typeof(string),
                typeof(bool),
                typeof(bool?),
                typeof(byte[]),
            }
            .ToHashSet();
        
        private static void CheckFieldType(DbDataReader reader, int ordinal, Type destinationType, Type type, DbConnection connection)
        {
            var fieldType = reader.GetFieldType(ordinal);
            var allowDbNull = AllowDbNull(reader, ordinal);
            if (allowDbNull)
            {
                if (destinationType.IsGenericType && destinationType.GetGenericTypeDefinition() == typeof(Nullable<>) &&
                    TypesAreCompatible(fieldType, destinationType.GetGenericArguments().Single()))
                {
                    //no-op
                }
                else if ((!destinationType.IsValueType || !connection.Adapter().NullabilityCheckEnabled) && 
                    TypesAreCompatible(fieldType, destinationType))
                {
                    //no-op
                }
                else
                    throw GetInnerException();
            }
            else
            {
                if (!TypesAreCompatible(fieldType, destinationType))
                    throw GetInnerException();
            }
            
            Exception GetInnerException()
            {
                var name = reader.GetName(ordinal);
                var destinationTypeName = destinationType.GetCSharpName();
                var fieldTypeName = reader.GetFieldTypeWithNullable(ordinal).GetCSharpName();
                return reader.GetException($"Type of field '{name.ToCamelCase()}' does not match. Field type is '{destinationTypeName}' in destination and `{fieldTypeName}` in query.", type);
            }
        }

        internal static ISqlAdapter Adapter(this DbConnection connection)
        {
            var connectionType = connection.GetType();
            var connectionTypeName = connectionType.Name.ToLower();

            if (_adapterDictionary.TryGetValue(connectionTypeName, out var value))
                return value;

            throw new Exception($"Unknown connection type '{connectionType}'");
        }

        private static readonly Dictionary<string, ISqlAdapter> _adapterDictionary = new()
        {
            ["sqlconnection"] = new SqlServerAdapter(),
            ["npgsqlconnection"] = new PostgreSqlAdapter(),
        };

        private static Exception GetException(this DbDataReader reader, string message, Type type)
        {
            return new($@"{message} You can copy list of properties to destination type {type.FullName}:
{GenerateDestinationProperties(reader)}");
        }

        private static string GenerateDestinationProperties(DbDataReader reader)
        {
            return string.Join(@"
",
                Enumerable.Range(0, reader.FieldCount).Select(i =>
                {
                    var type = reader.GetFieldTypeWithNullable(i);
                    var typeName = type.GetCSharpName();
                    return $"        public {typeName} {reader.GetName(i).EntityColumnName()} {{ get; set; }}";
                }));
        }

        private static Type GetFieldTypeWithNullable(this DbDataReader reader, int i)
        {
            return AllowDbNull(reader, i) && reader.GetFieldType(i).IsValueType
                ? typeof(Nullable<>).MakeGenericType(reader.GetFieldType(i))
                : reader.GetFieldType(i);
        }

        private static string EntityColumnName(this string name)
        {
            return DefaultTypeMap.MatchNamesWithUnderscores ? name.ToCamelCase() : name;
        }

        private static string ToCamelCase(this string name)
        {
            IEnumerable<char> Enumerate()
            {
                var toUpper = true;
                foreach (var c in name)
                {
                    if (toUpper)
                    {
                        toUpper = false;
                        yield return char.ToUpper(c);
                    }
                    else
                    {
                        if (c == '_')
                            toUpper = true;
                        else
                            yield return c;
                    }
                }
            }

            return new string(Enumerate().ToArray());
        }

        private static bool TypesAreCompatible(Type dbType, Type type)
        {
            if (type.IsEnum && dbType == Enum.GetUnderlyingType(type)) 
                return true;
            return type == dbType;
        }
        
        private static bool AllowDbNull(DbDataReader reader, int ordinal)
        {
            var row = reader.GetSchemaTable()!.Rows[ordinal];
            if (false.Equals(row["AllowDBNull"]))
                return false;
            else
                return true;
        }
        
        /// <summary>
        /// https://stackoverflow.com/a/33529925
        /// </summary>
        private static string GetCSharpName(this Type type)
        {
            if (_typeToFriendlyName.TryGetValue(type, out var friendlyName))
            {
                return friendlyName;
            }

            friendlyName = type.Name;
            if (type.IsGenericType)
            {
                if (type.GetGenericTypeDefinition() == typeof(Nullable<>))
                    friendlyName = type.GetGenericArguments().Single().GetCSharpName() + "?";
                else
                {
                    int backtick = friendlyName.IndexOf('`');
                    if (backtick > 0)
                    {
                        friendlyName = friendlyName.Remove(backtick);
                    }
                    friendlyName += "<";
                    Type[] typeParameters = type.GetGenericArguments();
                    for (int i = 0; i < typeParameters.Length; i++)
                    {
                        string typeParamName = typeParameters[i].GetCSharpName();
                        friendlyName += (i == 0 ? typeParamName : ", " + typeParamName);
                    }
                    friendlyName += ">";
                }
            }

            if (type.IsArray)
            {
                return type.GetElementType()!.GetCSharpName() + "[]";
            }

            return friendlyName;
        }
        
        private static readonly Dictionary<Type, string> _typeToFriendlyName = new()
        {
            {typeof(string), "string"},
            {typeof(object), "object"},
            {typeof(bool), "bool"},
            {typeof(byte), "byte"},
            {typeof(char), "char"},
            {typeof(decimal), "decimal"},
            {typeof(double), "double"},
            {typeof(short), "short"},
            {typeof(int), "int"},
            {typeof(long), "long"},
            {typeof(sbyte), "sbyte"},
            {typeof(float), "float"},
            {typeof(ushort), "ushort"},
            {typeof(uint), "uint"},
            {typeof(ulong), "ulong"},
            {typeof(void), "void"}
        };
        
        internal static void AddParams(this DbCommand command, Query query)
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
                var dynamicMethod = new DynamicMethod(Guid.NewGuid().ToString("N"), null,
                    new[] {typeof(DbCommand), typeof(T)}, true);

                var ilGenerator = dynamicMethod.GetILGenerator();

                foreach (var info in typeof(T).GetProperties())
                {
                    ilGenerator.Emit(OpCodes.Ldarg_0);
                    ilGenerator.Emit(OpCodes.Ldstr, "@" + info.Name);
                    ilGenerator.Emit(OpCodes.Ldarg_1);
                    ilGenerator.EmitCall(OpCodes.Callvirt, info.GetGetMethod()!, null);
                    ilGenerator.EmitCall(OpCodes.Call, GetAddParamMethod(info.PropertyType), null);
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
            if (typeof(SqlMapper.ICustomQueryParameter) == type)
                return GetMethodInfo<Action<DbCommand, string, SqlMapper.ICustomQueryParameter>>((command, name, value) => command.AddParam(name, value));
            throw new Exception($"Method of parameter adding not found for type '{type.FullName}'");
        }
        
        private static readonly Dictionary<Type, MethodInfo> AddParamsMethods = new[]
            {
                GetMethodInfo<Action<DbCommand, string, byte>>((command, name, value) => command.AddParam(name, value)),
                GetMethodInfo<Action<DbCommand, string, byte?>>((command, name, value) => command.AddParam(name, value)),
                GetMethodInfo<Action<DbCommand, string, int>>((command, name, value) => command.AddParam(name, value)),
                GetMethodInfo<Action<DbCommand, string, int?>>((command, name, value) => command.AddParam(name, value)),
                GetMethodInfo<Action<DbCommand, string, long>>((command, name, value) => command.AddParam(name, value)),
                GetMethodInfo<Action<DbCommand, string, long?>>((command, name, value) => command.AddParam(name, value)),
                GetMethodInfo<Action<DbCommand, string, decimal>>((command, name, value) => command.AddParam(name, value)),
                GetMethodInfo<Action<DbCommand, string, decimal?>>((command, name, value) => command.AddParam(name, value)),
                GetMethodInfo<Action<DbCommand, string, bool>>((command, name, value) => command.AddParam(name, value)),
                GetMethodInfo<Action<DbCommand, string, bool?>>((command, name, value) => command.AddParam(name, value)),
                GetMethodInfo<Action<DbCommand, string, Guid>>((command, name, value) => command.AddParam(name, value)),
                GetMethodInfo<Action<DbCommand, string, Guid?>>((command, name, value) => command.AddParam(name, value)),
                GetMethodInfo<Action<DbCommand, string, DateTime>>((command, name, value) => command.AddParam(name, value)),
                GetMethodInfo<Action<DbCommand, string, DateTime?>>((command, name, value) => command.AddParam(name, value)),
                GetMethodInfo<Action<DbCommand, string, string?>>((command, name, value) => command.AddParam(name, value)),
                GetMethodInfo<Action<DbCommand, string, byte[]?>>((command, name, value) => command.AddParam(name, value)),
            }
            .ToDictionary(methodInfo => methodInfo.GetParameters()[2].ParameterType);

        private static MethodInfo GetMethodInfo<T>(Expression<T> expression)
            => ((MethodCallExpression) expression.Body).Method;

        private static DbParameter AddParameterWithValue(this DbCommand command, string parameterName, object parameterValue)
        {
            var parameter = command.CreateParameter();
            parameter.ParameterName = parameterName;
            parameter.Value = parameterValue;
            command.Parameters.Add(parameter);
            return parameter;
        }
        
        private static DbParameter AddDbNull(this DbCommand command, string parameterName, DbType dbType)
        {
            var parameter = command.CreateParameter();
            parameter.ParameterName = parameterName;
            parameter.DbType = dbType;
            parameter.Value = DBNull.Value;
            command.Parameters.Add(parameter);
            return parameter;
        }
        
        private static void AddParam(this DbCommand command, string parameterName, byte value)
            => command.AddParameterWithValue(parameterName, value);
        
        private static void AddParam(this DbCommand command, string parameterName, byte? value)
        {
            if (value.HasValue)
                command.AddParam(parameterName, value.Value);
            else
                command.AddDbNull(parameterName, DbType.Byte);
        }

        private static void AddParam(this DbCommand command, string parameterName, int value)
            => command.AddParameterWithValue(parameterName, value);
        
        private static void AddParam(this DbCommand command, string parameterName, int? value)
        {
            if (value.HasValue)
                AddParam(command, parameterName, value.Value);
            else
                command.AddDbNull(parameterName, DbType.Int32);
        }

        private static void AddParam(this DbCommand command, string parameterName, long value)
            => command.AddParameterWithValue(parameterName, value);
        
        private static void AddParam(this DbCommand command, string parameterName, long? value)
        {
            if (value.HasValue)
                command.AddParam(parameterName, value.Value);
            else
                command.AddDbNull(parameterName, DbType.Int64);
        }

        private static void AddParam(this DbCommand command, string parameterName, decimal value)
        {
            var parameter = command.AddParameterWithValue(parameterName, value);
            const byte defaultPrecision = 38;
            if (parameter.Precision < defaultPrecision) parameter.Precision = defaultPrecision;
            const byte defaultScale = 8;
            if (parameter.Scale < defaultScale) parameter.Scale = 8;
        }
        
        private static void AddParam(this DbCommand command, string parameterName, decimal? value)
        {
            if (value.HasValue)
                command.AddParam(parameterName, value.Value);
            else
                command.AddDbNull(parameterName, DbType.Decimal);
        }

        private static void AddParam(this DbCommand command, string parameterName, bool value)
            => command.AddParameterWithValue(parameterName, value);
        
        private static void AddParam(this DbCommand command, string parameterName, bool? value)
        {
            if (value.HasValue)
                command.AddParam(parameterName, value.Value);
            else
                command.AddDbNull(parameterName, DbType.Boolean);
        }

        private static void AddParam(this DbCommand command, string parameterName, Guid value)
            => command.AddParameterWithValue(parameterName, value);

        private static void AddParam(this DbCommand command, string parameterName, Guid? value)
        {
            if (value.HasValue)
                command.AddParam(parameterName, value.Value);
            else
                command.AddDbNull(parameterName, DbType.Guid);
        }

        private static void AddParam(this DbCommand command, string parameterName, DateTime value)
            => command.AddParameterWithValue(parameterName, value);
        
        private static void AddParam(this DbCommand command, string parameterName, DateTime? value)
        {
            if (value.HasValue)
                command.AddParam(parameterName, value.Value);
            else
                command.AddDbNull(parameterName, DbType.DateTime);
        }

        private static void AddParam(this DbCommand command, string parameterName, string? value)
        {
            var parameter = value == null
                ? command.AddDbNull(parameterName, DbType.String)
                : command.AddParameterWithValue(parameterName, value);
            if (parameter.Size < DefaultLength && parameter.Size >= 0) parameter.Size = DefaultLength;
        }

        private static void AddParam(this DbCommand command, string parameterName, byte[]? value)
        {
            if (value == null)
                command.AddDbNull(parameterName, DbType.Binary);
            else
                command.AddParameterWithValue(parameterName, value);
        }

        private static void AddParam(this DbCommand command, string parameterName, SqlMapper.ICustomQueryParameter value)
        {
            value.AddParameter(command, parameterName);
        }

        private const int DefaultLength = 4000;

        private static readonly MethodInfo _intEnumParam = GetMethodInfo<Action<DbCommand, string, BindingFlags>>(
            (command, name, value) => command.AddIntEnumParam(name, value)).GetGenericMethodDefinition();

        private static readonly MethodInfo _longEnumParam = GetMethodInfo<Action<DbCommand, string, BindingFlags>>(
            (command, name, value) => command.AddLongEnumParam(name, value)).GetGenericMethodDefinition();

        private static readonly MethodInfo _nullableIntEnumParam = GetMethodInfo<Action<DbCommand, string, BindingFlags?>>(
            (command, name, value) => command.AddIntEnumParam(name, value)).GetGenericMethodDefinition();
        
        private static readonly MethodInfo _nullableLongEnumParam = GetMethodInfo<Action<DbCommand, string, BindingFlags?>>(
            (command, name, value) => command.AddLongEnumParam(name, value)).GetGenericMethodDefinition();
        
        private static void AddIntEnumParam<T>(this DbCommand command, string parameterName, T value)
            where T : Enum 
            => command.AddParam(parameterName, ToIntCache<T>.Func(value));

        private static void AddLongEnumParam<T>(this DbCommand command, string parameterName, T value)
            where T : Enum
            => command.AddParam(parameterName, ToLongCache<T>.Func(value));

        private static void AddIntEnumParam<T>(this DbCommand command, string parameterName, T? value)
            where T : struct, Enum
        {
            int? intValue;
            if (value.HasValue)
                intValue = ToIntCache<T>.Func(value.Value);
            else
                intValue = null;
            command.AddParam(parameterName, intValue);
        }
        
        private static void AddLongEnumParam<T>(this DbCommand command, string parameterName, T? value)
            where T : struct, Enum
        {
            long? intValue;
            if (value.HasValue)
                intValue = ToLongCache<T>.Func(value.Value);
            else
                intValue = null;
            command.AddParam(parameterName, intValue);
        }
        
        private static class ToIntCache<T>
        {
            public static readonly Func<T, int> Func;

            static ToIntCache()
            {
                var dynamicMethod = new DynamicMethod(Guid.NewGuid().ToString("N"), typeof(int),
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
                var dynamicMethod = new DynamicMethod(Guid.NewGuid().ToString("N"), typeof(long),
                    new[] {typeof(T)}, true);
                var ilGenerator = dynamicMethod.GetILGenerator();
                ilGenerator.Emit(OpCodes.Ldarg_0);
                ilGenerator.Emit(OpCodes.Ret);
                Func = (Func<T, long>) dynamicMethod.CreateDelegate(typeof(Func<T, long>));
            }
        }
    }
}