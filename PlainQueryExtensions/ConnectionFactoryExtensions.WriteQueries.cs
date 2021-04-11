using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using PlainQuery;

namespace PlainQueryExtensions
{
    public static class ConnectionProviderExtensions
    {
        public static async Task<TKey> Insert<TKey>(this IConnectionProvider connectionProvider, object param)
        {
            var type = param.GetType();
            var table = GetTableName(type);
            var columnInfos = await GetColumns(table, connectionProvider, type);
            var notAutoIncrementColumns = columnInfos
                .Where(_ => !_.IsAutoIncrement)
                .Select(_ => _.ColumnName)
                .ToList();
            var columnsClause = string.Join(",", notAutoIncrementColumns);
            var outClause = columnInfos.Single(_ => _.IsAutoIncrement).ColumnName;
            var valuesClause = string.Join(",", notAutoIncrementColumns.Select(_ => $"@{_}"));
            var query = new Query($@"
INSERT INTO {table} ({columnsClause}) 
OUTPUT inserted.{outClause}
VALUES ({valuesClause})", param);
            return await query.Single<TKey>(connectionProvider);
        }
        
        public static async Task<int> Insert(this IConnectionProvider connectionProvider, object param)
        {
            var type = param.GetType();
            var table = GetTableName(type);
            var columnInfos = await GetColumns(table, connectionProvider, type);
            var columns = columnInfos
                .Select(_ => _.ColumnName)
                .ToList();
            var columnsClause = string.Join(",", columns);
            var valuesClause = string.Join(",", columns.Select(_ => $"@{_}"));
            var query = new Query($@"
INSERT INTO {table} ({columnsClause}) 
VALUES ({valuesClause})", param);
            return await query.Execute(connectionProvider);
        }
        
        public static async Task<int> Update(this IConnectionProvider connectionProvider, object param)
        {
            var type = param.GetType();
            var table = GetTableName(type);
            var columnInfos = await GetColumns(table, connectionProvider, type);
            var setClause = string.Join(",",
                columnInfos
                    .Where(_ => !_.IsKey)
                    .Select(_ => $"{_.ColumnName}=@{_.ColumnName}"));
            var whereClause = string.Join(" AND ", 
                columnInfos.Where(_ => _.IsKey).Select(_ => $"{_.ColumnName}=@{_.ColumnName}"));
            var query = new Query($@"
UPDATE {table}
SET {setClause}
WHERE {whereClause}", param);
            return await query.Execute(connectionProvider);
        }

        public static async Task<int> Delete<T>(this IConnectionProvider connectionProvider, object param)
        {
            var type = typeof(T);
            var tableName = GetTableName(type);
            var columnInfos = await GetColumns(tableName, connectionProvider, type);
            var whereClause = string.Join(" AND ", 
                columnInfos.Where(_ => _.IsKey).Select(_ => $"{_.ColumnName}=@{_.ColumnName}"));
            var query = new Query($@"
DELETE FROM {tableName}
WHERE {whereClause}", param);
            return await query.Execute(connectionProvider);
        }
        
        public static async Task<T> GetByKey<T>(this IConnectionProvider connectionProvider, object param)
        {
            var type = typeof(T);
            var tableName = GetTableName(type);
            var columnInfos = await GetColumns(tableName, connectionProvider, type);
            var selectClause = string.Join(",", 
                columnInfos.Select(_ => _.ColumnName));
            var whereClause = string.Join(" AND ", 
                columnInfos.Where(_ => _.IsKey).Select(_ => $"{_.ColumnName}=@{_.ColumnName}"));
            var query = new Query($@"
SELECT {selectClause}
FROM {tableName}
WHERE {whereClause}", param);
            return await query.Single<T>(connectionProvider);
        }        

        private static string GetTableName(Type type) => type.Name;

        private static readonly ConcurrentDictionary<TableKey, List<ColumnInfo>> _columnDictionary = new();
        
        private static async Task<List<ColumnInfo>> GetColumns(string table, IConnectionProvider connectionProvider, Type type)
        {
            var tableKey = new TableKey(table, connectionProvider.ConnectionString);
            if (!_columnDictionary.TryGetValue(tableKey, out var value))
            {
                value = await GetColumnEnumerable(table, connectionProvider, type);
                _columnDictionary[tableKey] = value;
            }
            return value;
        }

        private record TableKey(string Table, string ConnectionString)
        {
        }

        private static async Task<List<ColumnInfo>> GetColumnEnumerable(string table, IConnectionProvider connectionProvider, Type type)
        {
            await using (var connection = await connectionProvider.GetConnection())
            {
                await connection.OpenIfClosed();

                await using (var command = connection.CreateCommand())
                {
                    command.CommandText = $"SELECT * FROM {table}";
                    await using (var reader = await command.ExecuteReaderAsync(CommandBehavior.SchemaOnly | CommandBehavior.KeyInfo))
                    {
                        reader.CheckMapping(type);
                        
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
        }
        
        private record ColumnInfo(string ColumnName, bool IsKey, bool IsAutoIncrement)
        {
        }
    }
}