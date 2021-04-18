using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using PlainQuery;

namespace PlainQueryExtensions
{
    public static class DbConnectionExtensions
    {
        public static async Task<TKey> Insert<TKey>(this DbConnection connection, object param)
        {
            var type = param.GetType();
            
            var table = GetTableName(type);
            
            var columnInfos = await GetColumns(table, connection, type);
            
            var adapter = connection.Adapter();
            
            var notAutoIncrementColumns = columnInfos
                .Where(_ => !_.IsAutoIncrement && !_.IsReadOnly(type))
                .ToList();
            var columnsClause = string.Join(",", notAutoIncrementColumns.Select(_ => _.ColumnName.EscapedName(adapter)));
            var autoIncrementColumn = columnInfos.SingleOrDefault(_ => _.IsAutoIncrement);
            if (autoIncrementColumn == null)
                throw new Exception("Auto increment column not found.");
            var outClause = autoIncrementColumn.ColumnName.EscapedName(adapter);
            var valuesClause = string.Join(",", notAutoIncrementColumns.Select(_ => $"@{type.EntityColumnName(_.ColumnName)}"));
            
            var queryText = adapter.InsertQueryText(table, columnsClause, valuesClause, outClause);
            
            var query = new Query(queryText, param);
            
            return await query.Single<TKey>(connection);
        }
        
        public static async Task<int> Insert(this DbConnection connection, object param)
        {
            var type = param.GetType();
            
            var table = GetTableName(type);
            
            var columnInfos = await GetColumns(table, connection, type);
            
            var adapter = connection.Adapter();

            var columns = columnInfos
                .Where(_ => !_.IsAutoIncrement && !_.IsReadOnly(type))
                .ToList();
            var columnsClause = string.Join(",", columns.Select(_ => _.ColumnName.EscapedName(adapter)));
            var valuesClause = string.Join(",", columns.Select(_ => $"@{type.EntityColumnName(_.ColumnName)}"));
            
            var query = new Query($@"
INSERT INTO {table} ({columnsClause}) 
VALUES ({valuesClause})", param);
            
            return await query.Execute(connection);
        }
        
        public static async Task<int> Update(this DbConnection connection, object param)
        {
            var type = param.GetType();
            
            var table = GetTableName(type);
            
            var columnInfos = await GetColumns(table, connection, type);
            
            var adapter = connection.Adapter();
            
            var setClause = string.Join(",",
                columnInfos
                    .Where(_ => !_.IsKey && !_.IsReadOnly(type))
                    .Select(_ => $"{_.ColumnName.EscapedName(adapter)}=@{type.EntityColumnName(_.ColumnName)}"));
            var whereClause = string.Join(" AND ", 
                columnInfos.Where(_ => _.IsKey).Select(_ => $"{_.ColumnName.EscapedName(adapter)}=@{type.EntityColumnName(_.ColumnName)}"));
            
            var query = new Query($@"
UPDATE {table}
SET {setClause}
WHERE {whereClause}", param);
            
            return await query.Execute(connection);
        }

        public static async Task<int> Delete<T>(this DbConnection connection, object param)
        {
            var type = typeof(T);
            
            var tableName = GetTableName(type);
            
            var adapter = connection.Adapter();

            var columnInfos = await GetColumns(tableName, connection, type);
            var whereClause = string.Join(" AND ", 
                columnInfos.Where(_ => _.IsKey).Select(_ => $"{_.ColumnName.EscapedName(adapter)}=@{type.EntityColumnName(_.ColumnName)}"));
            
            var query = new Query($@"
DELETE FROM {tableName}
WHERE {whereClause}", param);
            
            return await query.Execute(connection);
        }
        
        public static async Task<T> GetByKey<T>(this DbConnection connection, object param)
        {
            var type = typeof(T);
            
            var tableName = GetTableName(type);
            
            var columnInfos = await GetColumns(tableName, connection, type);
            
            var adapter = connection.Adapter();

            var selectClause = string.Join(",", 
                columnInfos.Select(_ => _.ColumnName.EscapedName(adapter)));
            var whereClause = string.Join(" AND ", 
                columnInfos.Where(_ => _.IsKey).Select(_ => $"{_.ColumnName.EscapedName(adapter)}=@{type.EntityColumnName(_.ColumnName)}"));
            
            var query = new Query($@"
SELECT {selectClause}
FROM {tableName}
WHERE {whereClause}", param);
            
            return await query.Single<T>(connection);
        }

        private static string EscapedName(this string name, ISqlAdapter adapter) => adapter.EscapedName(name);
        
        private static bool IsReadOnly(this ColumnInfo columnInfo, Type type) => type.ColumnIsReadOnly(columnInfo.ColumnName);

        private static bool ColumnIsReadOnly(this Type type, string columnName)
        {
            var key = new IsReadOnlyKey(type, columnName);
            if (_isReadOnlyDictionary.TryGetValue(key, out var value))
                return value;

            bool Find()
            {
                var property = type.FindProperty(columnName);

                if (property == null)
                    return false;

                return Attribute.IsDefined(property, typeof(ReadOnlyAttribute));
            }

            var result = Find();

            _isReadOnlyDictionary.TryAdd(key, result);

            return result;
        }

        private static readonly ConcurrentDictionary<IsReadOnlyKey, bool> _isReadOnlyDictionary = new();

        private record IsReadOnlyKey(Type Type, string ColumnName)
        {
        }

        private static string GetTableName(Type type)
        {
            if (_tableNameDictionary.TryGetValue(type, out var value))
                return value;
            
            var tableAttributeName = type.GetCustomAttribute<TableAttribute>()?.Name;
            var tableName = tableAttributeName ?? type.Name + "s";

            _tableNameDictionary.TryAdd(type, tableName);
            
            return tableName;
        }
        
        private static readonly ConcurrentDictionary<Type, string> _tableNameDictionary = new();
        
        private static readonly ConcurrentDictionary<TableKey, List<ColumnInfo>> _columnDictionary = new();
        
        private static async Task<List<ColumnInfo>> GetColumns(string table, DbConnection connection, Type type)
        {
            var tableKey = new TableKey(table, connection.ConnectionString);
            if (_columnDictionary.TryGetValue(tableKey, out var value)) 
                return value;
            
            var result = await GetColumnEnumerable(table, connection, type);
            _columnDictionary[tableKey] = result;
            return result;
        }

        private record TableKey(string Table, string ConnectionString)
        {
        }

        private static async Task<List<ColumnInfo>> GetColumnEnumerable(string table, DbConnection connection, Type type)
        {
            await connection.OpenIfClosedAsync();

            await using (var command = connection.CreateCommand())
            {
                command.CommandText = $"SELECT * FROM {table}";

                await using (var reader = await command.ExecuteReaderAsync(CommandBehavior.SchemaOnly | CommandBehavior.KeyInfo))
                {
                    reader.CheckMapping(type, connection);

                    var schemaTable = await reader.GetSchemaTableAsync();

                    return schemaTable!.Rows.Cast<DataRow>()
                        .Select(row => new ColumnInfo(
                            (string) row["ColumnName"],
                            true.Equals(row["IsKey"]),
                            true.Equals(row["IsAutoIncrement"])))
                        .ToList();
                }
            }
        }

        private record ColumnInfo(string ColumnName, bool IsKey, bool IsAutoIncrement)
        {
        }
    }
}