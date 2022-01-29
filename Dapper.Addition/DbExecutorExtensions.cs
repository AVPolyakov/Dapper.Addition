using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Dapper;

namespace Dapper.Addition
{
    public static partial class DbExecutorExtensions
    {
        public static Task<IEnumerable<T>> QueryAsync<T>(this IDbExecutor<IDbConnection> executor, Sql sql)
            => executor.ExecuteAsync(connection => connection.QueryAsync<T>(sql));
        
        public static Task<T> QuerySingleAsync<T>(this IDbExecutor<IDbConnection> executor, Sql sql)
            => executor.ExecuteAsync(connection => connection.QuerySingleAsync<T>(sql));

        public static Task<T> QuerySingleOrDefaultAsync<T>(this IDbExecutor<IDbConnection> executor, Sql sql)
            => executor.ExecuteAsync(connection => connection.QuerySingleOrDefaultAsync<T>(sql));

        public static Task<T> QueryFirst<T>(this IDbExecutor<IDbConnection> executor, Sql sql)
            => executor.ExecuteAsync(connection => connection.QueryFirstAsync<T>(sql));

        public static Task<T> QueryFirstOrDefaultAsync<T>(this IDbExecutor<IDbConnection> executor, Sql sql)
            => executor.ExecuteAsync(connection => connection.QueryFirstOrDefaultAsync<T>(sql));
        
        public static Task<List<T>> QueryListAsync<T>(this IDbExecutor<IDbConnection> executor, Sql sql)
            => executor.ExecuteAsync(connection => connection.QueryListAsync<T>(sql));

        public static Task<T[]> QueryArrayAsync<T>(this IDbExecutor<IDbConnection> executor, Sql sql)
            => executor.ExecuteAsync(connection => connection.QueryArrayAsync<T>(sql));

        public static Task<int> ExecuteAsync(this IDbExecutor<IDbConnection> executor, Sql sql)
            => executor.ExecuteAsync(connection => connection.ExecuteAsync(sql));
        
        public static Task<T> QuerySingleAsync<T>(this IDbExecutor<IDbConnection> executor, string sql, object? param = null)
            => executor.ExecuteAsync(connection => connection.QuerySingleAsync<T>(sql, param));
        
        public static Task<int> ExecuteAsync(this IDbExecutor<IDbConnection> executor, string sql, object? param = null)
            => executor.ExecuteAsync(connection => connection.ExecuteAsync(sql, param));
    }
}