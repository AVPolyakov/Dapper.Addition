using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using Dapper;

namespace PlainSql
{
    internal static class TypeExtensions
    {
        internal static string EntityColumnName(this Type type, string name)
        {
            var property = type.FindProperty(name);
            return property != null ? property.Name : name;
        }

        internal static PropertyInfo? FindProperty(this Type type, string name)
        {
            var key = new EntityColumnNameKey(type, name);
            if (_findPropertyDictionary.TryGetValue(key, out var value))
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

            _findPropertyDictionary.TryAdd(key, propertyInfo);
            
            return propertyInfo;
        }
        
        private static readonly ConcurrentDictionary<EntityColumnNameKey, PropertyInfo?> _findPropertyDictionary = new();

        private record EntityColumnNameKey(Type Type, string ColumnName)
        {
        }
    }
}