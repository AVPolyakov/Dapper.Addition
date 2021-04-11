using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PlainQuery;

namespace PlainQueryExtensions
{
    public static partial class QueryExtensions
    {
        public static async Task<List<T>> ToList<T>(this Query query, IConnectionProvider connectionProvider)
        {
            await using (var connection = await connectionProvider.GetConnection())
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
            }
        }
        
        public static async Task<T> Single<T>(this Query query, IConnectionProvider connectionProvider)
        {
            var list = await query.ToList<T>(connectionProvider);
            return list.Single();
        }

        public static async Task<T?> SingleOrDefault<T>(this Query query, IConnectionProvider connectionProvider)
        {
            var list = await query.ToList<T>(connectionProvider);
            return list.SingleOrDefault();
        }

        public static async Task<int> Execute(this Query query, IConnectionProvider connectionProvider)
        {
            await using (var connection = await connectionProvider.GetConnection())
            {
                await connection.OpenIfClosed();

                await using (var command = connection.CreateCommand())
                {
                    command.CommandText = query.StringBuilder.ToString();

                    AddParams(command, query);

                    return await command.ExecuteNonQueryAsync();
                }
            }
        }
    }
}