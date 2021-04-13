using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading;

namespace PlainQueryExtensions
{
    public static partial class QueryExtensions
    {
        private static readonly AsyncLocal<bool> _mappingCheckEnabled = new();

        public static bool MappingCheckEnabled
        {
            get => _mappingCheckEnabled.Value;
            set => _mappingCheckEnabled.Value = value;
        }

        private static void CheckMapping<T>(this DbDataReader reader)
        {
            if (!MappingCheckEnabled)
                return;
            
            reader.CheckMapping(typeof(T));
        }

        internal static void CheckMapping(this DbDataReader reader, Type type)
        {
            if (!MappingCheckEnabled)
                return;

            var readMethod = GetReadMethod(type);

            if (readMethod != null)
            {
                if (reader.FieldCount > 1)
                    throw reader.GetException($"Count of fields is greater than one. Query has {reader.FieldCount} fields.");
                
                CheckFieldType(reader, 0, type);
            }
            else
            {
                var propertiesByName = type.GetProperties().ToDictionary(_ => _.Name);

                for (var ordinal = 0; ordinal < reader.FieldCount; ordinal++)
                {
                    var name = reader.GetName(ordinal);
                    if (!propertiesByName.TryGetValue(name, out var value))
                        throw reader.GetException($"Property '{name}' not found in destination type.");

                    CheckFieldType(reader, ordinal, value.PropertyType);
                }
            }
        }

        private static void CheckFieldType(DbDataReader reader, int ordinal, Type destinationType)
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
                else if (!destinationType.IsValueType && TypesAreCompatible(fieldType, destinationType))
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
                var fieldTypeName = fieldType.GetCSharpName();
                return reader.GetException($"Type of field '{name}' does not match. Field type is '{destinationTypeName}' in destination and `{fieldTypeName}` with AllowDbNull='{allowDbNull}' in query.");
            }
        }

        private static Exception GetException(this DbDataReader reader, string message)
        {
            return new($@"{message} You can copy list of properties to destination type:
{GenerateDestinationProperties(reader)}");
        }

        private static string GenerateDestinationProperties(DbDataReader reader)
        {
            return string.Join(@"
",
                Enumerable.Range(0, reader.FieldCount).Select(i =>
                {
                    var type = AllowDbNull(reader, i) && reader.GetFieldType(i).IsValueType
                        ? typeof(Nullable<>).MakeGenericType(reader.GetFieldType(i))
                        : reader.GetFieldType(i);
                    var typeName = type.GetCSharpName();
                    return $"        public {typeName} {reader.GetName(i)} {{ get; set; }}";
                }));
        }
        
        private static bool TypesAreCompatible(Type dbType, Type type)
        {
            if (type.IsEnum && dbType == Enum.GetUnderlyingType(type)) 
                return true;
            return type == dbType;
        }
        
        private static bool AllowDbNull(DbDataReader reader, int ordinal)
        {
            return (bool) reader.GetSchemaTable()!.Rows[ordinal]["AllowDBNull"];
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