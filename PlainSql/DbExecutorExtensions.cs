using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace PlainSql
{
    public static partial class DbExecutorExtensions
    {
        public static Task<IEnumerable<T>> QueryAsync<T>(this IDbExecutor<IDbConnection> executor, Sql sql)
            => executor.ExecAsync(connection => connection.QueryAsync<T>(sql));
        
        public static Task<T> QuerySingleAsync<T>(this IDbExecutor<IDbConnection> executor, Sql sql)
            => executor.ExecAsync(connection => connection.QuerySingleAsync<T>(sql));

        public static Task<T> QuerySingleOrDefaultAsync<T>(this IDbExecutor<IDbConnection> executor, Sql sql)
            => executor.ExecAsync(connection => connection.QuerySingleOrDefaultAsync<T>(sql));

        public static Task<T> QueryFirst<T>(this IDbExecutor<IDbConnection> executor, Sql sql)
            => executor.ExecAsync(connection => connection.QueryFirstAsync<T>(sql));

        public static Task<T> QueryFirstOrDefaultAsync<T>(this IDbExecutor<IDbConnection> executor, Sql sql)
            => executor.ExecAsync(connection => connection.QueryFirstOrDefaultAsync<T>(sql));
        
        public static Task<List<T>> QueryListAsync<T>(this IDbExecutor<IDbConnection> executor, Sql sql)
            => executor.ExecAsync(connection => connection.QueryListAsync<T>(sql));

        public static Task<T[]> QueryArrayAsync<T>(this IDbExecutor<IDbConnection> executor, Sql sql)
            => executor.ExecAsync(connection => connection.QueryArrayAsync<T>(sql));

        public static Task<int> ExecuteAsync(this IDbExecutor<IDbConnection> executor, Sql sql)
            => executor.ExecAsync(connection => connection.ExecuteAsync(sql));
    }
}