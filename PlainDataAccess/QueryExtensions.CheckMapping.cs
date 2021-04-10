using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading;

namespace PlainDataAccess
{
    public static partial class QueryExtensions
    {
        private static readonly AsyncLocal<bool> _mappingCheckEnabled = new();

        public static bool MappingCheckEnabled
        {
            get => _mappingCheckEnabled.Value;
            set => _mappingCheckEnabled.Value = value;
        }

        private static void CheckMapping<T>(SqlDataReader reader)
        {
            if (!MappingCheckEnabled)
                return;
            
            CheckMapping(reader, typeof(T));
        }

        internal static void CheckMapping(SqlDataReader reader, Type type)
        {
            if (!MappingCheckEnabled)
                return;

            var readMethod = GetReadMethod(type);

            var properties = type.GetProperties();

            var queryFieldCount = reader.FieldCount;
            var destinationFieldCount = readMethod != null ? 1 : properties.Length;
            if (queryFieldCount != destinationFieldCount)
                throw reader.GetException($"Count of fields does not match. Query has {queryFieldCount} fields. Destination type has {destinationFieldCount} fields.");

            var fieldDictionary = Enumerable.Range(0, reader.FieldCount).ToDictionary(reader.GetName, i => i);

            foreach (var property in properties)
            {
                int ordinal;
                Type destinationType;
                if (readMethod != null)
                {
                    ordinal = 0;
                    destinationType = type;
                }
                else
                {
                    if (!fieldDictionary.TryGetValue(property.Name, out ordinal))
                        throw reader.GetException($"Field '{property.Name}' not found in query.'");
                    destinationType = property.PropertyType;
                }

                CheckFieldType(reader, ordinal, destinationType);
            }
        }

        private static void CheckFieldType(SqlDataReader reader, int ordinal, Type destinationType)
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

        private static Exception GetException(this SqlDataReader reader, string message)
        {
            return new($@"{message} You can copy list of properties to destination type:
{GenerateDestinationProperties(reader)}");
        }

        private static string GenerateDestinationProperties(SqlDataReader reader)
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
        
        private static bool AllowDbNull(SqlDataReader reader, int ordinal)
        {
            return (bool) reader.GetSchemaTable().Rows[ordinal]["AllowDBNull"];
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