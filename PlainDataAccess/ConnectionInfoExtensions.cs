using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace PlainDataAccess
{
    public static class ConnectionInfoExtensions
    {
        public static Query Query(this IConnectionInfo connectionInfo) => new(connectionInfo);

        public static Query Query(this IConnectionInfo connectionInfo, string queryText)
            => connectionInfo.Query().Append(queryText);
        
        public static Query Query(this IConnectionInfo connectionInfo, string queryText, object param)
            => connectionInfo.Query().Append(queryText, param);

        public static Task<int> InsertWithInt32Identity(this IConnectionInfo connectionInfo, object p) 
            => connectionInfo.Insert<int>(p);

        public static Task<long> InsertWithInt64Identity(this IConnectionInfo connectionInfo, object p) 
            => connectionInfo.Insert<long>(p);

        public static Task<TKey> Insert<TKey>(this IConnectionInfo connectionInfo, object param)
        {
            var type = param.GetType();
            var table = GetTableName(type);
            var columnInfos = GetColumns(table, connectionInfo, type);
            var notAutoIncrementColumns = columnInfos
                .Where(_ => !_.IsAutoIncrement)
                .Select(_ => _.ColumnName)
                .ToList();
            var columnsClause = string.Join(",", notAutoIncrementColumns);
            var outClause = columnInfos.Single(_ => _.IsAutoIncrement).ColumnName;
            var valuesClause = string.Join(",", notAutoIncrementColumns.Select(_ => $"@{_}"));
            var query = connectionInfo.Query($@"
INSERT INTO {table} ({columnsClause}) 
OUTPUT inserted.{outClause}
VALUES ({valuesClause})", param);
            return query.Single<TKey>();
        }
        
        public static Task<int> Update(this IConnectionInfo connectionInfo, object param)
        {
            var type = param.GetType();
            var table = GetTableName(type);
            var columnInfos = GetColumns(table, connectionInfo, type);
            var setClause = string.Join(",",
                columnInfos
                    .Where(_ => !_.IsKey)
                    .Select(_ => $"{_.ColumnName}=@{_.ColumnName}"));
            var whereClause = string.Join(" AND ", 
                columnInfos.Where(_ => _.IsKey).Select(_ => $"{_.ColumnName}=@{_.ColumnName}"));
            var query = connectionInfo.Query($@"
UPDATE {table}
SET {setClause}
WHERE {whereClause}", param);
            return query.Execute();
        }

        public static Task<int> Delete<T>(this IConnectionInfo connectionInfo, object param)
        {
            var type = typeof(T);
            var tableName = GetTableName(type);
            var columnInfos = GetColumns(tableName, connectionInfo, type);
            var whereClause = string.Join(" AND ", 
                columnInfos.Where(_ => _.IsKey).Select(_ => $"{_.ColumnName}=@{_.ColumnName}"));
            var query = connectionInfo.Query($@"
DELETE FROM {tableName}
WHERE {whereClause}", param);
            return query.Execute();
        }
        
        public static Task<T> GetByKey<T>(this IConnectionInfo connectionInfo, object param)
        {
            var type = typeof(T);
            var tableName = GetTableName(type);
            var columnInfos = GetColumns(tableName, connectionInfo, type);
            var selectClause = string.Join(",", 
                columnInfos.Select(_ => _.ColumnName));
            var whereClause = string.Join(" AND ", 
                columnInfos.Where(_ => _.IsKey).Select(_ => $"{_.ColumnName}=@{_.ColumnName}"));
            var query = connectionInfo.Query($@"
SELECT {selectClause}
FROM {tableName}
WHERE {whereClause}", param);
            return query.Single<T>();
        }        

        private static string GetTableName(Type type) => type.Name;

        private static readonly ConcurrentDictionary<TableKey, List<ColumnInfo>> _columnDictionary = new();
        
        private static List<ColumnInfo> GetColumns(string table, IConnectionInfo connectionInfo, Type type)
        {
            var tableKey = new TableKey(table, connectionInfo.ConnectionString);
            if (!_columnDictionary.TryGetValue(tableKey, out var value))
            {
                value = GetColumnEnumerable(table, connectionInfo, type).ToList();
                _columnDictionary[tableKey] = value;
            }
            return value;
        }

        private record TableKey(string Table, string ConnectionString)
        {
        }

        private static IEnumerable<ColumnInfo> GetColumnEnumerable(string table, IConnectionInfo connectionInfo, Type type)
        {
            using (var connection = new SqlConnection(connectionInfo.ConnectionString))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = $"SELECT * FROM {table}";
                    using (var reader = command.ExecuteReader(CommandBehavior.SchemaOnly | CommandBehavior.KeyInfo))
                    {
                        QueryExtensions.CheckMapping(reader, type);
                        
                        var schemaTable = reader.GetSchemaTable();
                        foreach (DataRow dataRow in schemaTable.Rows)
                            yield return new ColumnInfo(
                                (string) dataRow["ColumnName"],
                                true.Equals(dataRow["IsKey"]),
                                true.Equals(dataRow["IsAutoIncrement"]));
                    }
                }
            }
        }        
    }
}