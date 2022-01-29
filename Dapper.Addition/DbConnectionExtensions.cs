using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;

namespace Dapper.Addition
{
    public static partial class DbConnectionExtensions
    {
        public static Task<IEnumerable<T>> QueryAsync<T>(this IDbConnection connection, Sql sql) 
            => connection.QueryAsync<T>(sql.Text, sql.Parameters);

        public static Task<T> QuerySingleAsync<T>(this IDbConnection connection, Sql sql) 
            => connection.QuerySingleAsync<T>(sql.Text, sql.Parameters);

        public static Task<T> QuerySingleOrDefaultAsync<T>(this IDbConnection connection, Sql sql) 
            => connection.QuerySingleOrDefaultAsync<T>(sql.Text, sql.Parameters);

        public static Task<T> QueryFirstAsync<T>(this IDbConnection connection, Sql sql) 
            => connection.QueryFirstAsync<T>(sql.Text, sql.Parameters);

        public static Task<T> QueryFirstOrDefaultAsync<T>(this IDbConnection connection, Sql sql) 
            => connection.QueryFirstOrDefaultAsync<T>(sql.Text, sql.Parameters);

        public static async Task<List<T>> QueryListAsync<T>(this IDbConnection connection, Sql sql)
        {
            var enumerable = await connection.QueryAsync<T>(sql);
            return enumerable.AsList();
        }

        public static async Task<T[]> QueryArrayAsync<T>(this IDbConnection connection, Sql sql)
        {
            var enumerable = await connection.QueryAsync<T>(sql);
            return enumerable.ToArray();
        }

        public static Task<int> ExecuteAsync(this IDbConnection connection, Sql sql) => 
            connection.ExecuteAsync(sql.Text, sql.Parameters);
    }
}