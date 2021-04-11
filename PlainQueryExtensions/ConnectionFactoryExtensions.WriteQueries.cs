using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using PlainQuery;

namespace PlainQueryExtensions
{
    public static class ConnectionProviderExtensions
    {
        public static async Task<TKey> Insert<TKey>(this IConnectionHandler<DbConnection> connectionHandler, object param)
        {
            var type = param.GetType();
            var table = GetTableName(type);
            var columnInfos = await GetColumns(table, connectionHandler, type);
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
            return await query.Single<TKey>(connectionHandler);
        }
        
        public static async Task<int> Insert(this IConnectionHandler<DbConnection> connectionHandler, object param)
        {
            var type = param.GetType();
            var table = GetTableName(type);
            var columnInfos = await GetColumns(table, connectionHandler, type);
            var columns = columnInfos
                .Select(_ => _.ColumnName)
                .ToList();
            var columnsClause = string.Join(",", columns);
            var valuesClause = string.Join(",", columns.Select(_ => $"@{_}"));
            var query = new Query($@"
INSERT INTO {table} ({columnsClause}) 
VALUES ({valuesClause})", param);
            return await query.Execute(connectionHandler);
        }
        
        public static async Task<int> Update(this IConnectionHandler<DbConnection> connectionHandler, object param)
        {
            var type = param.GetType();
            var table = GetTableName(type);
            var columnInfos = await GetColumns(table, connectionHandler, type);
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
            return await query.Execute(connectionHandler);
        }

        public static async Task<int> Delete<T>(this IConnectionHandler<DbConnection> connectionHandler, object param)
        {
            var type = typeof(T);
            var tableName = GetTableName(type);
            var columnInfos = await GetColumns(tableName, connectionHandler, type);
            var whereClause = string.Join(" AND ", 
                columnInfos.Where(_ => _.IsKey).Select(_ => $"{_.ColumnName}=@{_.ColumnName}"));
            var query = new Query($@"
DELETE FROM {tableName}
WHERE {whereClause}", param);
            return await query.Execute(connectionHandler);
        }
        
        public static async Task<T> GetByKey<T>(this IConnectionHandler<DbConnection> connectionHandler, object param)
        {
            var type = typeof(T);
            var tableName = GetTableName(type);
            var columnInfos = await GetColumns(tableName, connectionHandler, type);
            var selectClause = string.Join(",", 
                columnInfos.Select(_ => _.ColumnName));
            var whereClause = string.Join(" AND ", 
                columnInfos.Where(_ => _.IsKey).Select(_ => $"{_.ColumnName}=@{_.ColumnName}"));
            var query = new Query($@"
SELECT {selectClause}
FROM {tableName}
WHERE {whereClause}", param);
            return await query.Single<T>(connectionHandler);
        }        

        private static string GetTableName(Type type) => type.Name;

        private static readonly ConcurrentDictionary<TableKey, List<ColumnInfo>> _columnDictionary = new();
        
        private static async Task<List<ColumnInfo>> GetColumns(string table, IConnectionHandler<DbConnection> connectionHandler, Type type)
        {
            var tableKey = new TableKey(table, connectionHandler.ConnectionString);
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
            });
        }
        
        private record ColumnInfo(string ColumnName, bool IsKey, bool IsAutoIncrement)
        {
        }
    }
}