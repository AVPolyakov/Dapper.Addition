using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using PlainQuery;

namespace PlainQueryExtensions
{
    public static partial class QueryExtensions
    {
        public static Task<List<T>> ToList<T>(this Query query, IHandler<DbConnection> connectionHandler)
        {
            return connectionHandler.Handle(async connection =>
            {
                await connection.OpenIfClosed();

                await using (var command = connection.CreateCommand())
                {
                    command.CommandText = query.StringBuilder.ToString();

                    AddParams(command, query);

                    await using (var reader = await command.ExecuteReaderAsync())
                    {
                        reader.CheckMapping<T>();

                        var materializer = reader.GetMaterializer<T>();

                        var result = new List<T>();

                        while (await reader.ReadAsync())
                            result.Add(materializer());

                        return result;
                    }
                }
            });
        }
        
        public static async Task<T> Single<T>(this Query query, IHandler<DbConnection> connectionHandler)
        {
            var list = await query.ToList<T>(connectionHandler);
            return list.Single();
        }

        public static async Task<T?> SingleOrDefault<T>(this Query query, IHandler<DbConnection> connectionHandler)
        {
            var list = await query.ToList<T>(connectionHandler);
            return list.SingleOrDefault();
        }

        public static Task<int> Execute(this Query query, IHandler<DbConnection> connectionHandler)
        {
            return connectionHandler.Handle(async connection =>
            {
                await connection.OpenIfClosed();

                await using (var command = connection.CreateCommand())
                {
                    command.CommandText = query.StringBuilder.ToString();

                    AddParams(command, query);

                    return await command.ExecuteNonQueryAsync();
                }
            });
        }
    }
}