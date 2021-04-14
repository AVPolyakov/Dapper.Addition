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
    public static partial class ConnectionHandlerExtensions
    {
        public static async Task<TKey> Insert<TKey>(this IHandler<DbConnection> connectionHandler, object param)
        {
            var type = param.GetType();
            var table = GetTableName(type);
            var columnInfos = await GetColumns(table, connectionHandler, type);
            var notAutoIncrementColumns = columnInfos
                .Where(_ => !_.IsAutoIncrement && !_.IsComputed)
                .ToList();
            var columnsClause = string.Join(",", notAutoIncrementColumns.Select(_ => _.ColumnName.EscapedName()));
            var autoIncrementColumn = columnInfos.SingleOrDefault(_ => _.IsAutoIncrement);
            if (autoIncrementColumn == null)
                throw new Exception("Auto increment column not found.");
            var outClause = autoIncrementColumn.ColumnName.EscapedName();
            var valuesClause = string.Join(",", notAutoIncrementColumns.Select(_ => $"@{_.ColumnName}"));
            var query = new Query($@"
INSERT INTO {table} ({columnsClause}) 
OUTPUT inserted.{outClause}
VALUES ({valuesClause})", param);
            return await query.Single<TKey>(connectionHandler);
        }
        
        public static async Task<int> Insert(this IHandler<DbConnection> connectionHandler, object param)
        {
            var type = param.GetType();
            var table = GetTableName(type);
            var columnInfos = await GetColumns(table, connectionHandler, type);
            var columns = columnInfos
                .Where(_ => !_.IsAutoIncrement && !_.IsComputed)
                .ToList();
            var columnsClause = string.Join(",", columns.Select(_ => _.ColumnName.EscapedName()));
            var valuesClause = string.Join(",", columns.Select(_ => $"@{_.ColumnName}"));
            var query = new Query($@"
INSERT INTO {table} ({columnsClause}) 
VALUES ({valuesClause})", param);
            return await query.Execute(connectionHandler);
        }
        
        public static async Task<int> Update(this IHandler<DbConnection> connectionHandler, object param)
        {
            var type = param.GetType();
            var table = GetTableName(type);
            var columnInfos = await GetColumns(table, connectionHandler, type);
            var setClause = string.Join(",",
                columnInfos
                    .Where(_ => !_.IsKey && !_.IsComputed)
                    .Select(_ => $"{_.ColumnName.EscapedName()}=@{_.ColumnName}"));
            var whereClause = string.Join(" AND ", 
                columnInfos.Where(_ => _.IsKey).Select(_ => $"{_.ColumnName.EscapedName()}=@{_.ColumnName}"));
            var query = new Query($@"
UPDATE {table}
SET {setClause}
WHERE {whereClause}", param);
            return await query.Execute(connectionHandler);
        }

        public static async Task<int> Delete<T>(this IHandler<DbConnection> connectionHandler, object param)
        {
            var type = typeof(T);
            var tableName = GetTableName(type);
            var columnInfos = await GetColumns(tableName, connectionHandler, type);
            var whereClause = string.Join(" AND ", 
                columnInfos.Where(_ => _.IsKey).Select(_ => $"{_.ColumnName.EscapedName()}=@{_.ColumnName}"));
            var query = new Query($@"
DELETE FROM {tableName}
WHERE {whereClause}", param);
            return await query.Execute(connectionHandler);
        }
        
        public static async Task<T> GetByKey<T>(this IHandler<DbConnection> connectionHandler, object param)
        {
            var type = typeof(T);
            var tableName = GetTableName(type);
            var columnInfos = await GetColumns(tableName, connectionHandler, type);
            var selectClause = string.Join(",", 
                columnInfos.Select(_ => _.ColumnName.EscapedName()));
            var whereClause = string.Join(" AND ", 
                columnInfos.Where(_ => _.IsKey).Select(_ => $"{_.ColumnName.EscapedName()}=@{_.ColumnName}"));
            var query = new Query($@"
SELECT {selectClause}
FROM {tableName}
WHERE {whereClause}", param);
            return await query.Single<T>(connectionHandler);
        }

        private static string EscapedName(this string name) => $"[{name}]";

        private static string GetTableName(Type type)
        {
            if (_tableNameDictionary.TryGetValue(type, out var value))
                return value;
            
            var tableAttributeName = type.GetCustomAttribute<TableAttribute>()?.Name;
            var tableName = tableAttributeName ?? type.Name;

            _tableNameDictionary.TryAdd(type, tableName);
            
            return tableName;
        }
        
        private static readonly ConcurrentDictionary<Type, string> _tableNameDictionary = new();
        
        private static readonly ConcurrentDictionary<TableKey, List<ColumnInfo>> _columnDictionary = new();
        
        private static async Task<List<ColumnInfo>> GetColumns(string table, IHandler<DbConnection> connectionHandler, Type type)
        {
            var connectionString = await connectionHandler.Handle(connection => Task.FromResult(connection.ConnectionString));
            var tableKey = new TableKey(table, connectionString);
            if (!_columnDictionary.TryGetValue(tableKey, out var value))
            {
                value = await GetColumnEnumerable(table, connectionHandler, type);
                _columnDictionary[tableKey] = value;
            }
            return value;
        }

        private record TableKey(string Table, string ConnectionString)
        {
        }

        private static Task<List<ColumnInfo>> GetColumnEnumerable(string table, IHandler<DbConnection> connectionHandler, Type type)
        {
            return connectionHandler.Handle(async connection =>
            {
                await connection.OpenIfClosedAsync();

                var computedColumns = new HashSet<string>();
                await using (var command = connection.CreateCommand())
                {
                    command.CommandText = $"SELECT name FROM sys.computed_columns WHERE object_id = OBJECT_ID('{table}')";
                    await using (var reader = await command.ExecuteReaderAsync())
                        while (await reader.ReadAsync())
                            computedColumns.Add(reader.GetString(0));
                }
                
                await using (var command = connection.CreateCommand())
                {
                    command.CommandText = $"SELECT * FROM {table}";
                    await using (var reader = await command.ExecuteReaderAsync(CommandBehavior.SchemaOnly | CommandBehavior.KeyInfo))
                    {
                        reader.CheckMapping(type);

                        var schemaTable = await reader.GetSchemaTableAsync();

                        return schemaTable!.Rows.Cast<DataRow>()
                            .Select(row =>
                            {
                                var columnName = (string) row["ColumnName"];
                                return new ColumnInfo(
                                    columnName,
                                    true.Equals(row["IsKey"]),
                                    true.Equals(row["IsAutoIncrement"]),
                                    computedColumns.Contains(columnName));
                            })
                            .ToList();
                    }
                }
            });
        }
        
        private record ColumnInfo(string ColumnName, bool IsKey, bool IsAutoIncrement, bool IsComputed)
        {
        }
    }
}