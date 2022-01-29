using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;

namespace Dapper.Addition
{
    internal static class DataReaderExtensions
    {
        internal static void CheckMapping(this IDataReader reader, Type type)
        {
            if (!Sql.MappingCheckEnabled)
                return;

            if (type.ReadTypeIsScalar())
            {
                if (reader.FieldCount > 1)
                    throw reader.GetException($"Count of fields is greater than one. Query has {reader.FieldCount} fields.", type);
                
                CheckFieldType(reader, 0, type, type);
            }
            else
            {

                for (var ordinal = 0; ordinal < reader.FieldCount; ordinal++)
                {
                    var name = reader.GetName(ordinal);

                    var propertyInfo = type.FindProperty(name);
                    if (propertyInfo == null)
                        throw reader.GetException($"Property '{name.ToCamelCase()}' not found in destination type.", type);
                    
                    CheckFieldType(reader, ordinal, propertyInfo.PropertyType, type);
                }
            }
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
        
        private static void CheckFieldType(IDataReader reader, int ordinal, Type destinationType, Type type)
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
                else if ((!destinationType.IsValueType || !ISqlAdapter.Current.NullabilityCheckEnabled) && 
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
        
        private static Exception GetException(this IDataReader reader, string message, Type type)
        {
            return new($@"{message} You can copy list of properties to destination type {type.FullName}:
{GenerateDestinationProperties(reader)}");
        }

        private static string GenerateDestinationProperties(IDataReader reader)
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

        private static Type GetFieldTypeWithNullable(this IDataReader reader, int i)
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
        
        private static bool AllowDbNull(IDataReader reader, int ordinal)
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
    }
}