using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace PlainDataAccess
{
    public static partial class QueryExtensions
    {
        public static Query Append(this Query query, string queryText)
        {
            query.StringBuilder.Append(queryText);
            return query;
        }
        
        public static Query Append(this Query query, string queryText, object param) 
            => query.Append(queryText).AddParams(param);

        public static async Task<List<T>> ToList<T>(this Query query)
        {
            await using (var connection = new SqlConnection(query.ConnectionInfo.ConnectionString))
            {
                await connection.OpenAsync();

                await using (var command = connection.CreateCommand())
                {
                    command.CommandText = query.StringBuilder.ToString();

                    foreach (var dbCommandAction in query.DbCommandActions) 
                        dbCommandAction(command);
                    
                    await using (var reader = await command.ExecuteReaderAsync())
                    {
                        CheckMapping<T>(reader);

                        var materializer = reader.GetMaterializer<T>();

                        var result = new List<T>();
                        
                        while (await reader.ReadAsync())
                            result.Add(materializer());
                        
                        return result;
                    }
                }
            }
        }
        
        public static async Task<T> Single<T>(this Query query)
        {
            var list = await query.ToList<T>();
            return list.Single();
        }

        public static async Task<int> Execute(this Query query)
        {
            await using (var connection = new SqlConnection(query.ConnectionInfo.ConnectionString))
            {
                await connection.OpenAsync();

                await using (var command = connection.CreateCommand())
                {
                    command.CommandText = query.StringBuilder.ToString();

                    foreach (var dbCommandAction in query.DbCommandActions)
                        dbCommandAction(command);

                    return await command.ExecuteNonQueryAsync();
                }
            }
        }

        public static Query Query(this Query query) => new(query.ConnectionInfo, query.DbCommandActions);
        
        public static Query Query(this Query query, string queryText)
            => query.Query().Append(queryText);
        
        public static Query Query(this Query query, string queryText, object param)
            => query.Query().Append(queryText, param);
    }
}